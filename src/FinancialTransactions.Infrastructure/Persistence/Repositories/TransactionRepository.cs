using FinancialTransactions.Application.Abstractions;
using FinancialTransactions.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Infrastructure.Persistence.Repositories
{
    public sealed class TransactionRepository : ITransactionRepository
    {
        private readonly FinancialTransactionsDbContext _dbContext;

        public TransactionRepository(FinancialTransactionsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> ExistsByEventIdAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return _dbContext.Transactions
                .AnyAsync(transaction => transaction.EventId == eventId, cancellationToken);
        }

        public async Task<IReadOnlyList<FinancialTransaction>> GetByAccountIdAsync(
            Guid accountId,
            CancellationToken cancellationToken)
        {
            return await _dbContext.Transactions
                .AsNoTracking()
                .Where(transaction => transaction.AccountId == accountId)
                .OrderBy(transaction => transaction.OccurredAt)
                .ThenBy(transaction => transaction.CreatedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(FinancialTransaction transaction, CancellationToken cancellationToken)
        {
            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
        }
    }
}
