namespace FinancialTransactions.Api.Contracts
{
    public sealed record AccountResponse(
        Guid AccountId,
        decimal Balance,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt);
}
