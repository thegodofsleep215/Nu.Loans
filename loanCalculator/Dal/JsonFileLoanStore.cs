using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Nu.Loans.BO;

namespace loanCalculator.Dal
{
    class JsonFileLoanStore : ILoanStore
    {
        private readonly string file;

        private List<LoanDto> loanCache;

        public JsonFileLoanStore(string file)
        {
            loanCache = new List<LoanDto>();
            this.file = file;
            var dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            ReadStore();

        }

        private void ReadStore()
        {
            if (File.Exists(file))
            {
                using (var stream = new StreamReader(File.OpenRead(file))) ReadStore(stream);
            }
            else
            {
                File.Create(file).Close();
            }
        }

        private void ReadStore(StreamReader stream)
        {
            var contents = stream.ReadToEnd();
            loanCache = JsonConvert.DeserializeObject<List<LoanDto>>(contents) ?? new List<LoanDto>();
        }

        private void WriteStore()
        {
            File.Delete(file);
            using (var stream = new StreamWriter(File.Create(file)))
            {
                stream.WriteLine(JsonConvert.SerializeObject(loanCache));
            }
        }

        public int Save(Loan loan)
        {
            var max = NextIdent();
            loanCache.Add(new LoanDto {Ident = max + 1, Loan = loan});
            WriteStore();
            return max + 1;
        }

        private int NextIdent()
        {
            var max = loanCache.Count == 0 ? 1 : loanCache.Max(x => x.Ident);
            return max;
        }

        public void Save(List<Loan> loans)
        {
            loans.ForEach(x => loanCache.Add(new LoanDto {Ident = NextIdent(), Loan = x}));
            WriteStore();
        }

        public List<Loan> Read(Func<Loan, bool> whereClause)
        {
            // lazy way of copying.
            var json = JsonConvert.SerializeObject(loanCache.Select(x => x.Loan).Where(whereClause));
            return JsonConvert.DeserializeObject<List<Loan>>(json);
        }
    }
}