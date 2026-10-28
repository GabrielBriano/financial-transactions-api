namespace FinancialTransactions.Domain.Common
{
    public sealed class InsufficientFundsException : DomainException
    {
        public InsufficientFundsException() : base("Insufficient funds.")
        {
        }
    }
}
