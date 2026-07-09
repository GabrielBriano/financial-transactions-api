# Financial Transactions API

API RESTful desenvolvida em ASP.NET Core para processamento seguro de transações financeiras, com foco em idempotência, consistência de saldo, transacionalidade, testes automatizados e execução via Docker Compose.

O projeto simula o motor principal de um serviço financeiro responsável por receber eventos de crédito e débito, atualizar o saldo consolidado de contas bancárias e manter o histórico transacional.

---

## Tecnologias utilizadas

- C# / ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Docker e Docker Compose
- Swagger / OpenAPI
- xUnit
- Moq
- FluentAssertions
- Testcontainers
- Redis
- RabbitMQ
- Serilog
- Health Checks

---

## Funcionalidades

- Processamento de transações financeiras do tipo `CREDIT` e `DEBIT`
- Idempotência por `eventId`
- Bloqueio de saldo negativo
- Histórico completo de transações processadas
- Atualização transacional de saldo e histórico
- Consulta de saldo da conta
- Consulta de histórico de transações por conta
- Processamento síncrono via API REST
- Processamento assíncrono via RabbitMQ
- Cache de consulta de conta com Redis
- Invalidação automática do cache após novas transações
- Health Check da aplicação
- Testes unitários e de integração

---

## Arquitetura

O projeto foi organizado seguindo uma separação inspirada em Clean Architecture:

```txt
src/
 ├── FinancialTransactions.Api
 ├── FinancialTransactions.Application
 ├── FinancialTransactions.Domain
 └── FinancialTransactions.Infrastructure

tests/
 ├── FinancialTransactions.UnitTests
 └── FinancialTransactions.IntegrationTests
```

### FinancialTransactions.Domain

Contém as entidades e regras de negócio principais.

Principais classes:

- `Account`
- `FinancialTransaction`
- `TransactionType`
- `DomainException`

A regra de saldo fica centralizada na entidade `Account`, garantindo que operações de débito não permitam saldo negativo.

### FinancialTransactions.Application

Contém o caso de uso principal da aplicação:

- `ProcessTransactionUseCase`

Essa camada orquestra:

- validação do comando
- verificação de idempotência
- busca ou criação da conta
- aplicação de crédito ou débito
- criação do histórico da transação
- execução dentro de uma unidade transacional
- invalidação do cache após processamento

Também contém as abstrações utilizadas pela aplicação:

- `IAccountRepository`
- `ITransactionRepository`
- `IUnitOfWork`
- `ICacheService`
- `ITransactionMessagePublisher`

### FinancialTransactions.Infrastructure

Contém as implementações de infraestrutura:

- Entity Framework Core
- PostgreSQL
- Repositórios
- Unit of Work
- Redis Cache
- RabbitMQ Publisher
- RabbitMQ Consumer
- Migrations

### FinancialTransactions.Api

Contém os controllers, contratos HTTP, configuração da aplicação, Swagger, Health Checks e injeção de dependência.

---

## Regras de negócio

### 1. Idempotência

Cada evento financeiro possui um `eventId`.

Um evento com o mesmo `eventId` não pode ser processado mais de uma vez.

A idempotência é garantida em duas camadas:

1. Consulta prévia no caso de uso.
2. Índice único no banco de dados para a coluna `event_id`.

Índice utilizado:

```txt
ux_transactions_event_id
```

Caso o mesmo evento seja enviado novamente, a API retorna:

```json
{
  "status": "Duplicated",
  "message": "Transaction already processed. Duplicate event ignored.",
  "eventId": "aaaaaaaa-1111-1111-1111-111111111111",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "currentBalance": null
}
```

---

### 2. Consistência de saldo

A conta não pode ficar com saldo negativo.

A regra fica dentro da entidade `Account`:

- `Credit(amount)` aumenta o saldo.
- `Debit(amount)` valida se existe saldo suficiente antes de debitar.

Caso o saldo seja insuficiente, a API retorna HTTP `409 Conflict`.

---

### 3. Transacionalidade

