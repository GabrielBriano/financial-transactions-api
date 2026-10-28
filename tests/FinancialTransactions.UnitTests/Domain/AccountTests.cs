using FinancialTransactions.Domain.Accounts;
using FinancialTransactions.Domain.Common;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.UnitTests.Domain
{
    public sealed class AccountTests
    {
        [Fact]
        public void Constructor_Should_CreateAccountWithZeroBalance_When_IdIsValid()
        {
            var accountId = Guid.NewGuid();

            var account = new Account(accountId);

            account.Id.Should().Be(accountId);
            account.Balance.Should().Be(0);
            account.CreatedAt.Should().NotBe(default);
            account.UpdatedAt.Should().NotBe(default);
        }

        [Fact]
        public void Constructor_Should_ThrowDomainException_When_IdIsEmpty()
        {
            var action = () => new Account(Guid.Empty);

            action.Should()
                .Throw<DomainException>()
                .WithMessage("Account id cannot be empty.");
        }

        [Fact]
        public void Credit_Should_IncreaseBalance_When_AmountIsValid()
        {
            var account = new Account(Guid.NewGuid());

            account.Credit(150.75m);

            account.Balance.Should().Be(150.75m);
        }

        [Fact]
        public void Credit_Should_ThrowDomainException_When_AmountIsZero()
        {
            var account = new Account(Guid.NewGuid());

            var action = () => account.Credit(0);

            action.Should()
                .Throw<DomainException>()
                .WithMessage("Amount must be greater than zero.");
        }

        [Fact]
        public void Credit_Should_ThrowDomainException_When_AmountIsNegative()
        {
            var account = new Account(Guid.NewGuid());

            var action = () => account.Credit(-10);

            action.Should()
                .Throw<DomainException>()
                .WithMessage("Amount must be greater than zero.");
        }

        [Fact]
        public void Debit_Should_DecreaseBalance_When_AmountIsValidAndBalanceIsEnough()
        {
            var account = new Account(Guid.NewGuid());

            account.Credit(150.75m);
            account.Debit(50.75m);

            account.Balance.Should().Be(100m);
        }

        [Fact]
        public void Debit_Should_ThrowDomainException_When_BalanceIsInsufficient()
        {
            var account = new Account(Guid.NewGuid());

            account.Credit(100m);

            var action = () => account.Debit(150m);

            action.Should()
                .Throw<DomainException>()
                .WithMessage("Insufficient funds.");

            account.Balance.Should().Be(100m);
        }

        [Fact]
        public void Debit_Should_ThrowDomainException_When_AmountIsZero()
        {
            var account = new Account(Guid.NewGuid());

            var action = () => account.Debit(0);

            action.Should()
                .Throw<DomainException>()
                .WithMessage("Amount must be greater than zero.");
        }
    }
}
