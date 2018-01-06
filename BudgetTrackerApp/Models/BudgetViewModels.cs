using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetTrackerApp.Models
{
    public class ExpensesViewModel
    {
        public IEnumerable<SelectListItem> Categories { get; set; }
        public List<Expense> Expenses { get; set; }
        public List<Picture> Pictures { get; set; }
        public class Picture
        {
            public Picture(string PictureUrl, int expenseId)
            {
                this.PictureUrl = PictureUrl;
                this.expenseId = expenseId;
            }

            public string PictureUrl { get; set; }
            public int expenseId { get; set; }
        }
    }

    public class IncomeViewModel
    {
        public IEnumerable<SelectListItem> PeriodicIncome { get; set; }
        public List<Income> IncomeList { get; set; }
    }

    public class CalculatorsViewModel
    {
        public IEnumerable<SelectListItem> Categories { get; set; }
    }

    public class StatisticsViewModel
    {
        public IEnumerable<SelectListItem> Categories { get; set; }
        public bool IsEnabled { get; set; }
    }
}