A gravação do histórico da transação e a atualização do saldo consolidado ocorrem dentro da mesma transação de banco de dados.

Ou as duas operações são confirmadas, ou nenhuma alteração é persistida.

Isso é feito por meio do `UnitOfWork`, usando transação do EF Core/PostgreSQL.

---

### 4. Concorrência

Para proteger o saldo em operações concorrentes, o repositório de contas utiliza lock pessimista com PostgreSQL:

```sql
SELECT *
FROM accounts
WHERE id = @accountId
FOR UPDATE
```

Isso evita que dois débitos simultâneos leiam o mesmo saldo e causem inconsistência.

Também existe um teste de integração validando o cenário de dois débitos concorrentes na mesma conta.

---

## Endpoints

### Processar transação síncrona

```http
POST /api/transactions
```

Payload:

```json
{
  "eventId": "aaaaaaaa-1111-1111-1111-111111111111",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "type": "CREDIT",
  "amount": 150.75,
  "occurredAt": "2026-01-30T10:15:00Z"
}
```

Tipos permitidos:

```txt
CREDIT
DEBIT
```

---

### Processar transação assíncrona

```http
POST /api/transactions/async
```

Esse endpoint publica a mensagem no RabbitMQ e retorna HTTP `202 Accepted`.

O processamento real é feito por um `BackgroundService`, que consome a fila e reutiliza o mesmo `ProcessTransactionUseCase`.

---

### Consultar conta

```http
GET /api/accounts/{accountId}
```

Exemplo de resposta:

```json
{
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "balance": 100,
  "createdAt": "2026-07-07T14:35:27.124951+00:00",
  "updatedAt": "2026-07-07T14:37:02.292597+00:00"
}
```

---

### Consultar histórico de transações

```http
GET /api/accounts/{accountId}/transactions
```

Exemplo de resposta:

```json
[
  {
    "id": "6c10cb68-02dc-45b5-8be2-fad5eb035d0f",
    "eventId": "aaaaaaaa-1111-1111-1111-111111111111",
    "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
    "type": "Credit",
    "amount": 150.75,
    "occurredAt": "2026-01-30T10:15:00+00:00",
    "createdAt": "2026-07-07T14:35:27.166651+00:00"
  }
]
```

---

### Health Check

```http
GET /health
```

Resposta esperada:

```txt
Healthy
```

O endpoint `/health` verifica a saúde da aplicação e dependências configuradas, como PostgreSQL e Redis.

O RabbitMQ também possui healthcheck configurado no Docker Compose.

---

## Como rodar o projeto

### Pré-requisitos

- Docker
- Docker Compose
- .NET SDK 10

---

### Subir aplicação e dependências

Na raiz do projeto, execute:

```bash
docker compose up --build
```

A aplicação ficará disponível em:

```txt
http://localhost:8080
```

Swagger:

```txt
http://localhost:8080/swagger
```

Health Check:

```txt
http://localhost:8080/health
```

RabbitMQ Management:

```txt
http://localhost:15672
```

Credenciais do RabbitMQ:

```txt
Usuário: guest
Senha: guest
```

---

### Derrubar ambiente

```bash
docker compose down
```

Para remover também os volumes do banco:

```bash
docker compose down -v
```

---

## Banco de dados

O projeto utiliza PostgreSQL.

O banco é criado automaticamente pelo Docker Compose com as seguintes configurações:

```txt
Host: postgres
Port: 5432
Database: financial_transactions
Username: postgres
Password: postgres
```

Dentro do container da API, a connection string aponta para o host `postgres`, que é o nome do serviço definido no Docker Compose.

Ao iniciar a aplicação fora do ambiente `Testing`, as migrations são aplicadas automaticamente.

---

## Migrations

As migrations ficam em:

```txt
src/FinancialTransactions.Infrastructure/Persistence/Migrations
```

Para criar uma nova migration:

```bash
dotnet ef migrations add NomeDaMigration --project src/FinancialTransactions.Infrastructure --startup-project src/FinancialTransactions.Api --output-dir Persistence/Migrations
```

---

## Redis

