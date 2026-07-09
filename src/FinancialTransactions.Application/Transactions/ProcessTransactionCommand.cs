using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Transactions
{
    public sealed record ProcessTransactionCommand(
        Guid EventId,
        Guid AccountId,
        string Type,
        decimal Amount,
        DateTimeOffset OccurredAt);
}
