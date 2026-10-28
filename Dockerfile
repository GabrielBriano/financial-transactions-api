FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FinancialTransactions.slnx ./

COPY src/FinancialTransactions.Api/FinancialTransactions.Api.csproj src/FinancialTransactions.Api/
COPY src/FinancialTransactions.Application/FinancialTransactions.Application.csproj src/FinancialTransactions.Application/
COPY src/FinancialTransactions.Domain/FinancialTransactions.Domain.csproj src/FinancialTransactions.Domain/
COPY src/FinancialTransactions.Infrastructure/FinancialTransactions.Infrastructure.csproj src/FinancialTransactions.Infrastructure/

COPY tests/FinancialTransactions.UnitTests/FinancialTransactions.UnitTests.csproj tests/FinancialTransactions.UnitTests/
COPY tests/FinancialTransactions.IntegrationTests/FinancialTransactions.IntegrationTests.csproj tests/FinancialTransactions.IntegrationTests/

RUN dotnet restore FinancialTransactions.slnx

COPY . .

RUN dotnet publish src/FinancialTransactions.Api/FinancialTransactions.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "FinancialTransactions.Api.dll"]