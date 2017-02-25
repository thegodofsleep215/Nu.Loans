using System;
using System.Collections.Generic;
using System.Linq;
using Nu.Loans.BO;

namespace Nu.Loans.BL
{
    public class LoanPayoff
    {
        public Loan ApplyPayments(Loan loan, DateTime startTime, LoanPayments payments,  out LoanStatistics loanStats)
        {
            loanStats = new LoanStatistics();
            var fv = ApplyPayments(startTime.Year, startTime.Month, startTime.Day, loan.PresentValue, loan.DailyInterestRate,
                payments, ref loanStats);

            return new Loan(loan.Name, loan.AnnualPercentageRate, fv, loan.AnnualWorth);
        }

        public Loan ApplyPayments(Loan loan, out LoanStatistics loanStats)
        {
//j                       Financial.FGivenP((decimal) 6653.54, n, allLoans.MonthlyInterestRate) -
 //                       Financial.FGivenA(annualWorth, n, allLoans.MonthlyInterestRate), 0, annualWorth, out finalPayment);
  

            decimal garbage;
            var periods = Financial.FindN(n => Financial.FGivenP(loan.PresentValue, n, loan.MonthlyInterestRate) - Financial.FGivenA(loan.AnnualWorth, n, loan.MonthlyInterestRate), 0,
                loan.AnnualWorth, out garbage);
            loanStats = new LoanStatistics();
            var now = DateTime.Now;
            var year = now.Year;
            var month = now.Month;
            var day = now.Day;
            var payments = new LoanPayments();

            if (day > 1)
            {
                month = NextMonth(month, ref year);
            }
            for (int i = 0; i < periods; i++)
            {
                var period = new DateTime(year, month, 1);
                payments[period] = new LoanPayment {Year = year, Month = month, Day = day, Payment = loan.AnnualWorth};
                month = NextMonth(month, ref year);
            }
            if (payments.Count != periods) throw new Exception();

            var fv = ApplyPayments(now.Year, now.Month, now.Day, loan.PresentValue, loan.DailyInterestRate, payments, ref loanStats);
            return new Loan(loan.Name, loan.AnnualPercentageRate, fv, loan.AnnualWorth);
        }

        private static int NextMonth(int month, ref int year)
        {
            if (month == 12)
            {
                month = 1;
                year++;
            }
            else
            {
                month++;
            }
            return month;
        }

        private decimal ApplyPayments(int startYear, int startMonth, int startDay, decimal presentValue, decimal interestRate, LoanPayments payments,
            ref LoanStatistics stats)
        {
            var currentDate = new DateTime(startYear, startMonth, startDay);
            var orderedList = payments.Values.OrderBy(x => x.Year).ThenBy(x => x.Month).ThenBy(x => x.Day);
            foreach (var loanPayment in orderedList)
            {
                // Assuming allLoans payment date is after current date.
                if (presentValue == 0)
                {
                    stats[loanPayment.Date] = new LoanPeriodsStatistics(loanPayment.Date, 0, 0, 0);
                }
                else
                {
                    var daysOfInterest = (loanPayment.Date - currentDate).Days;
                    var fv = Financial.FGivenP(presentValue, daysOfInterest, interestRate);
                    var poi = fv - presentValue;
                    if ((fv < loanPayment.Payment && fv > 0) ||
                        (fv > loanPayment.Payment && fv < 0))
                    {
                        presentValue = 0;
                        stats[loanPayment.Date] = new LoanPeriodsStatistics(loanPayment.Date, fv, 0, poi);
                    }
                    else
                    {
                        presentValue = fv - loanPayment.Payment;
                        stats[loanPayment.Date] = new LoanPeriodsStatistics(loanPayment, presentValue, poi);
                    }
                }
                currentDate = loanPayment.Date;
            }
            return presentValue;
        }

