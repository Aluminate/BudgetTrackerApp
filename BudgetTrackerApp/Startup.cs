using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(BudgetTrackerApp.Startup))]
namespace BudgetTrackerApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
