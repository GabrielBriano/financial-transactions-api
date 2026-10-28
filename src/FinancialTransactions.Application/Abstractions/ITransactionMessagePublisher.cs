using FinancialTransactions.Application.Transactions;

namespace FinancialTransactions.Application.Abstractions
{
    public interface ITransactionMessagePublisher
    {
        Task PublishAsync(ProcessTransactionCommand command, CancellationToken cancellationToken);
    }
}