        public Dictionary<string, LoanStatistics> ApplyPayments(List<Loan> allLoans, decimal additionalMoney,
            decimal initialDown)
        {

            var loans = allLoans.Select(x => new LoanExt(x)).ToList();
                 var now = DateTime.Now;
            var startYear = now.Year;
            var startMonth = now.Month;
            var startDay = now.Day;
            int paymentDay = 1;
            var stats = loans.ToDictionary(x => x.Name, x => new LoanStatistics());
            var currentDate = new DateTime(startYear, startMonth, startDay);
            DateTime nextPaymentDay;
            if (currentDate.Day > paymentDay)
            {
                nextPaymentDay = startMonth == 12
                    ? new DateTime(startYear + 1, 1, paymentDay)
                    : new DateTime(startYear, startMonth, paymentDay);
            }
            else
            {
                nextPaymentDay = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day);
            }
            while(loans.Any(x => x.PresentValue > 0))
            {
                decimal totalOverage = 0;
                foreach (var loan in loans)
                {
                    // Assuming allLoans payment date is after current date.
                    if (loan.PresentValue == 0)
                    {
                        stats[loan.Name][nextPaymentDay] = new LoanPeriodsStatistics(nextPaymentDay, 0, 0, 0);
                    }
                    else
                    {
                        decimal poi;
                        var fv = Fv(loan, currentDate, nextPaymentDay, out poi);

                        if ((fv < loan.TotalPayment && fv > 0) ||
                            (fv - loan.TotalPayment == 0))
                        {
                            totalOverage += fv;
                            loan.PresentValue = 0;
                            stats[loan.Name][nextPaymentDay] = new LoanPeriodsStatistics(nextPaymentDay, fv, 0, poi);
                        }
                        else
                        {
                            loan.PresentValue = fv - loan.TotalPayment;
                            stats[loan.Name][nextPaymentDay] = new LoanPeriodsStatistics(nextPaymentDay, loan.TotalPayment, loan.PresentValue, poi);
                            decimal overage;
                            if (NextPaymentIsFinal(loan, nextPaymentDay, out overage))
                            {
                                loan.TotalPayment -= overage;
                                totalOverage += overage;
                            }
                        }
                    }
                }

                // Rolling over payments that just went into loans that were paid off.
                currentDate = nextPaymentDay;
                nextPaymentDay = NextPaymentDay(nextPaymentDay);
            }
            return stats;
        }

        public Dictionary<string, LoanStatistics> ApplyPaymentsWithRollOver(List<Loan> loans, decimal additionalMoney, decimal initialDown,
            Func<List<LoanExt>, Queue<LoanExt>> selectLoanForRollOver)
        {
            var now = DateTime.Now;
            var year = now.Year;
            var month = now.Month;
            var day = now.Day;

            return ApplyPaymentsWithRollOver(year, month, day, 1, additionalMoney, initialDown, loans.Select(x => new LoanExt(x)).ToList(),
                selectLoanForRollOver);
        }

