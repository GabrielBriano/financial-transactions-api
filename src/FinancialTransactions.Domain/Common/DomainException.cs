using System;
using System.Collections.Generic;
using System.Text;

namespace FinancialTransactions.Domain.Common
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message)
        {
        }
    }
}
