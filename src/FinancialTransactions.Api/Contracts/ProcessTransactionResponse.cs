namespace FinancialTransactions.Api.Contracts
{
    public sealed record ProcessTransactionResponse(
        string Status,
        string Message,
        Guid EventId,
        Guid AccountId,
        decimal? CurrentBalance);
}
