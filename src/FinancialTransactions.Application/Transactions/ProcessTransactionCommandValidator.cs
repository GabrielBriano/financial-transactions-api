using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Transactions
{
    public sealed class ProcessTransactionCommandValidator : AbstractValidator<ProcessTransactionCommand>
    {
        public ProcessTransactionCommandValidator()
        {
            RuleFor(command => command.EventId)
                .NotEmpty()
                .WithMessage("EventId is required.");

            RuleFor(command => command.AccountId)
                .NotEmpty()
                .WithMessage("AccountId is required.");

            RuleFor(command => command.Type)
                .NotEmpty()
                .WithMessage("Type is required.")
                .Must(type => type.Equals("CREDIT", StringComparison.OrdinalIgnoreCase)
                           || type.Equals("DEBIT", StringComparison.OrdinalIgnoreCase))
                .WithMessage("Type must be CREDIT or DEBIT.");

            RuleFor(command => command.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");

            RuleFor(command => command.OccurredAt)
                .NotEmpty()
                .WithMessage("OccurredAt is required.");
        }
    }
}
