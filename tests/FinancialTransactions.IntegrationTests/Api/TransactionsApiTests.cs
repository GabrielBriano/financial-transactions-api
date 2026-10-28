using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FinancialTransactions.IntegrationTests.Infrastructure;

namespace FinancialTransactions.IntegrationTests.Api
{
    public sealed class TransactionsApiTests : IClassFixture<FinancialTransactionsApiFactory>
    {
        private readonly HttpClient _client;

        public TransactionsApiTests(FinancialTransactionsApiFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task PostTransaction_Should_ProcessCredit_And_UpdateAccountBalance()
        {
            var accountId = Guid.NewGuid();

            var request = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "CREDIT",
                Amount = 150.75m,
                OccurredAt = DateTimeOffset.UtcNow
            };

            var response = await _client.PostAsJsonAsync("/api/transactions", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<TransactionProcessResponse>();

            body.Should().NotBeNull();
            body!.Status.Should().Be("Processed");
            body.CurrentBalance.Should().Be(150.75m);

            var accountResponse = await _client.GetAsync($"/api/accounts/{accountId}");

            accountResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>();

            account.Should().NotBeNull();
            account!.Balance.Should().Be(150.75m);
        }

        [Fact]
        public async Task PostTransaction_Should_ReturnDuplicated_When_EventIdAlreadyExists()
        {
            var eventId = Guid.NewGuid();
            var accountId = Guid.NewGuid();

            var request = new
            {
                EventId = eventId,
                AccountId = accountId,
                Type = "CREDIT",
                Amount = 100m,
                OccurredAt = DateTimeOffset.UtcNow
            };

            var firstResponse = await _client.PostAsJsonAsync("/api/transactions", request);
            var secondResponse = await _client.PostAsJsonAsync("/api/transactions", request);

            firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondBody = await secondResponse.Content.ReadFromJsonAsync<TransactionProcessResponse>();

            secondBody.Should().NotBeNull();
            secondBody!.Status.Should().Be("Duplicated");

            var accountResponse = await _client.GetAsync($"/api/accounts/{accountId}");
            var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>();

            account.Should().NotBeNull();
            account!.Balance.Should().Be(100m);
        }

        [Fact]
        public async Task PostTransaction_Should_ReturnConflict_When_DebitExceedsBalance()
        {
            var accountId = Guid.NewGuid();

            var creditRequest = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "CREDIT",
                Amount = 100m,
                OccurredAt = DateTimeOffset.UtcNow
            };

            var debitRequest = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "DEBIT",
                Amount = 150m,
                OccurredAt = DateTimeOffset.UtcNow
            };

            await _client.PostAsJsonAsync("/api/transactions", creditRequest);

            var debitResponse = await _client.PostAsJsonAsync("/api/transactions", debitRequest);

            debitResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

            var debitBody = await debitResponse.Content.ReadFromJsonAsync<TransactionProcessResponse>();

            debitBody.Should().NotBeNull();
            debitBody!.Status.Should().Be("InsufficientFunds");
            debitBody.CurrentBalance.Should().Be(100m);

            var accountResponse = await _client.GetAsync($"/api/accounts/{accountId}");
            var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>();

            account.Should().NotBeNull();
            account!.Balance.Should().Be(100m);
        }

        [Fact]
        public async Task GetTransactions_Should_ReturnAccountTransactionHistory()
        {
            var accountId = Guid.NewGuid();

            var creditRequest = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "CREDIT",
                Amount = 200m,
                OccurredAt = DateTimeOffset.UtcNow
            };

            var debitRequest = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "DEBIT",
                Amount = 50m,
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(1)
            };

            await _client.PostAsJsonAsync("/api/transactions", creditRequest);
            await _client.PostAsJsonAsync("/api/transactions", debitRequest);

            var response = await _client.GetAsync($"/api/accounts/{accountId}/transactions");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();

            transactions.Should().NotBeNull();
            transactions.Should().HaveCount(2);
            transactions![0].Amount.Should().Be(200m);
            transactions[1].Amount.Should().Be(50m);
        }

        [Fact]
        public async Task PostTransaction_Should_NotAllowNegativeBalance_When_TwoDebitsRunConcurrently()
        {
            var accountId = Guid.NewGuid();

            var creditRequest = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "CREDIT",
                Amount = 100m,
                OccurredAt = DateTimeOffset.UtcNow
            };

            var creditResponse = await _client.PostAsJsonAsync("/api/transactions", creditRequest);

            creditResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var debitRequestOne = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "DEBIT",
                Amount = 80m,
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(1)
            };

            var debitRequestTwo = new
            {
                EventId = Guid.NewGuid(),
                AccountId = accountId,
                Type = "DEBIT",
                Amount = 80m,
                OccurredAt = DateTimeOffset.UtcNow.AddMinutes(2)
            };

            var debitTasks = new[]
            {
        _client.PostAsJsonAsync("/api/transactions", debitRequestOne),
        _client.PostAsJsonAsync("/api/transactions", debitRequestTwo)
    };

            var debitResponses = await Task.WhenAll(debitTasks);

            debitResponses.Should().ContainSingle(response => response.StatusCode == HttpStatusCode.OK);
            debitResponses.Should().ContainSingle(response => response.StatusCode == HttpStatusCode.Conflict);

            var accountResponse = await _client.GetAsync($"/api/accounts/{accountId}");

            accountResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var account = await accountResponse.Content.ReadFromJsonAsync<AccountResponse>();

            account.Should().NotBeNull();
            account!.Balance.Should().Be(20m);

            var transactionsResponse = await _client.GetAsync($"/api/accounts/{accountId}/transactions");

            transactionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var transactions = await transactionsResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>();

            transactions.Should().NotBeNull();
            transactions.Should().HaveCount(2);

            transactions!
                .Where(transaction => transaction.Type == "Debit")
                .Should()
                .ContainSingle();
        }

        private sealed record TransactionProcessResponse(
            string Status,
            string Message,
            Guid EventId,
            Guid AccountId,
            decimal? CurrentBalance);

        private sealed record AccountResponse(
            Guid AccountId,
            decimal Balance,
            DateTimeOffset CreatedAt,
            DateTimeOffset UpdatedAt);

        private sealed record TransactionResponse(
            Guid Id,
            Guid EventId,
            Guid AccountId,
            string Type,
            decimal Amount,
            DateTimeOffset OccurredAt,
            DateTimeOffset CreatedAt);
    }
}
