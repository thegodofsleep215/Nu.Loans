using System;

namespace Nu.Loans.BO
{
    public class LoanPayment
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public DateTime Date => new DateTime(Year, Month, Day);

        public decimal Payment { get; set; }
    }
}