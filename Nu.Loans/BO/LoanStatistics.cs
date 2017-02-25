using System;
using System.Collections.Generic;
using System.Linq;

namespace Nu.Loans.BO
{
    public class LoanStatistics : Dictionary<DateTime, LoanPeriodsStatistics>
    {
        public decimal InterestPaid
        {
            get { return Values.Sum(x => x.PaidOnInterest); }
        }

        public decimal TotalPaid
        {
            get { return Values.Sum(x => x.Payment); }
        }

        public LoanPeriodsStatistics this[int y, int m, int d]
        {
            get { return this[new DateTime(y, m, d)]; }
            set { this[new DateTime(y, m, d)] = value; }
        }

        public LoanPeriodsStatistics this[int i]
        {
            get { return this.ElementAt(i).Value; }
            set { this[this.ElementAt(i).Key] = value; }
        }


    }
}