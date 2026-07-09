using System;
using System.Collections.Generic;
using System.Text;
using FinancialTransactions.Domain.Accounts;
using FinancialTransactions.Domain.Common;

namespace FinancialTransactions.Domain.Transactions
{
    public sealed class FinancialTransaction
    {
        private FinancialTransaction()
        {
        }

        public FinancialTransaction(
            Guid eventId,
            Guid accountId,
            TransactionType type,
            decimal amount,
            DateTimeOffset occurredAt)
        {
            if (eventId == Guid.Empty)
                throw new DomainException("Event id cannot be empty.");

            if (accountId == Guid.Empty)
                throw new DomainException("Account id cannot be empty.");

            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero.");

            Id = Guid.NewGuid();
            EventId = eventId;
            AccountId = accountId;
            Type = type;
            Amount = amount;
            OccurredAt = occurredAt;
            CreatedAt = DateTimeOffset.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid EventId { get; private set; }
        public Guid AccountId { get; private set; }
        public TransactionType Type { get; private set; }
        public decimal Amount { get; private set; }
        public DateTimeOffset OccurredAt { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        public Account? Account { get; private set; }
    }
}