O Redis foi utilizado como cache para consultas de conta.

Fluxo:

1. O endpoint `GET /api/accounts/{accountId}` tenta buscar a conta no cache.
2. Se não existir cache, busca no PostgreSQL.
3. Após buscar no banco, salva no Redis por tempo limitado.
4. Sempre que uma nova transação é processada, o cache da conta é invalidado.

O PostgreSQL continua sendo a fonte da verdade.

Caso o Redis esteja indisponível, a aplicação continua funcionando normalmente, apenas sem cache.

---

## RabbitMQ

O RabbitMQ foi utilizado como diferencial para processamento assíncrono.

Fluxo:

```txt
POST /api/transactions/async
        ↓
Publica mensagem no RabbitMQ
        ↓
RabbitMqTransactionConsumer consome a mensagem
        ↓
ProcessTransactionUseCase processa a transação
        ↓
PostgreSQL atualiza saldo e histórico
```

O endpoint síncrono `POST /api/transactions` foi mantido como fluxo principal, garantindo uma resposta imediata para o processamento da transação.

O endpoint assíncrono retorna `202 Accepted`, indicando que o evento foi aceito para processamento.

---

## Testes

O projeto possui testes unitários e testes de integração.

### Testes unitários

Cobrem:

- criação de conta
- crédito
- débito
- saldo insuficiente
- validações de domínio
- processamento de transação
- evento duplicado
- comando inválido

### Testes de integração

Utilizam Testcontainers com PostgreSQL real.

Cobrem:

- processamento de crédito
- idempotência
- saldo insuficiente
- histórico de transações
- concorrência básica com dois débitos simultâneos

Nos testes de integração, o ambiente `Testing` não executa o consumer do RabbitMQ e não aplica migrations automaticamente no `Program.cs`. O próprio teste sobe o PostgreSQL via Testcontainers, substitui a connection string e aplica as migrations no banco temporário.

---

## Rodar testes

Para rodar todos os testes:

```bash
dotnet test
```

Resultado esperado:

```txt
Total: 19
Failed: 0
Passed: 19
```

---

## Exemplos de testes manuais

### Crédito

Endpoint:

```http
POST /api/transactions
```

Payload:

```json
{
  "eventId": "aaaaaaaa-1111-1111-1111-111111111111",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "type": "CREDIT",
  "amount": 150.75,
  "occurredAt": "2026-01-30T10:15:00Z"
}
```

Resposta esperada:

```json
{
  "status": "Processed",
  "message": "Transaction processed successfully.",
  "eventId": "aaaaaaaa-1111-1111-1111-111111111111",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "currentBalance": 150.75
}
```

---

### Evento duplicado

Reenviando o mesmo payload:

```json
{
  "status": "Duplicated",
  "message": "Transaction already processed. Duplicate event ignored.",
  "eventId": "aaaaaaaa-1111-1111-1111-111111111111",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "currentBalance": null
}
```

---

### Débito

Endpoint:

```http
POST /api/transactions
```

Payload:

```json
{
  "eventId": "aaaaaaaa-1111-1111-1111-111111111112",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "type": "DEBIT",
  "amount": 50.75,
  "occurredAt": "2026-01-30T10:20:00Z"
}
```

Resposta esperada:

```json
{
  "status": "Processed",
  "message": "Transaction processed successfully.",
  "eventId": "aaaaaaaa-1111-1111-1111-111111111112",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "currentBalance": 100
}
```

---

### Saldo insuficiente

Endpoint:

```http
POST /api/transactions
```

Payload:

```json
{
  "eventId": "aaaaaaaa-1111-1111-1111-111111111113",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "type": "DEBIT",
  "amount": 9999.99,
  "occurredAt": "2026-01-30T10:25:00Z"
}
```

Resposta esperada:

```json
{
  "status": "InsufficientFunds",
  "message": "Insufficient funds.",
  "eventId": "aaaaaaaa-1111-1111-1111-111111111113",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "currentBalance": 100
}
```

HTTP esperado:

```txt
409 Conflict
```

---

### Tipo inválido

