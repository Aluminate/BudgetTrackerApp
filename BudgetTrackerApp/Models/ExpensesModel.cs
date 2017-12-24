namespace BudgetTrackerApp.Models
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class ExpensesModel : DbContext
    {
        public ExpensesModel()
            : base("name=ExpensesModel")
        {
        }

        public virtual DbSet<Expense> Expenses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Expense>()
                .Property(e => e.Amount)
                .HasPrecision(19, 4);
        }
    }
}
