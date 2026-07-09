using FinancialTransactions.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Domain.Accounts
{
    public sealed class Account
    {
        private Account()
        {
        }

        public Account(Guid id)
        {
            if (id == Guid.Empty)
                throw new DomainException("Account id cannot be empty.");

            Id = id;
            Balance = 0;
            CreatedAt = DateTimeOffset.UtcNow;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public Guid Id { get; private set; }
        public decimal Balance { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset UpdatedAt { get; private set; }

        public void Credit(decimal amount)
        {
            ValidateAmount(amount);

            Balance += amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void Debit(decimal amount)
        {
            ValidateAmount(amount);

            if (Balance < amount)
                throw new DomainException("Insufficient funds.");

            Balance -= amount;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static void ValidateAmount(decimal amount)
        {
            if (amount <= 0)
                throw new DomainException("Amount must be greater than zero.");
        }
    }
}
