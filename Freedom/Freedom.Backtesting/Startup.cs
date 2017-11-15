using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Freedom.Backtesting.Startup))]
namespace Freedom.Backtesting
{
    public partial class Startup {
        public void Configuration(IAppBuilder app) {
            ConfigureAuth(app);
        }
    }
}
