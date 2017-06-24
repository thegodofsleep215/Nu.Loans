using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Xml;
using loanCalculator.Dal;
using Nu.CommandLine;
using Nu.CommandLine.Attributes;
using Nu.CommandLine.Communication;
using Nu.ConsoleArguments;
using Nu.Loans.BL;
using Nu.Loans.BO;

namespace loanCalculator
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var parsedArgs = ConsoleArguments.Parse(args);
            var console = new ConsoleCommunicator();
            var cp = new CommandProcessor(console);
            cp.RegisterObject(new LoanCommands(CreateLoanStore()));
            cp.Start();
            Console.WriteLine(console.SendCommand(parsedArgs.UnnamedArguments[0], parsedArgs.NamedArguments));
        }

        private static ILoanStore CreateLoanStore()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                       "\\loanCalculator\\loanStore.json";
            return new JsonFileLoanStore(path);
        }

        private static void RandomCrap()
        {
//var loan = new Loan("4652 Sig Loan", (decimal) .0525, (decimal) 3630.07, (decimal) 47.22);
            var loan = new Loan("Van Loan", (decimal) .0449, (decimal) 6653.54, (decimal) 500);
            var bl = new LoanPayoff();

            var currentDate = new DateTime(2016, 11, 27);
            var payOffDate = new DateTime(2018, 1, 09);
            LoanStatistics stats;
            var payments = Payments2(payOffDate, currentDate, loan, 8);
            var finalLoan = bl.ApplyPayments(loan, currentDate, payments, out stats);
            //288.49

            decimal finalPayment;
            decimal annualWorth = 500;
            var actualN =
                Financial.FindN(
                    n =>
                        Financial.FGivenP((decimal) 6653.54, n, loan.MonthlyInterestRate) -
                        Financial.FGivenA(annualWorth, n, loan.MonthlyInterestRate), 0, annualWorth, out finalPayment);
            var fa = Financial.FGivenA(loan, 94, l => l.MonthlyInterestRate);
            var fp = Financial.FGivenP(loan, 94, l => l.MonthlyInterestRate);
            var fv = fp - fa;
        }

        private static LoanPayments Payments(DateTime payOffDate, DateTime currentDate, Loan loan, int paymentDay)
        {
            var payments = Enumerable.Range(0, (payOffDate - currentDate).Days + 1)
                .Select(day => currentDate.AddDays(day))
                .Where(x => x.Day == paymentDay)
                .Select(x => new LoanPayment {Year = x.Year, Month = x.Month, Day = x.Day, Payment = loan.AnnualWorth})
                .ToDictionary(x => x.Date, x => x);
            return new LoanPayments(payments);
        }

        private static LoanPayments Payments2(DateTime payOffDate, DateTime currentDate, Loan loan, int paymentDay)
        {
            bool done = false;
            var year = currentDate.Year;
            var month = currentDate.Month;
            var day = currentDate.Day;
            List<DateTime> paymentDates = new List<DateTime>();
            while (!done)
            {
                var date = new DateTime(year, month, paymentDay);
                if (date > payOffDate)
                {
                    done = true;
                }
                else if (date >= currentDate)
                {
                    paymentDates.Add(date);

                }

                if (month + 1 == 13)
                {
                    month = 1;
                    year++;
                }
                else
                {
                    month++;
                }
            }
            return
                new LoanPayments(paymentDates.ToDictionary(dt => dt,
                    dt => new LoanPayment {Year = dt.Year, Month = dt.Month, Day = dt.Day, Payment = loan.AnnualWorth}));
        }
    }


    internal class LoanCommands
    {
        private readonly ILoanStore loanStore;

        public LoanCommands(ILoanStore loanStore)
        {

            this.loanStore = loanStore;
        }

        [TypedCommand("insert", "")]
        public string Insert(string name, decimal apr, decimal pv, decimal aw)
        {
            return loanStore.Save(new Loan(name, apr, pv, aw)).ToString();
        }

        [TypedCommand("import", "")]
        public string Import(string file)
        {
            if (!File.Exists(file)) return "File not found.";
            using (var s = File.OpenRead(file))
            {
                var foo = Csv.CsvReader.ReadFromStream(s);
                var loans =
                    foo.Select(x => x.Headers.ToDictionary(h => h.ToUpper(), h => x[h])).Select(CsvToLoan).ToList();
                //var line = s.ReadLine();
                //if (line == null) return "Done, nothing to impot.";
                //var header = line.Split(',');

                //List<Loan> loans = new List<Loan>();
                //while (!s.EndOfStream)
                //{
                //    line = s.ReadLine();
                //    if (line == null) continue;
                //    var values = line.Split(',');
                //    loans.Add(CsvToLoan(header, values));
                //}

                if (loans.Count > 0) loanStore.Save(loans);
                return $"Imported {loans.Count} loans.";
            }
        }

        private Loan CsvToLoan(Dictionary<string, string> dict)
        {
            var apr = decimal.Parse(dict["APR"].Replace("%", ""))/100;
            var pv = decimal.Parse(dict["PV"].Replace("$", "").Replace(",", ""));
            var aw = decimal.Parse(dict["AW"].Replace("$", "").Replace(",", ""));
            return new Loan(dict["NAME"], apr, pv, aw);
        }

        [TypedCommand("payoff", "")]
        public string Payoff(string baseFile, decimal additionalMoney, decimal initialDown)
        {
            var loans = loanStore.Read(x => true);
            var lp = new LoanPayoff();
            //var stats = lp.ApplyPaymentsWithRollOver(loans, additionalMoney, initialDown, HighestInterest);
            var stats = lp.ApplyPayments(loans, additionalMoney, initialDown);
            CsvReportEngine.PreseventValueByMonth(stats, $"{baseFile}_pvByMonth.csv");
            CsvReportEngine.PaymentByMonth(stats, $"{baseFile}_paymentMonth.csv");
            return "done";
        }

        private Queue<LoanExt> HighestInterest(List<LoanExt> loans)
        {
            var queue = new Queue<LoanExt>();
            loans.Where(x => x.PresentValue > 0).OrderByDescending(x => x.DailyInterestRate).ToList().ForEach(queue.Enqueue);
            return queue;
        }

        private Queue<LoanExt> LowestPv(List<LoanExt> loans)
        {
            
            var queue = new Queue<LoanExt>();
            loans.Where(x => x.PresentValue > 0).OrderBy(x => x.PresentValue).ToList().ForEach(queue.Enqueue);
            return queue;
        }
    }

    internal class CsvReportEngine
    {
        public static void PreseventValueByMonth(Dictionary<string, LoanStatistics> stats, string file)
        {
            var dates = stats.Values.SelectMany(x => x.Keys).Distinct();
            var header = "Date," + string.Join(",", stats.Keys.Select(x => $"\"{x}\""));
            Func<string, DateTime, decimal> f = (h, dt) => stats[h].ContainsKey(dt) ? stats[h][dt].PresentValue : 0;
            var rows = dates.Select(dt => $"\"{dt.ToShortDateString()}\"," + string.Join(",", stats.Keys.Select(h => $"\"{f(h, dt)}\""))).ToList();

            using(var s = new StreamWriter(File.Create(file)))
            {
                s.WriteLine(header);
                rows.ForEach(s.WriteLine);
                s.Flush();
            }
        }

        public static void PaymentByMonth(Dictionary<string, LoanStatistics> stats, string file)
        {
            var dates = stats.Values.SelectMany(x => x.Keys).Distinct();
            var header = "Date," + string.Join(",", stats.Keys.Select(x => $"\"{x}\""));
            Func<string, DateTime, decimal> f = (h, dt) => stats[h].ContainsKey(dt) ? stats[h][dt].Payment : 0;
            var rows = dates.Select(dt => $"\"{dt.ToShortDateString()}\"," + string.Join(",", stats.Keys.Select(h => $"\"{f(h, dt)}\""))).ToList();

            using(var s = new StreamWriter(File.Create(file)))
            {
                s.WriteLine(header);
                rows.ForEach(s.WriteLine);
                s.Flush();
            }
        }
    }
}
