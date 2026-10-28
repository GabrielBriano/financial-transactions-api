using FinancialTransactions.Domain.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Abstractions
{
    public interface ITransactionRepository
    {
        Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinancialTransaction>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken);
        Task AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken);
    }
}
