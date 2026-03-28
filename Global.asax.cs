using Coepd.Web.Infrastructure;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Routing;

namespace Coepd.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            RuntimeStore.Configure(HostingEnvironment.MapPath("~/"));
            DbBootstrapper.EnsureInitialized();
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
