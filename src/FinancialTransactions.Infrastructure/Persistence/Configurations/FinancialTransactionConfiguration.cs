using FinancialTransactions.Domain.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Infrastructure.Persistence.Configurations
{
    public sealed class FinancialTransactionConfiguration : IEntityTypeConfiguration<FinancialTransaction>
    {
        public void Configure(EntityTypeBuilder<FinancialTransaction> builder)
        {
            builder.ToTable("transactions");

            builder.HasKey(transaction => transaction.Id);

            builder.Property(transaction => transaction.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            builder.Property(transaction => transaction.EventId)
                .HasColumnName("event_id")
                .IsRequired();

            builder.Property(transaction => transaction.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(transaction => transaction.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(transaction => transaction.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(transaction => transaction.OccurredAt)
                .HasColumnName("occurred_at")
                .IsRequired();

            builder.Property(transaction => transaction.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.HasIndex(transaction => transaction.EventId)
                .IsUnique()
                .HasDatabaseName("ux_transactions_event_id");

            builder.HasIndex(transaction => transaction.AccountId)
                .HasDatabaseName("ix_transactions_account_id");

            builder.HasOne(transaction => transaction.Account)
                .WithMany()
                .HasForeignKey(transaction => transaction.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
