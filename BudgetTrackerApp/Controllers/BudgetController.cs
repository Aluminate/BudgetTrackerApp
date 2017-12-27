using BudgetTrackerApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetTrackerApp.Controllers
{
    [Authorize]
    public class BudgetController : Controller
    {
        private MyBudgetTrackerAppEntities db = new MyBudgetTrackerAppEntities();

        // GET: Expenses
        public ActionResult Expenses()
        {
            ExpensesViewModel viewModel = new ExpensesViewModel();
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            viewModel.Categories = db.Categories.Where(c => c.BudgetId == budgetId).ToList();
            return View(viewModel);
        }

        // GET: Income
        public ActionResult Income()
        {
            return View();
        }

        // GET: Statistics
        public ActionResult Statistics()
        {
            return View();
        }

        // GET: Calculator
        public ActionResult Calculator()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}