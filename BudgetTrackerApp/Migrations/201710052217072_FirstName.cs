namespace BudgetTrackerApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FirstName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "FirstName", c => c.String());
            AddColumn("dbo.AspNetUsers", "MiddleName", c => c.String());
            AddColumn("dbo.AspNetUsers", "LastName", c => c.String());
            AddColumn("dbo.AspNetUsers", "Gender", c => c.String());
            AddColumn("dbo.AspNetUsers", "SecurityQuestion", c => c.String());
            AddColumn("dbo.AspNetUsers", "SecurityQuestionAnswer", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "SecurityQuestionAnswer");
            DropColumn("dbo.AspNetUsers", "SecurityQuestion");
            DropColumn("dbo.AspNetUsers", "Gender");
            DropColumn("dbo.AspNetUsers", "LastName");
            DropColumn("dbo.AspNetUsers", "MiddleName");
            DropColumn("dbo.AspNetUsers", "FirstName");
        }
    }
}
