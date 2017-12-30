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
            return View();
        }

        // POST: CreateFeedback
        [HttpPost]
        public ActionResult CreateFeedback(bool isPublic, string comment)
        {
            if (ModelState.IsValid)
            {
                Feedback feedback = new Feedback();
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