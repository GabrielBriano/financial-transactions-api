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