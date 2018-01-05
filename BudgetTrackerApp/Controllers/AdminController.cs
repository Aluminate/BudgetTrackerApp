using BudgetTrackerApp.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetTrackerApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private MyBudgetTrackerAppEntities db = new MyBudgetTrackerAppEntities();

        // GET: SiteSettings
        public ActionResult SiteSettings()
        {
            return View();
        }

        // GET: Accounts
        public ActionResult Accounts()
        {
            var viewModel = new AccountsViewModel();

            var context = new ApplicationDbContext();

            var adminId = User.Identity.GetUserId();
            var allUsers = context.Users.Where(u => u.Id != adminId).OrderByDescending(u => u.CreatedDate).ToList();
            var accountsList = new List<AccountsViewModel.Account>();
            allUsers.ForEach(data =>
            {
                var account = new AccountsViewModel.Account(
                        data.UserName, 
                        data.FirstName, 
                        data.LastName, 
                        data.LastOnlineDate, 
                        data.CreatedDate
                    );
                accountsList.Add(account);
            });

            viewModel.Accounts = accountsList;
            return View(viewModel);
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