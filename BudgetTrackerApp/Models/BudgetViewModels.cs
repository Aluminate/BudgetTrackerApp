﻿using System;
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
    } 
}