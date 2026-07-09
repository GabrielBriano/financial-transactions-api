namespace FinancialTransactions.Api.Contracts
{
    public sealed record ProcessTransactionRequest(
        Guid EventId,
        Guid AccountId,
        string Type,
        decimal Amount,
        DateTimeOffset OccurredAt);
}
