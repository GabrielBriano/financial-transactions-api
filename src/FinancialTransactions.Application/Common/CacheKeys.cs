namespace FinancialTransactions.Application.Common
{
    public static class CacheKeys
    {
        public static string Account(Guid accountId)
        {
            return $"accounts:{accountId}";
        }
    }
}
