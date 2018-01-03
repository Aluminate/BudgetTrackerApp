using BudgetTrackerApp.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

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
            var viewModel = new HomeIndexViewModel();
            var context = new ApplicationDbContext();
            var allUsers = context.Users.ToList();
            var testimonials = db.Feedbacks.Where(f => f.IsTestimonial == true && f.IsHidden == false).OrderByDescending(f => f.CreatedDate).ToList();
            var testimonialList = new List<HomeIndexViewModel.Testimonial>();
            testimonials.ForEach(data =>
            {
                var user = allUsers.Single(au => au.Id == data.UserId);
                var nameOfUser = user.FirstName + " " + user.LastName;
                testimonialList.Add(new HomeIndexViewModel.Testimonial(data.Message, nameOfUser));
            });
            viewModel.Testimonials = testimonialList;
            return View(viewModel);
        }

        [Authorize]
        public ActionResult Dashboard()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
            var viewModel = new DashboardViewModel();
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
            var viewModel = new AdminDashboardViewModel();
            var context = new ApplicationDbContext();
            var currentYear = DateTime.Now.Year;
            var currentMonth = DateTime.Now.Month;

            var adminId = User.Identity.GetUserId();
            var allUsers = context.Users.Where(u => u.Id != adminId).ToList();

            var adminAccount = (System.Security.Claims.ClaimsIdentity)User.Identity;

            viewModel.totalUsers = allUsers.Count;
            viewModel.registrationsThisMonth = allUsers.Where(au => au.CreatedDate.Year == currentYear && au.CreatedDate.Month == currentMonth).Count();

            return View(viewModel);
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