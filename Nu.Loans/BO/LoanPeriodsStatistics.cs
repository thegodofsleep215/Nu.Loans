using System;

namespace Nu.Loans.BO
{
    public class LoanPeriodsStatistics
    {
        public LoanPeriodsStatistics(int year, int month, int day, decimal payment)
        {
            Year = year;
            Month = month;
            Day = day;
            PresentValue = 0;
            PaidOnInterest = 0;
            Payment = payment;
        }

        public LoanPeriodsStatistics(LoanPayment payment, decimal presentValue, decimal paidOnInterest)
        {
            Year = payment.Year;
            Month = payment.Month;
            Day = payment.Day;
            Payment = payment.Payment;
            PresentValue = presentValue;
            PaidOnInterest = paidOnInterest;
        }

        public LoanPeriodsStatistics(DateTime date, decimal payment, decimal presentValue, decimal paidOnInterest)
        {
            Year = date.Year;
            Month = date.Month;
            Day = date.Day;
            Payment = payment;
            PresentValue = presentValue;
            PaidOnInterest = paidOnInterest;
        }

        public int Year { get; set; }

        public int Month { get; set; }

        public int Day { get; set; }

        public decimal PresentValue { get; set; }

        public decimal Payment { get; set; }

        public decimal PaidOnInterest { get; set; }
    }
}