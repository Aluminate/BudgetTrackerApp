﻿using BudgetTrackerApp.Models;
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
                var budgetGoal = db.BudgetGoals.SingleOrDefault(bg => bg.BudgetId == budgetId);
                if (budgetGoal != null)
                {
                    viewModel.progressBarEnabled = budgetGoal.IsProgressBarEnabled;
                    var currentYear = DateTime.Now.Year;
                    var currentMonth = DateTime.Now.Month;
                    var expenses = db.Expenses.Where(e => e.BudgetId == budgetId && e.Date.Year == currentYear && e.Date.Month == currentMonth);
                    var monthlyExpenses = Decimal.Zero;
                    if (expenses.Count() > 0)
                        monthlyExpenses = expenses.Sum(e => e.Amount);
                    viewModel.progressBarPercentage = monthlyExpenses / budgetGoal.BudgetAmount * 100;
                    viewModel.progressBarText = $"${monthlyExpenses.ToString()} / ${budgetGoal.BudgetAmount.ToString()}";
                }
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

            viewModel.totalUsers = allUsers.Count;
            viewModel.registrationsThisMonth = allUsers.Where(au => au.CreatedDate.Year == currentYear && au.CreatedDate.Month == currentMonth).Count();

            return View(viewModel);
        }

        // POST: CreateFeedback
        [HttpPost]
        [Authorize(Roles = "User")]
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
        [Authorize(Roles = "User")]
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
        [Authorize(Roles = "User")]
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
                var currentYear = DateTime.Now.Year;
                var currentMonth = DateTime.Now.Month;
                var expenses = db.Expenses.Where(e => e.BudgetId == budgetId && e.Date.Year == currentYear && e.Date.Month == currentMonth);
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult getMonthlyRegistrations()
        {
            var context = new ApplicationDbContext();
            var adminId = User.Identity.GetUserId();
            var allUsers = context.Users.Where(u => u.Id != adminId).ToList();

            var dateRangeStart = allUsers.Min(au => au.CreatedDate);
            var dateRangeEnd = allUsers.Max(au => au.CreatedDate);
            // set to the first day of month
            dateRangeStart = new DateTime(dateRangeStart.Year, dateRangeStart.Month, 1);
            dateRangeEnd = new DateTime(dateRangeEnd.Year, dateRangeEnd.Month, 1);

            var chartData = new List<object>();

            chartData.Add(new object[]
            {
                "Registrations", "Total Registrations"
            });

            var selectedDates = new List<DateTime>();

            for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddMonths(1))
            {
                selectedDates.Add(date);
            }

            var intervalEndDate = new DateTime();
            selectedDates.ForEach(date =>
            {
                intervalEndDate = date.AddMonths(1);
                chartData.Add(new object[]
                    {
                        date.ToString("MMM yyyy"), allUsers.Where(au => au.CreatedDate >= date && au.CreatedDate < intervalEndDate).Count()
                    });
            });


            return Json(chartData);
        }

        [HttpPost]
        [Authorize(Roles = "User")]
        public JsonResult getMonthlyExpensesIncome()
        {
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var chartData = new List<object>();
            if (checkBudgetId())
            {
                chartData.Add(new object[]
                {
                    "Date", "Income", "Expenses"
                });

                var expenses = db.Expenses.Where(e => e.BudgetId == budgetId);

                var income = db.Incomes.Where(i => i.BudgetId == budgetId);


                var dateRangeStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var dateRangeEnd = DateTime.Now;

                var groupedExpenses = expenses
                    .GroupBy(e => e.Date)
                    .Select(data => new
                    {
                        Date = (DateTime)data.Key,
                        Amount = data.Sum(d => d.Amount)
                    }).ToList();

                var groupedIncome = income
                    .GroupBy(e => e.Date)
                    .Select(data => new
                    {
                        Date = (DateTime)data.Key,
                        Amount = data.Sum(d => d.Amount)
                    }).ToList();

                
                var selectedDates = new List<DateTime>();

                for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(1))
                {
                    selectedDates.Add(date);
                }

                var totalIncome = new decimal();
                var totalExpenses = new decimal();
                selectedDates.ForEach(date =>
                {
                    totalIncome += groupedIncome.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                    totalExpenses += groupedExpenses.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                    chartData.Add(new object[]
                        {
                    date.ToString("yyyy/MM/dd"), totalIncome, totalExpenses
                        });
                });
                
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