using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Application.Transactions
{
    public enum ProcessTransactionStatus
    {
        Processed = 1,
        Duplicated = 2,
        InsufficientFunds = 3,
        Invalid = 4
    }
}
