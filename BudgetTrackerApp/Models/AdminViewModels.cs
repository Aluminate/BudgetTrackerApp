using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BudgetTrackerApp.Models
{
    public class AccountsViewModel
    {
        public class Account
        {
            public Account(string Username, string FirstName, string LastName, DateTime LastOnlineDate, DateTime CreatedDate)
            {
                this.Username = Username;
                this.FirstName = FirstName;
                this.LastName = LastName;
                this.LastOnlineDate = LastOnlineDate;
                this.CreatedDate = CreatedDate;
            }

            public string Username;
            public string FirstName;
            public string LastName;
            public DateTime LastOnlineDate;
            public DateTime CreatedDate;
        }

        public List<Account> Accounts { get; set; }
    }

    public class SiteSettingsViewModel
    {
        public class Testimonial
        {
            public Testimonial(Feedback feedback, string name)
            {
                this.feedback = feedback;
                this.Name = name;
            }

            public Feedback feedback;
            public string Name;
        }

        public List<Testimonial> PrivateFeedback { get; set; }
        public List<Testimonial> PublicTestimonial { get; set; }
    }
}