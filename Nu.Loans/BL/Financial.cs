using System;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Nu.Loans.BO;

namespace Nu.Loans.BL
{
    public static class Financial
    {
        public static decimal AGivenP(Loan loan, int periods, Func<Loan, decimal> interestRate)
        {
            return AGivenP(loan.PresentValue, periods, interestRate(loan));

        }

        public static decimal AGivenP(decimal presentValue, int periods, decimal interestRate)
        {
            var factor = InterestToPeriodFactor(interestRate, periods);
            var numerator = presentValue*interestRate*factor;
            var denomenator = factor - 1;
            return numerator/denomenator;
        }

        public static decimal FGivenP(Loan loan, int periods, Func<Loan, decimal> interestRate)
        {
            return FGivenP(loan.PresentValue, periods, interestRate(loan));
        }

        public static decimal FGivenP(decimal presentValue, int periods, decimal interestRate)
        {
            var factor = InterestToPeriodFactor(interestRate, periods);
            return presentValue*factor;
        }

        public static decimal FGivenA(Loan loan, int periods, Func<Loan, decimal> interestRate)
        {
            return FGivenA(loan.AnnualWorth, periods, interestRate(loan));
        }

        public static decimal FGivenA(decimal annualWorth, int periods, decimal interestRate)
        {
            var factor = InterestToPeriodFactor(interestRate, periods);
            return annualWorth*((factor - 1)/interestRate);
        }


        public static int FindN(Func<int, decimal> loanFunc, decimal targetValue, decimal annualWorth, out decimal finalPayment)
        {
            var a = 1;
            var b = 5;

            // Find a good b value
            while (loanFunc(b) > targetValue)
            {
                b *= 2;
            }

            while (true)
            {
                var n = (b/2) + (a/2);
                var result = loanFunc(n);
                finalPayment = result - targetValue;
                var dif = Math.Abs(finalPayment);
                if (dif <= annualWorth)
                    return dif > 0 ? n+1 : n;
                if (result > targetValue) a = n;
                else b = n;
            }
        }

        private static decimal InterestToPeriodFactor(decimal interestRate, int periods)
        {
            return Pow(1 + interestRate, periods);
        }

        private static decimal Pow(decimal x, int y)
        {
            decimal value = x;

            for (int i = 1; i < y; i++)
            {
                value *= x;
            }
            return value;
        }
    }
}