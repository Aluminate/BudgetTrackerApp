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

        // GET: Expenses
        public ActionResult Expenses()
        {
            return View();
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
    }
}