using BudgetTrackerApp.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace BudgetTrackerApp.Controllers
{
    [Authorize(Roles = "User")]
    public class BudgetController : Controller
    {
        private MyBudgetTrackerAppEntities db = new MyBudgetTrackerAppEntities();

        // GET: Expenses
        public ActionResult Expenses()
        {
            var viewModel = new ExpensesViewModel();
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
            var viewModel = new IncomeViewModel();
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
            var viewModel = new StatisticsViewModel();
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

        [HttpPost]
        public JsonResult getExpensesUserCategories(DateTime? dateRangeStart, DateTime? dateRangeEnd, int[] categories)
        {

            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var chartData = new List<object>();
            if (checkBudgetId())
            {
                chartData.Add(new object[]
                {
                    "Category", "Amount ($)"
                });
                var expenses = db.Expenses.Where(e => e.BudgetId == budgetId);

                if (dateRangeStart != null)
                {
                    expenses = db.Expenses.Where(e => e.Date > dateRangeStart);
                }
                if (dateRangeEnd != null)
                {
                    expenses = db.Expenses.Where(e => e.Date < dateRangeEnd);
                }
                if (categories != null)
                {
                    expenses = expenses.Where(e => categories.Contains(e.CategoryId));
                }
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
        public JsonResult getNetOverTime(DateTime? dateRangeStart, DateTime? dateRangeEnd, string timeInterval)
        {
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var chartData = new List<object>();
            if (checkBudgetId())
            {
                chartData.Add(new object[]
                {
                    "Date", "Net ($)"
                });
                var expenses = db.Expenses.Where(e => e.BudgetId == budgetId);

                var income = db.Incomes.Where(i => i.BudgetId == budgetId);

                if (dateRangeStart == null)
                {
                    var earliestExpenseDate = expenses.Min(e => e.Date);
                    var earliestIncomeDate = income.Min(e => e.Date);
                    dateRangeStart = (earliestExpenseDate < earliestIncomeDate ? earliestExpenseDate : earliestIncomeDate);
                }

                if (dateRangeEnd == null)
                {
                    var latestExpenseDate = expenses.Max(e => e.Date);
                    var latestIncomeDate = income.Max(e => e.Date);
                    dateRangeEnd = (latestExpenseDate > latestIncomeDate ? latestExpenseDate : latestIncomeDate);
                }

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

                if (timeInterval == "daily")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(1))
                    {
                        selectedDates.Add(date);
                    }

                    var net = new decimal();
                    selectedDates.ForEach(date =>
                    {
                        net += groupedIncome.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                        net -= groupedExpenses.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), net
                            });
                    });
                }
                else if (timeInterval == "weekly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(7))
                    {
                        selectedDates.Add(date);
                    }

                    var net = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddDays(7);
                        net += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        net -= groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), net
                            });
                    });
                }
                else if (timeInterval == "monthly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddMonths(1))
                    {
                        selectedDates.Add(date);
                    }

                    var net = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddMonths(1);
                        net += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        net -= groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), net
                            });
                    });
                }
                else if (timeInterval == "yearly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddYears(1))
                    {
                        selectedDates.Add(date);
                    }

                    var net = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddYears(1);
                        net += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        net -= groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), net
                            });
                    });
                }
            }
            return Json(chartData);
        }

        [HttpPost]
        public JsonResult GetExpensesIncomeOverTime(DateTime? dateRangeStart, DateTime? dateRangeEnd, string timeInterval, bool showIncome, bool showExpenses)
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

                if (dateRangeStart == null)
                {
                    var earliestExpenseDate = expenses.Min(e => e.Date);
                    var earliestIncomeDate = income.Min(e => e.Date);
                    dateRangeStart = (earliestExpenseDate < earliestIncomeDate ? earliestExpenseDate : earliestIncomeDate);
                }

                if (dateRangeEnd == null)
                {
                    var latestExpenseDate = expenses.Max(e => e.Date);
                    var latestIncomeDate = income.Max(e => e.Date);
                    dateRangeEnd = (latestExpenseDate > latestIncomeDate ? latestExpenseDate : latestIncomeDate);
                }

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

                if (timeInterval == "daily")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(1))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    selectedDates.ForEach(date =>
                    {
                        if (showIncome)
                            totalIncome += groupedIncome.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                        if (showExpenses)
                            totalExpenses += groupedExpenses.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), totalIncome, totalExpenses
                            });
                    });
                }
                else if (timeInterval == "weekly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(7))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddDays(7);
                        if (showIncome)
                            totalIncome += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        if (showExpenses)
                            totalExpenses += groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), totalIncome, totalExpenses
                            });
                    });
                }
                else if (timeInterval == "monthly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddMonths(1))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddMonths(1);
                        if (showIncome)
                            totalIncome += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        if (showExpenses)
                            totalExpenses += groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), totalIncome, totalExpenses
                            });
                    });
                }
                else if (timeInterval == "yearly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddYears(1))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddYears(1);
                        if (showIncome)
                            totalIncome += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        if (showExpenses)
                            totalExpenses += groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                                date.ToString("dd/MM/yyyy"), totalIncome, totalExpenses
                            });
                    });
                }
            }
            return Json(chartData);
        }

        [HttpPost]
        public JsonResult GetDatatable(DateTime? dateRangeStart, DateTime? dateRangeEnd, string timeInterval)
        {
            var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
            var chartData = new List<object>();
            if (checkBudgetId())
            {
                chartData.Add(new object[]
                {
                    "Date", "Income ($)", "Expenses ($)", "Net ($)"
                });

                var expenses = db.Expenses.Where(e => e.BudgetId == budgetId);

                var income = db.Incomes.Where(i => i.BudgetId == budgetId);

                if (dateRangeStart == null)
                {
                    var earliestExpenseDate = expenses.Min(e => e.Date);
                    var earliestIncomeDate = income.Min(e => e.Date);
                    dateRangeStart = (earliestExpenseDate < earliestIncomeDate ? earliestExpenseDate : earliestIncomeDate);
                }

                if (dateRangeEnd == null)
                {
                    var latestExpenseDate = expenses.Max(e => e.Date);
                    var latestIncomeDate = income.Max(e => e.Date);
                    dateRangeEnd = (latestExpenseDate > latestIncomeDate ? latestExpenseDate : latestIncomeDate);
                }

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

                if (timeInterval == "daily")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(1))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    selectedDates.ForEach(date =>
                    {
                        totalIncome = groupedIncome.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                        totalExpenses = groupedExpenses.FirstOrDefault(gi => gi.Date == date)?.Amount ?? 0;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy"), totalIncome, totalExpenses, (totalIncome - totalExpenses)
                            });
                    });
                }
                else if (timeInterval == "weekly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddDays(7))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddDays(7);
                        totalIncome += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        totalExpenses += groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy") + " to " + intervalEndDate.AddDays(-1).ToString("dd/MM/yyyy"), totalIncome, totalExpenses, (totalIncome - totalExpenses)
                            });
                    });
                }
                else if (timeInterval == "monthly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddMonths(1))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddMonths(1);
                        totalIncome += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        totalExpenses += groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                        date.ToString("dd/MM/yyyy") + " to " + intervalEndDate.AddDays(-1).ToString("dd/MM/yyyy"), totalIncome, totalExpenses, (totalIncome - totalExpenses)
                            });
                    });
                }
                else if (timeInterval == "yearly")
                {
                    var selectedDates = new List<DateTime>();

                    for (var date = (DateTime)dateRangeStart; date <= dateRangeEnd; date = date.AddYears(1))
                    {
                        selectedDates.Add(date);
                    }

                    var totalIncome = new decimal();
                    var totalExpenses = new decimal();
                    var intervalEndDate = new DateTime();
                    selectedDates.ForEach(date =>
                    {
                        intervalEndDate = date.AddYears(1);
                        totalIncome += groupedIncome.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        totalExpenses += groupedExpenses.Where(gi => gi.Date >= date && gi.Date < intervalEndDate).Sum(gi => (decimal?)gi.Amount) ?? Decimal.Zero;
                        chartData.Add(new object[]
                            {
                                date.ToString("dd/MM/yyyy") + " to " + intervalEndDate.AddDays(-1).ToString("dd/MM/yyyy"), totalIncome, totalExpenses, (totalIncome - totalExpenses)
                            });
                    });
                }
            }
            return Json(chartData);
        }

        // GET: Calculator
        public ActionResult Calculator()
        {
            var viewModel = new CalculatorsViewModel();
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
                var newExpense = new Expense();
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
                var income = new Income();
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
                var editExpense = db.Expenses.SingleOrDefault(e => e.ExpenseId == expenseId && e.BudgetId == budgetId);
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
                var income = db.Incomes.SingleOrDefault(e => e.IncomeId == incomeId && e.BudgetId == budgetId);
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
                var expense = db.Expenses.SingleOrDefault(e => e.ExpenseId == expenseId && e.BudgetId == budgetId);
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
                var income = db.Incomes.SingleOrDefault(e => e.IncomeId == incomeId && e.BudgetId == budgetId);
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
                var newCategory = new Category();
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
                var editCategory = db.Categories.Single(c => c.BudgetId == budgetId && c.CategoryId == categoryIdConverted);
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
                var deleteCategory = db.Categories.FirstOrDefault(c => c.BudgetId == budgetId && c.CategoryId == categoryIdConverted);
                if (deleteCategory != null)
                {
                    db.Categories.Remove(deleteCategory);
                    db.SaveChanges();
                }
            }
            return RedirectToAction("Expenses");
        }

        
        public void ExportExpensesToExcel()
        {
            if (checkBudgetId())
            {
                var gridView = new GridView();
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                gridView.DataSource = db.Expenses.Where(e => e.BudgetId == budgetId).OrderByDescending(e => e.Date).Select(data => new {
                    Description = data.Description,
                    Category = data.Category.Name,
                    Date = data.Date,
                    Amount = data.Amount
                }).ToList();
                gridView.DataBind();
                Response.ClearContent();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", "attachment; filename=Expenses.xls");
                Response.ContentType = "application/ms-excel";
                Response.Charset = "";
                StringWriter stringWriter = new StringWriter();
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter);
                gridView.RenderControl(htmlTextWriter);
                Response.Output.Write(stringWriter.ToString());
                Response.Flush();
                Response.End();
            }
        }

        public void ExportIncomeToExcel()
        {
            if (checkBudgetId())
            {
                var gridView = new GridView();
                var budgetId = Convert.ToInt32(Request.Cookies["BudgetId"].Value);
                gridView.DataSource = db.Incomes.Where(e => e.BudgetId == budgetId).OrderByDescending(e => e.Date).Select(data => new {
                    Description = data.Description,
                    Date = data.Date,
                    Amount = data.Amount
                }).ToList();
                gridView.DataBind();
                Response.ClearContent();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", "attachment; filename=Income.xls");
                Response.ContentType = "application/ms-excel";
                Response.Charset = "";
                StringWriter stringWriter = new StringWriter();
                HtmlTextWriter htmlTextWriter = new HtmlTextWriter(stringWriter);
                gridView.RenderControl(htmlTextWriter);
                Response.Output.Write(stringWriter.ToString());
                Response.Flush();
                Response.End();
            }
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