using FinancialTransactions.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Infrastructure.Persistence.Configurations
{
    public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("accounts");

            builder.HasKey(account => account.Id);

            builder.Property(account => account.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(account => account.Balance)
                .HasColumnName("balance")
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(account => account.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(account => account.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(account => account.Id)
                .HasDatabaseName("ix_accounts_id");
        }
    }
}
