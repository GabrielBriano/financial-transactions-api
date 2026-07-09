using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Transactions
{
    public sealed record ProcessTransactionResult(
    ProcessTransactionStatus Status,
    string Message,
    Guid EventId,
    Guid AccountId,
    decimal? CurrentBalance = null)
    {
        public bool Success => Status is ProcessTransactionStatus.Processed or ProcessTransactionStatus.Duplicated;

        public static ProcessTransactionResult Processed(
            Guid eventId,
            Guid accountId,
            decimal currentBalance)
        {
            return new ProcessTransactionResult(
                ProcessTransactionStatus.Processed,
                "Transaction processed successfully.",
                eventId,
                accountId,
                currentBalance);
        }

        public static ProcessTransactionResult Duplicated(
            Guid eventId,
            Guid accountId)
        {
            return new ProcessTransactionResult(
                ProcessTransactionStatus.Duplicated,
                "Transaction already processed. Duplicate event ignored.",
                eventId,
                accountId);
        }

        public static ProcessTransactionResult InsufficientFunds(
            Guid eventId,
            Guid accountId,
            decimal currentBalance)
        {
            return new ProcessTransactionResult(
                ProcessTransactionStatus.InsufficientFunds,
                "Insufficient funds.",
                eventId,
                accountId,
                currentBalance);
        }

        public static ProcessTransactionResult Invalid(
            Guid eventId,
            Guid accountId,
            string message)
        {
            return new ProcessTransactionResult(
                ProcessTransactionStatus.Invalid,
                message,
                eventId,
                accountId);
        }
    }
}
