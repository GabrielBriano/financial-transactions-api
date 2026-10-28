namespace FinancialTransactions.Api.Contracts
{
    public sealed record TransactionResponse(
        Guid Id,
        Guid EventId,
        Guid AccountId,
        string Type,
        decimal Amount,
        DateTimeOffset OccurredAt,
        DateTimeOffset CreatedAt);
}
