using FinancialTransactions.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancialTransactions.Infrastructure.Persistence
{
    public sealed class UnitOfWork : IUnitOfWork
    {
        private const int MaxAttempts = 2;

        private readonly FinancialTransactionsDbContext _dbContext;

        public UnitOfWork(FinancialTransactionsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await ExecuteOnceAsync(operation, cancellationToken);
                    return;
                }
                catch (DbUpdateException ex) when (attempt < MaxAttempts && IsUniqueViolation(ex))
                {
                    // Duas requisições concorrentes criaram a mesma conta ou usaram o mesmo EventId.
                    // Limpa o estado rastreado e repete: na nova tentativa a conta já existe
                    // (e é bloqueada com FOR UPDATE) ou o EventId é reconhecido como duplicado.
                    _dbContext.ChangeTracker.Clear();
                }
            }
        }

        private async Task ExecuteOnceAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await operation(cancellationToken);

                    await _dbContext.SaveChangesAsync(cancellationToken);

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }
}
