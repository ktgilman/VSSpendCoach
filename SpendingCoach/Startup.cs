using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SpendingCoach.Startup))]
namespace SpendingCoach
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
