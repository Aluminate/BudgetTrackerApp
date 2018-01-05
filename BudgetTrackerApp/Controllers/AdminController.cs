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
            var viewModel = new SiteSettingsViewModel();
            var context = new ApplicationDbContext();
            var allUsers = context.Users.ToList();

            // Private Feedback
            var privateFeedbackList = db.Feedbacks
                .Where(f => f.IsTestimonial == false && f.IsHidden == false)
                .OrderByDescending(f => f.CreatedDate)
                .ToList();

            var privateFeedback = new List<SiteSettingsViewModel.Testimonial>();
            privateFeedbackList.ForEach(data =>
            {
                var user = allUsers.Single(au => au.Id == data.UserId);
                var nameOfUser = user.FirstName + " " + user.LastName;
                privateFeedback.Add(new SiteSettingsViewModel.Testimonial(data, nameOfUser));
            });
            viewModel.PrivateFeedback = privateFeedback;

            // Public Testimonial
            var publicTestimonialList = db.Feedbacks
                .Where(f => f.IsTestimonial == true && f.IsHidden == false)
                .OrderByDescending(f => f.CreatedDate)
                .ToList();

            var testimonialList = new List<SiteSettingsViewModel.Testimonial>();
            publicTestimonialList.ForEach(data =>
            {
                var user = allUsers.Single(au => au.Id == data.UserId);
                var nameOfUser = user.FirstName + " " + user.LastName;
                testimonialList.Add(new SiteSettingsViewModel.Testimonial(data, nameOfUser));
            });
            viewModel.PublicTestimonial = testimonialList;

            return View(viewModel);
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

        // POST: HideFeedback
        [HttpPost]
        public ActionResult HideFeedback(int FeedbackId)
        {
            if (ModelState.IsValid)
            {
                var feedback = db.Feedbacks.SingleOrDefault(f => f.FeedbackId == FeedbackId);
                db.Feedbacks.Remove(feedback);
                db.SaveChanges();
            }
            return RedirectToAction("SiteSettings");
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