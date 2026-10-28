using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Infrastructure.Persistence.Repositories
{
    public sealed class AccountRepository : IAccountRepository
    {
        private readonly FinancialTransactionsDbContext _dbContext;

        public AccountRepository(FinancialTransactionsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .FirstOrDefaultAsync(account => account.Id == accountId, cancellationToken);
        }

        public Task<Account?> GetByIdForUpdateAsync(Guid accountId, CancellationToken cancellationToken)
        {
            return _dbContext.Accounts
                .FromSqlInterpolated($"""
                SELECT * 
                FROM accounts 
                WHERE id = {accountId}
                FOR UPDATE
                """)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken)
        {
            await _dbContext.Accounts.AddAsync(account, cancellationToken);
        }
    }
}