Endpoint:

```http
POST /api/transactions
```

Payload:

```json
{
  "eventId": "aaaaaaaa-1111-1111-1111-111111111114",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "type": "TRANSFER",
  "amount": 10,
  "occurredAt": "2026-01-30T10:35:00Z"
}
```

Resposta esperada:

```json
{
  "status": "Invalid",
  "message": "Type must be CREDIT or DEBIT.",
  "eventId": "aaaaaaaa-1111-1111-1111-111111111114",
  "accountId": "bbbbbbbb-2222-2222-2222-222222222222",
  "currentBalance": null
}
```

HTTP esperado:

```txt
400 Bad Request
```

---

### Processamento assíncrono com RabbitMQ

Endpoint:

```http
POST /api/transactions/async
```

Payload:

```json
{
  "eventId": "dddddddd-4444-4444-4444-444444444444",
  "accountId": "eeeeeeee-5555-5555-5555-555555555555",
  "type": "CREDIT",
  "amount": 250.50,
  "occurredAt": "2026-01-30T11:00:00Z"
}
```

Resposta esperada:

```json
{
  "status": "Accepted",
  "message": "Transaction event accepted for asynchronous processing.",
  "eventId": "dddddddd-4444-4444-4444-444444444444",
  "accountId": "eeeeeeee-5555-5555-5555-555555555555",
  "currentBalance": null
}
```

Após alguns segundos, a conta pode ser consultada em:

```http
GET /api/accounts/eeeeeeee-5555-5555-5555-555555555555
```

Resposta esperada:

```json
{
  "accountId": "eeeeeeee-5555-5555-5555-555555555555",
  "balance": 250.50,
  "createdAt": "...",
  "updatedAt": "..."
}
```

---

## Decisões técnicas

### Por que separar em camadas?

A separação em `Domain`, `Application`, `Infrastructure` e `Api` facilita manutenção, testes e evolução do projeto.

As regras financeiras ficam isoladas do framework web e do banco de dados.

---

### Por que usar Unit of Work?

O `UnitOfWork` garante que a atualização do saldo e a gravação do histórico ocorram na mesma transação de banco.

Isso atende ao requisito de transacionalidade.

---

### Por que usar índice único em event_id?

A idempotência não deve depender apenas de validação em memória ou consulta prévia.

O índice único no banco garante integridade mesmo em cenários concorrentes.

---

### Por que usar lock pessimista?

Em sistemas financeiros, dois débitos simultâneos na mesma conta podem causar inconsistência se ambos lerem o mesmo saldo.

O `FOR UPDATE` bloqueia a linha da conta durante a transação e evita saldo negativo em concorrência.

---

### Por que manter endpoint síncrono e assíncrono?

O endpoint síncrono atende diretamente ao fluxo principal da API.

O endpoint assíncrono demonstra maturidade arquitetural, permitindo processar eventos via fila com RabbitMQ sem duplicar regras de negócio.

Ambos reutilizam o mesmo caso de uso.

---

### Por que Redis?

O Redis foi usado como cache para consultas de conta, reduzindo leituras no banco.

A aplicação invalida o cache sempre que uma transação é processada, evitando retorno de saldo desatualizado.

---

## Trade-offs e possíveis evoluções

- Capturar exceções de unique constraint em duplicidade concorrente extrema e retornar `Duplicated`.
- Usar `INSERT ... ON CONFLICT DO NOTHING` na criação concorrente de contas.
- Adicionar DLQ para mensagens com falha definitiva no RabbitMQ.
- Adicionar métricas com Prometheus/Grafana.
- Adicionar Elasticsearch/Kibana para centralização de logs estruturados.
- Adicionar autenticação/autorização.
- Adicionar paginação no histórico de transações.
- Adicionar correlation id para rastreabilidade ponta a ponta.

---

## Status da entrega

A solução entrega:

- Requisitos obrigatórios de negócio
- Requisitos técnicos
- Testes automatizados
- Execução via Docker Compose
- Diferenciais opcionais com Redis, RabbitMQ e Health Checks