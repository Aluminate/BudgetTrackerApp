//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BudgetTrackerApp
{
    using System;
    using System.Collections.Generic;
    
    public partial class Expense
    {
        public int ExpenseId { get; set; }
        public int BudgetId { get; set; }
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public System.DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string PictureUrl { get; set; }
    
        public virtual Budget Budget { get; set; }
        public virtual Category Category { get; set; }
    }
}
