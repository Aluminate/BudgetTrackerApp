namespace BudgetTrackerApp.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Expenses")]
    public partial class Expense
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ExpenseId { get; set; }

        public int BudgetId { get; set; }

        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Description { get; set; }

        [Column(TypeName = "date")]
        public DateTime Date { get; set; }

        [Column(TypeName = "money")]
        public decimal Amount { get; set; }

        [StringLength(50)]
        public string PictureUrl { get; set; }
    }
}
