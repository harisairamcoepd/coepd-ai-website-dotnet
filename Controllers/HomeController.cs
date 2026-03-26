using System.IO;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Coepd.Web.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index() => RenderTemplate("index.html", true);

        [HttpGet]
        public ActionResult Privacy() => RenderTemplate("privacy.html", false);

        [HttpGet]
        public ActionResult Health() => Json(new { status = "running", database = "connected", auth = "enabled" }, JsonRequestBehavior.AllowGet);

        [HttpGet]
        public ActionResult Dashboard()
        {
            var role = (Session["role"] as string ?? string.Empty).ToLowerInvariant();
            if (role != "staff") return Redirect("/staff");
            return RenderTemplate("dashboard.html", false);
        }

        [HttpGet]
        public ActionResult AdminDashboard()
        {
            var role = (Session["role"] as string ?? string.Empty).ToLowerInvariant();
            if (role != "admin") return Redirect("/admin");
            return RenderTemplate("admin.html", false);
        }

        private ActionResult RenderTemplate(string fileName, bool isHomePage)
        {
            var html = RenderJinjaLikeTemplate(fileName, isHomePage);
            return Content(html, "text/html");
        }

        private string RenderJinjaLikeTemplate(string fileName, bool isHomePage)
        {
            var templatesRoot = Server.MapPath("~/Content/templates/");
            var pageTemplatePath = Path.Combine(templatesRoot, fileName);
            var pageTemplate = System.IO.File.ReadAllText(pageTemplatePath);

            var titleMatch = Regex.Match(pageTemplate, @"\{%\s*block\s+title\s*%\}(.*?)\{%\s*endblock\s*%\}", RegexOptions.Singleline);
            var contentMatch = Regex.Match(pageTemplate, @"\{%\s*block\s+content\s*%\}(.*?)\{%\s*endblock\s*%\}", RegexOptions.Singleline);

            var pageTitle = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "COEPD";
            var pageContent = contentMatch.Success ? contentMatch.Groups[1].Value : pageTemplate;
            pageContent = ResolveIncludes(pageContent, templatesRoot);

            var baseTemplatePath = Path.Combine(templatesRoot, "base.html");
            var baseTemplate = System.IO.File.ReadAllText(baseTemplatePath);

            baseTemplate = Regex.Replace(baseTemplate, @"\{%\s*block\s+title\s*%\}.*?\{%\s*endblock\s*%\}", pageTitle, RegexOptions.Singleline);
            baseTemplate = Regex.Replace(baseTemplate, @"\{%\s*block\s+content\s*%\}.*?\{%\s*endblock\s*%\}", pageContent, RegexOptions.Singleline);

            var role = (Session["role"] as string ?? "staff").ToLowerInvariant();
            var name = Session["name"] as string ?? (role == "admin" ? "Admin" : "Staff");
            var email = Session["email"] as string ?? string.Empty;
            return TransformTemplateExpressions(baseTemplate, isHomePage, role, name, email);
        }

        private static string ResolveIncludes(string content, string templatesRoot)
        {
            return Regex.Replace(
                content,
                @"\{%\s*include\s+""([^""]+)""\s*%\}",
                match =>
                {
                    var relativePath = match.Groups[1].Value.Replace("/", "\\");
                    var includePath = Path.Combine(templatesRoot, relativePath);
                    return System.IO.File.Exists(includePath) ? System.IO.File.ReadAllText(includePath) : string.Empty;
                });
        }

        private static string TransformTemplateExpressions(string html, bool isHomePage, string role, string name, string email)
        {
            var rendered = html.Replace("{% set is_home_page = request.url.path == \"/\" %}", string.Empty);
            rendered = rendered.Replace("{{ '#home' if is_home_page else '/' }}", isHomePage ? "#home" : "/");
            rendered = rendered.Replace("{{ '#home' if is_home_page else '/#home' }}", isHomePage ? "#home" : "/#home");
            rendered = rendered.Replace("{{ '#program' if is_home_page else '/#program' }}", isHomePage ? "#program" : "/#program");
            rendered = rendered.Replace("{{ '#system' if is_home_page else '/#system' }}", isHomePage ? "#system" : "/#system");
            rendered = rendered.Replace("{{ '#tools' if is_home_page else '/#tools' }}", isHomePage ? "#tools" : "/#tools");
            rendered = rendered.Replace("{{ '#testimonials' if is_home_page else '/#testimonials' }}", isHomePage ? "#testimonials" : "/#testimonials");
            rendered = rendered.Replace("{{ '#curriculum' if is_home_page else '/#curriculum' }}", isHomePage ? "#curriculum" : "/#curriculum");
            rendered = rendered.Replace("{{ '#contact' if is_home_page else '/#contact' }}", isHomePage ? "#contact" : "/#contact");
            rendered = rendered.Replace("{{ '#placements' if is_home_page else '/#placements' }}", isHomePage ? "#placements" : "/#placements");
            rendered = rendered.Replace("{{ '#domains' if is_home_page else '/#domains' }}", isHomePage ? "#domains" : "/#domains");
            rendered = rendered.Replace("{{ '#system' if is_home_page else '/#system' }}", isHomePage ? "#system" : "/#system");
            rendered = rendered.Replace("{{ '#home' if is_home_page else '/#home' }}", isHomePage ? "#home" : "/#home");

            rendered = rendered.Replace("{{ current_user.role | title }}", ToTitleCase(role));
            rendered = rendered.Replace("{{ current_user.role }}", role);
            rendered = rendered.Replace("{{ current_user.name }}", name);
            rendered = rendered.Replace("{{ current_user.email }}", email);
            rendered = rendered.Replace("{{ current_user.picture or '' }}", string.Empty);
            rendered = rendered.Replace("{{ csrf_token }}", string.Empty);

            rendered = rendered.Replace("/static/", "/Content/static/");
            rendered = rendered.Replace("/chatbot/", "/Content/static/chatbot/");
            rendered = Regex.Replace(rendered, @"\{%\s*[^%]*%\}", string.Empty);
            rendered = Regex.Replace(rendered, @"\{\{\s*[^}]+\s*\}\}", string.Empty);
            return rendered;
        }

        private static string ToTitleCase(string value)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0) return string.Empty;
            return char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
        }
    }
}
