using FinancialTransactions.Domain.Accounts;
using FinancialTransactions.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Infrastructure.Persistence
{
    public sealed class FinancialTransactionsDbContext : DbContext
    {
        public FinancialTransactionsDbContext(DbContextOptions<FinancialTransactionsDbContext> options)
            : base(options)
        {
        }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<FinancialTransaction> Transactions => Set<FinancialTransaction>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinancialTransactionsDbContext).Assembly);
        }
    }
}