        private Dictionary<string, LoanStatistics> ApplyPaymentsWithRollOver(int startYear, int startMonth, int startDay, int paymentDay, decimal additionalMoney, decimal initialDown,
            List<LoanExt> loans, Func<List<LoanExt>, Queue<LoanExt>> selectLoanForRollOver )
        {
            var stats = loans.ToDictionary(x => x.Name, x => new LoanStatistics());
            var currentDate = new DateTime(startYear, startMonth, startDay);
            DateTime nextPaymentDay;
            if (currentDate.Day > paymentDay)
            {
                nextPaymentDay = startMonth == 12
                    ? new DateTime(startYear + 1, 1, paymentDay)
                    : new DateTime(startYear, startMonth, paymentDay);
            }
            else
            {
                nextPaymentDay = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day);
            }
            DisperseInitialDown(loans, initialDown, selectLoanForRollOver, ref additionalMoney );
            DisperseExtraMoney(loans, additionalMoney, nextPaymentDay, selectLoanForRollOver);
            while(loans.Any(x => x.PresentValue > 0))
            {
                decimal totalOverage = 0;
                foreach (var loan in loans)
                {
                    // Assuming allLoans payment date is after current date.
                    if (loan.PresentValue == 0)
                    {
                        stats[loan.Name][nextPaymentDay] = new LoanPeriodsStatistics(nextPaymentDay, 0, 0, 0);
                    }
                    else
                    {
                        decimal poi;
                        var fv = Fv(loan, currentDate, nextPaymentDay, out poi);

                        if ((fv < loan.TotalPayment && fv > 0) ||
                            (fv - loan.TotalPayment == 0))
                        {
                            totalOverage += fv;
                            loan.PresentValue = 0;
                            stats[loan.Name][nextPaymentDay] = new LoanPeriodsStatistics(nextPaymentDay, fv, 0, poi);
                        }
                        else
                        {
                            loan.PresentValue = fv - loan.TotalPayment;
                            stats[loan.Name][nextPaymentDay] = new LoanPeriodsStatistics(nextPaymentDay, loan.TotalPayment, loan.PresentValue, poi);
                            decimal overage;
                            if (NextPaymentIsFinal(loan, nextPaymentDay, out overage))
                            {
                                loan.TotalPayment -= overage;
                                totalOverage += overage;
                            }
                        }
                    }
                }

                // Rolling over payments that just went into loans that were paid off.
                DisperseExtraMoney(loans, totalOverage, nextPaymentDay, selectLoanForRollOver);
                currentDate = nextPaymentDay;
                nextPaymentDay = NextPaymentDay(nextPaymentDay);
            }
            return stats;
        }

        private void DisperseInitialDown(List<LoanExt> loans, decimal initialDown,
            Func<List<LoanExt>, Queue<LoanExt>> selectLoanForRollOver, ref decimal additionalMoney)
        {
            var pq = selectLoanForRollOver(loans);
            while (pq.Count > 0 && initialDown > 0)
            {
                var loan = pq.Dequeue();
                var dif = loan.PresentValue - initialDown;
                if (dif >= 0)
                {
                    loan.PresentValue -= initialDown;
                    initialDown = 0;
                }
                else if (initialDown > loan.PresentValue)
                {
                    var opv = loan.PresentValue;
                    loan.PresentValue = 0;
                    additionalMoney += loan.TotalPayment;
                    loan.TotalPayment = 0;
                    initialDown = initialDown - opv;
                }
            }
        }

        private static void DisperseExtraMoney(List<LoanExt> loans, decimal totalOverage,
            DateTime nextPaymentDay, Func<List<LoanExt>, Queue<LoanExt>> selectLoanForRollOver)
        {
            var pq = selectLoanForRollOver(loans);
            while (pq.Count > 0 && totalOverage > 0)
            {
                var loan = pq.Dequeue();
                decimal garbage;
                var fv = Fv(loan, nextPaymentDay, NextPaymentDay(nextPaymentDay), out garbage);
                var dif = fv - (loan.TotalPayment + totalOverage);
                if (dif >= 0)
                {
                    loan.TotalPayment += totalOverage;
                    totalOverage = 0;
                }
                else if (fv > loan.TotalPayment)
                {
                    var otp = loan.TotalPayment;
                    loan.TotalPayment = fv;
                    totalOverage = totalOverage - fv + otp;
                }
            }
        }

        private bool NextPaymentIsFinal(LoanExt loan, DateTime currentDay, out decimal overage)
        {
            var nextPaymentDay = NextPaymentDay(currentDay);
            decimal poi;
            var fv = Fv(loan, currentDay, nextPaymentDay, out poi);
            if ((fv < loan.TotalPayment))
            {
                overage = loan.TotalPayment - fv;
                return true;
            }
            overage = 0;
            return false;
        }

        private static DateTime NextPaymentDay(DateTime currentDay)
        {
            var nextPaymentDay = currentDay.Month == 12
                ? new DateTime(currentDay.Year + 1, 1, currentDay.Day)
                : new DateTime(currentDay.Year, currentDay.Month + 1, currentDay.Day);
            return nextPaymentDay;
        }

        private static decimal Fv(LoanExt loan, DateTime currentDay, DateTime nextPaymentDay, out decimal poi)
        {
            var daysOfInterest = (nextPaymentDay - currentDay).Days;
            var fv = Math.Round(Financial.FGivenP(loan.PresentValue, daysOfInterest, loan.DailyInterestRate), 2);
            poi = fv - loan.PresentValue;
            return fv;
        }
    }

    public class LoanExt
    {
        public LoanExt(Loan loan)
        {
            Name = loan.Name;
            DailyInterestRate = loan.DailyInterestRate;
            PresentValue = loan.PresentValue;
            TotalPayment = loan.AnnualWorth;
        }

        public string Name { get; set; }

        public decimal DailyInterestRate { get; set; }

        public decimal PresentValue { get; set; }

        public decimal TotalPayment { get; set; }
    }
}
