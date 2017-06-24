namespace Nu.Loans.BO
{
    public class Loan
    {
        public string Name { get; set; }

        public decimal AnnualPercentageRate { get; set; }

        public decimal MonthlyInterestRate { get; }

        public decimal DailyInterestRate { get; }

        public decimal PresentValue { get; set; }

        public decimal AnnualWorth { get; set; }

        public Loan(string name, decimal annualPercentageRate, decimal presentValue, decimal annualWorth)
        {
            Name = name;
            AnnualPercentageRate = annualPercentageRate;
            PresentValue = presentValue;
            AnnualWorth = annualWorth;
            MonthlyInterestRate = AnnualPercentageRate/12;
            DailyInterestRate = AnnualPercentageRate/365;
        }
    }
}
