using BudgetTrackerApp.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetTrackerApp.Controllers
{
    public class HomeController : Controller
    {
        private MyBudgetTrackerAppEntities db = new MyBudgetTrackerAppEntities();

        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("Dashboard");
            }
            return View();
        }

        [Authorize]
        public ActionResult Dashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
            DashboardViewModel viewModel = new DashboardViewModel();
            if (checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                viewModel.Categories = db.Categories.Where(c => c.BudgetId == budgetId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToList();

            }
            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AdminDashboard()
        {

            return View();
        }

        // POST: CreateFeedback
        [HttpPost]
        public ActionResult CreateFeedback(bool isPublic, string comment)
        {
            if (ModelState.IsValid)
            {
                var feedback = new Feedback();
                feedback.IsTestimonial = isPublic;
                feedback.Message = comment;
                feedback.IsHidden = false;
                feedback.CreatedDate = DateTime.Now;
                feedback.UserId = User.Identity.GetUserId();
                db.Feedbacks.Add(feedback);
                db.SaveChanges();
            }
            return RedirectToAction("Dashboard");
        }

        // POST: CreateExpense
        [HttpPost]
        public ActionResult CreateExpense(DateTime date, string categoryId, string description, float amount)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var newExpense = new Expense();
                newExpense.CategoryId = Convert.ToInt32(categoryId);
                newExpense.BudgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                newExpense.Description = description;
                newExpense.Date = date;
                newExpense.Amount = (decimal)amount;
                db.Expenses.Add(newExpense);
                db.SaveChanges();
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public JsonResult getMonthlyExpenses()
        {
           
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var chartData = new List<object>();
            if (checkBudgetId())
            {
                chartData.Add(new object[]
                {
                    "Category", "Amount ($)"
                });
                var dateNow = DateTime.Now;
                var beginningOfMonth = dateNow.AddMonths(1 - dateNow.Day);
                var expenses = db.Expenses.Where(e => e.BudgetId == budgetId && e.Date > beginningOfMonth);
                var allCategories = expenses.Select(e => e.Category.Name).Distinct();
                allCategories.ToList().ForEach(data =>
                    chartData.Add(new object[]
                        {
                        data, expenses.Where(e => e.Category.Name == data).Sum(e => e.Amount)
                        })
                );
            }
            return Json(chartData);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        // Checks if user should have access to this budgetId
        private bool checkBudgetId()
        {
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var userId = User.Identity.GetUserId();
            var accountBudget = db.AccountBudgets.FirstOrDefault(ab => ab.BudgetId == budgetId && ab.UserId == userId);
            return accountBudget != null;
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