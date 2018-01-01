using BudgetTrackerApp.Models;
using Microsoft.AspNet.Identity;
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
            if (checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                viewModel.Categories = db.Categories.Where(c => c.BudgetId == budgetId)
                    .Select(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.Name
                    }).ToList();
                viewModel.Expenses = db.Expenses.Where(e => e.BudgetId == budgetId).OrderByDescending(e => e.Date).ToList();
            }
            return View(viewModel);
        }

        // GET: Income
        public ActionResult Income()
        {
            IncomeViewModel viewModel = new IncomeViewModel();
            if (checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                viewModel.PeriodicIncome = db.PeriodicIncomes.Where(c => c.BudgetId == budgetId)
                    .Select(pi => new SelectListItem
                    {
                        Value = pi.PeriodicIncomeId.ToString(),
                        Text = pi.Description
                    }).ToList();
                viewModel.IncomeList = db.Incomes.Where(i => i.BudgetId == budgetId).OrderByDescending(i => i.Date).ToList();
            }
            return View(viewModel);
        }

        // GET: Statistics
        public ActionResult Statistics()
        {
            return View();
        }

        // GET: Calculator
        public ActionResult Calculator()
        {
            CalculatorsViewModel viewModel = new CalculatorsViewModel();
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

        // POST: CreateExpense
        [HttpPost]
        public ActionResult CreateExpense(DateTime date, string categoryId, string description, float amount)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                Expense newExpense = new Expense();
                newExpense.CategoryId = Convert.ToInt32(categoryId);
                newExpense.BudgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                newExpense.Description = description;
                newExpense.Date = date;
                newExpense.Amount = (decimal)amount;
                db.Expenses.Add(newExpense);
                db.SaveChanges();
            }
            return RedirectToAction("Expenses");
        }

        // POST: CreateIncome
        [HttpPost]
        public ActionResult CreateIncome(DateTime date, string description, float amount)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                Income income = new Income();
                income.BudgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                income.Description = description;
                income.Date = date;
                income.Amount = (decimal)amount;
                db.Incomes.Add(income);
                db.SaveChanges();
            }
            return RedirectToAction("Income");
        }

        // POST: EditExpense
        [HttpPost]
        public ActionResult EditExpense(int expenseId, DateTime date, string categoryId, string description, float amount)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                Expense editExpense = db.Expenses.SingleOrDefault(e => e.ExpenseId == expenseId && e.BudgetId == budgetId);
                editExpense.CategoryId = Convert.ToInt32(categoryId);
                editExpense.BudgetId = budgetId;
                editExpense.Description = description;
                editExpense.Date = date;
                editExpense.Amount = (decimal)amount;
                db.SaveChanges();
            }
            return RedirectToAction("Expenses");
        }

        // POST: EditIncome
        [HttpPost]
        public ActionResult EditIncome(int incomeId, DateTime date, string description, float amount)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                Income income = db.Incomes.SingleOrDefault(e => e.IncomeId == incomeId && e.BudgetId == budgetId);
                income.BudgetId = budgetId;
                income.Description = description;
                income.Date = date;
                income.Amount = (decimal)amount;
                db.SaveChanges();
            }
            return RedirectToAction("Income");
        }

        // POST: DeleteExpense
        [HttpPost]
        public ActionResult DeleteExpense(int expenseId)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                Expense expense = db.Expenses.SingleOrDefault(e => e.ExpenseId == expenseId && e.BudgetId == budgetId);
                db.Expenses.Remove(expense);
                db.SaveChanges();
            }
            return RedirectToAction("Expenses");
        }

        // POST: DeleteIncome
        [HttpPost]
        public ActionResult DeleteIncome(int incomeId)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                Income income = db.Incomes.SingleOrDefault(e => e.IncomeId == incomeId && e.BudgetId == budgetId);
                db.Incomes.Remove(income);
                db.SaveChanges();
            }
            return RedirectToAction("Income");
        }

        // POST: CreateCategory
        [HttpPost]
        public ActionResult CreateCategory(string categoryName)
        {
            if (ModelState.IsValid && checkBudgetId())
            {
                Category newCategory = new Category();
                newCategory.Name = categoryName;
                newCategory.BudgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                db.Categories.Add(newCategory);
                db.SaveChanges();
            }
            return RedirectToAction("Expenses");
        }

        // POST: EditCategory
        [HttpPost]
        public ActionResult EditCategory(string categoryId, string categoryName)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(categoryId) && checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                var categoryIdConverted = Convert.ToInt32(categoryId);
                Category editCategory = db.Categories.Single(c => c.BudgetId == budgetId && c.CategoryId == categoryIdConverted);
                editCategory.Name = categoryName;
                db.SaveChanges();
            }
            return RedirectToAction("Expenses");
        }

        // POST: DeleteCategory
        [HttpPost]
        public ActionResult DeleteCategory(string categoryId)
        {
            if (ModelState.IsValid && !string.IsNullOrEmpty(categoryId) && checkBudgetId())
            {
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                var categoryIdConverted = Convert.ToInt32(categoryId);
                Category deleteCategory = db.Categories.FirstOrDefault(c => c.BudgetId == budgetId && c.CategoryId == categoryIdConverted);
                if (deleteCategory != null)
                {
                    db.Categories.Remove(deleteCategory);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Expenses");
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