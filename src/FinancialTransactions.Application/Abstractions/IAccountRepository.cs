using FinancialTransactions.Domain.Accounts;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Abstractions
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);
        Task<Account?> GetByIdForUpdateAsync(Guid accountId, CancellationToken cancellationToken);
        Task AddAsync(Account account, CancellationToken cancellationToken);
    }
}
