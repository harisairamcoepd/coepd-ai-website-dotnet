using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Coepd.Web.Infrastructure
{
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles ?? new string[0];
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var role = Convert.ToString(httpContext.Session["role"] ?? string.Empty).ToLowerInvariant();
            return _roles.Any() && _roles.Contains(role);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            filterContext.Result = new RedirectResult("/Auth/Staff");
        }
    }
}
