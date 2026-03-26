using System.Web.Mvc;
using System.Web.Routing;

namespace Coepd.Web
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Home",
                url: "",
                defaults: new { controller = "Home", action = "Index" }
            );

            routes.MapRoute(
                name: "Privacy",
                url: "privacy",
                defaults: new { controller = "Home", action = "Privacy" }
            );

            routes.MapRoute(
                name: "Health",
                url: "health",
                defaults: new { controller = "Home", action = "Health" }
            );

            routes.MapRoute(
                name: "StaffLoginPage",
                url: "staff",
                defaults: new { controller = "Auth", action = "Staff" }
            );

            routes.MapRoute(
                name: "AdminLoginPage",
                url: "admin",
                defaults: new { controller = "Auth", action = "Admin" }
            );

            routes.MapRoute(
                name: "Dashboard",
                url: "dashboard",
                defaults: new { controller = "Home", action = "Dashboard" }
            );

            routes.MapRoute(
                name: "AdminDashboard",
                url: "admin/dashboard",
                defaults: new { controller = "Home", action = "AdminDashboard" }
            );

            routes.MapRoute(
                name: "Chat",
                url: "chat",
                defaults: new { controller = "LeadApi", action = "Chat" }
            );

            routes.MapRoute(
                name: "Lead",
                url: "lead",
                defaults: new { controller = "LeadApi", action = "Lead" }
            );

            routes.MapRoute(
                name: "Contact",
                url: "contact",
                defaults: new { controller = "LeadApi", action = "Contact" }
            );

            routes.MapRoute(
                name: "Enquiry",
                url: "enquiry",
                defaults: new { controller = "LeadApi", action = "Enquiry" }
            );

            routes.MapRoute(
                name: "ApiLeads",
                url: "api/leads/{id}",
                defaults: new { controller = "LeadApi", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "ApiLogin",
                url: "api/login",
                defaults: new { controller = "Auth", action = "ApiLogin" }
            );
            routes.MapRoute(
                name: "AuthMe",
                url: "auth/me",
                defaults: new { controller = "Auth", action = "Me" }
            );
            routes.MapRoute(
                name: "AuthLogout",
                url: "auth/logout",
                defaults: new { controller = "Auth", action = "AuthLogout" }
            );
            routes.MapRoute(
                name: "Logout",
                url: "logout",
                defaults: new { controller = "Auth", action = "Logout" }
            );

            routes.MapRoute(
                name: "ApiAdminLogin",
                url: "api/admin/login",
                defaults: new { controller = "Auth", action = "ApiAdminLogin" }
            );

            routes.MapRoute(
                name: "ApiStaffLogin",
                url: "api/staff/login",
                defaults: new { controller = "Auth", action = "ApiStaffLogin" }
            );
            routes.MapRoute(
                name: "ApiAnalyticsCity",
                url: "api/analytics/city-distribution",
                defaults: new { controller = "Analytics", action = "CityDistribution" }
            );
            routes.MapRoute(
                name: "ApiAnalyticsExperience",
                url: "api/analytics/experience-distribution",
                defaults: new { controller = "Analytics", action = "ExperienceDistribution" }
            );
            routes.MapRoute(
                name: "ApiAnalyticsTopIndustries",
                url: "api/analytics/top-industries",
                defaults: new { controller = "Analytics", action = "TopIndustries" }
            );
            routes.MapRoute(
                name: "ApiAnalyticsLocationTrends",
                url: "api/analytics/location-trends",
                defaults: new { controller = "Analytics", action = "LocationTrends" }
            );
            routes.MapRoute(
                name: "ApiAnalyticsExperienceTrends",
                url: "api/analytics/experience-trends",
                defaults: new { controller = "Analytics", action = "ExperienceTrends" }
            );
            routes.MapRoute(
                name: "ApiAnalyticsDomainTrends",
                url: "api/analytics/domain-trends",
                defaults: new { controller = "Analytics", action = "DomainTrends" }
            );

            routes.MapRoute(
                name: "ApiAdminLeadGrowth",
                url: "api/admin/lead-growth",
                defaults: new { controller = "AdminApi", action = "LeadGrowth" }
            );
            routes.MapRoute(
                name: "ApiAdminSourceBreakdown",
                url: "api/admin/source-breakdown",
                defaults: new { controller = "AdminApi", action = "SourceBreakdown" }
            );
            routes.MapRoute(
                name: "ApiAdminStaffActivate",
                url: "api/admin/staff/activate/{id}",
                defaults: new { controller = "AdminApi", action = "Activate" }
            );
            routes.MapRoute(
                name: "ApiAdminStaffDeactivate",
                url: "api/admin/staff/deactivate/{id}",
                defaults: new { controller = "AdminApi", action = "Deactivate" }
            );
            routes.MapRoute(
                name: "ApiAdminStaffSetRole",
                url: "api/admin/staff/set-role/{id}",
                defaults: new { controller = "AdminApi", action = "SetRole" }
            );
            routes.MapRoute(
                name: "ApiAdmin",
                url: "api/admin/{action}/{id}",
                defaults: new { controller = "AdminApi", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
