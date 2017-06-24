using System;
using System.Collections.Generic;

namespace Nu.Loans.BO
{
    public class LoanPayments : Dictionary<DateTime, LoanPayment>
    {
        public LoanPayments(Dictionary<DateTime, LoanPayment> dict) : base(dict)
        {
        }

        public LoanPayments()
        {
        }
    }
}