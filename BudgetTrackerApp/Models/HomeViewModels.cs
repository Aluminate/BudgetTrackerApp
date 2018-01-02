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
    }

    public class AdminDashboardViewModel
    {
        public int totalUsers { get; set; }
        public int registrationsThisMonth { get; set; }
    }

    public class HomeIndexViewModel
    {
        public class testimonial
        {
            public testimonial(string message, string name)
            {
                this.message = message;
                this.name = name;
            }

            public string message;
            public string name;
        }

        public List<testimonial> testimonials { get; set; }
    }
}