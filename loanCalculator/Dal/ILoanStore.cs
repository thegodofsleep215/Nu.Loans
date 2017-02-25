using System;
using System.Collections.Generic;
using Nu.Loans.BO;

namespace loanCalculator.Dal
{
    interface ILoanStore
    {
        int Save(Loan loan);
        void Save(List<Loan> loan);

        List<Loan> Read(Func<Loan, bool> whereClause);
    }
}