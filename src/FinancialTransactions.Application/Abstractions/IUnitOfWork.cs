using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Abstractions
{
    public interface IUnitOfWork
    {
        Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken);
    }
}
