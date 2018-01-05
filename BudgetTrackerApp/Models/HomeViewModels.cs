using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetTrackerApp.Models
{
    public class DashboardViewModel
    {
        public IEnumerable<SelectListItem> Categories { get; set; }
        public bool progressBarEnabled { get; set; }
        public decimal progressBarPercentage { get; set; }
        public string progressBarText { get; set; }
    }

    public class AdminDashboardViewModel
    {
        public int totalUsers { get; set; }
        public int registrationsThisMonth { get; set; }
    }

    public class HomeIndexViewModel
    {
        public class Testimonial
        {
            public Testimonial(string message, string name)
            {
                this.Message = message;
                this.Name = name;
            }

            public string Message;
            public string Name;
        }

        public List<Testimonial> Testimonials { get; set; }
    }
}