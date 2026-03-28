using System.IO;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Coepd.Web.Infrastructure;
using Coepd.Web.Models;
using System.Collections.Generic;
using System.Globalization;

namespace Coepd.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly CoepdDbContext _db = new CoepdDbContext();

        [HttpGet]
        public ActionResult Index()
        {
            return RenderTemplate("index.html", true);
        }

        [HttpGet]
        public ActionResult Privacy()
        {
            return RenderTemplate("privacy.html", false);
        }

        [HttpGet]
        public ActionResult Health()
        {
            return Json(new { status = "running", database = "connected", auth = "enabled" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Dashboard()
        {
            var role = (Session["role"] as string ?? string.Empty).ToLowerInvariant();
            if (role != "staff") return Redirect("/staff");
            return Content(BuildStaffDashboardHtml(), "text/html");
        }

        [HttpGet]
        public ActionResult AdminDashboard()
        {
            var role = (Session["role"] as string ?? string.Empty).ToLowerInvariant();
            if (role != "admin") return Redirect("/admin");
            return Content(BuildAdminDashboardHtml(), "text/html");
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
            var pageTemplate = CleanTemplateText(System.IO.File.ReadAllText(pageTemplatePath));

            var titleMatch = Regex.Match(pageTemplate, @"\{%\s*block\s+title\s*%\}(.*?)\{%\s*endblock\s*%\}", RegexOptions.Singleline);
            var contentMatch = Regex.Match(pageTemplate, @"\{%\s*block\s+content\s*%\}(.*?)\{%\s*endblock\s*%\}", RegexOptions.Singleline);

            var pageTitle = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : "COEPD";
            var pageContent = contentMatch.Success ? contentMatch.Groups[1].Value : pageTemplate;
            pageContent = ResolveIncludes(pageContent, templatesRoot);

            var baseTemplatePath = Path.Combine(templatesRoot, "base.html");
            var baseTemplate = CleanTemplateText(System.IO.File.ReadAllText(baseTemplatePath));

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
                    return System.IO.File.Exists(includePath)
                        ? CleanTemplateText(System.IO.File.ReadAllText(includePath))
                        : string.Empty;
                });
        }

        private static string CleanTemplateText(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("\uFEFF", string.Empty) // UTF-8 BOM char
                .Replace("ï»¿", string.Empty);   // BOM bytes decoded as visible text
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

        private string BuildStaffDashboardHtml()
        {
            var leads = GetLeads();
            var search = (Request.QueryString["search"] ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.ToLowerInvariant();
                leads = leads.Where(x =>
                    (x.Name ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (x.Email ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (x.Phone ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (x.Location ?? string.Empty).ToLowerInvariant().Contains(q)).ToList();
            }

            var total = leads.Count;
            var chatbot = leads.Count(x => NormalizeSource(x.Source) == "chatbot");
            var website = leads.Count(x => NormalizeSource(x.Source) == "webpage");
            var pageSize = 10;
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            var page = 1;
            int.TryParse(Request.QueryString["page"], out page);
            page = Math.Max(1, Math.Min(page <= 0 ? 1 : page, totalPages));
            var pageLeads = leads.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var html = CleanTemplateText(System.IO.File.ReadAllText(Server.MapPath("~/Content/templates/dashboard.html")));
            html = html.Replace("/static/", "/Content/static/");
            html = html.Replace("{{ current_user.name }}", HttpUtility.HtmlEncode(Session["name"] as string ?? "Staff"));
            html = html.Replace("{{ total_leads }}", total.ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ chatbot_leads }}", chatbot.ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ website_leads }}", website.ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ search }}", HttpUtility.HtmlAttributeEncode(search));
            html = html.Replace("{{ page }}", page.ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ total_pages }}", totalPages.ToString(CultureInfo.InvariantCulture));
            html = Regex.Replace(html, @"\{%\s*for\s+lead\s+in\s+leads\s*%\}.*?\{%\s*endfor\s*%\}", BuildStaffLeadRows(pageLeads), RegexOptions.Singleline);
            html = Regex.Replace(html, @"\{%\s*if\s+not\s+leads\s*%\}.*?\{%\s*endif\s*%\}", pageLeads.Any() ? string.Empty : "<tr><td colspan=\"7\" class=\"muted\" style=\"text-align:center;padding:24px\">No leads found.</td></tr>", RegexOptions.Singleline);
            html = Regex.Replace(html, @"\{%\s*if\s+page\s*>\s*1\s*%\}.*?\{%\s*endif\s*%\}", page > 1 ? "<a href=\"/dashboard?page=" + (page - 1).ToString(CultureInfo.InvariantCulture) + "&search=" + HttpUtility.UrlEncode(search) + "\">Prev</a>" : string.Empty, RegexOptions.Singleline);
            html = Regex.Replace(html, @"\{%\s*if\s+page\s*<\s*total_pages\s*%\}.*?\{%\s*endif\s*%\}", page < totalPages ? "<a href=\"/dashboard?page=" + (page + 1).ToString(CultureInfo.InvariantCulture) + "&search=" + HttpUtility.UrlEncode(search) + "\">Next</a>" : string.Empty, RegexOptions.Singleline);
            html = Regex.Replace(html, @"\{%\s*[^%]*%\}", string.Empty);
            html = Regex.Replace(html, @"\{\{\s*[^}]+\s*\}\}", string.Empty);
            return html;
        }

        private string BuildAdminDashboardHtml()
        {
            var leads = FilterLeads(GetLeads());
            var html = CleanTemplateText(System.IO.File.ReadAllText(Server.MapPath("~/Content/templates/admin.html")));
            var filterDate = (Request.QueryString["date"] ?? string.Empty).Trim();
            var filterSource = (Request.QueryString["source"] ?? "all").Trim().ToLowerInvariant();
            var filterSearch = (Request.QueryString["search"] ?? string.Empty).Trim();
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-7);
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var userName = Session["name"] as string ?? "Admin";
            var userEmail = Session["email"] as string ?? string.Empty;

            html = html.Replace("/static/", "/Content/static/");
            html = html.Replace("{{ current_user.name }}", HttpUtility.HtmlEncode(userName));
            html = html.Replace("role: \"admin\",\r\n      csrfToken: \"\",\r\n      name: \"" + HttpUtility.HtmlEncode(userName) + "\"", "role: \"admin\",\r\n      csrfToken: \"\",\r\n      name: \"" + HttpUtility.JavaScriptStringEncode(userName) + "\"");
            html = html.Replace("email: \"" + HttpUtility.HtmlEncode(userEmail) + "\"", "email: \"" + HttpUtility.JavaScriptStringEncode(userEmail) + "\"");
            html = html.Replace("{{ current_user.email }}", HttpUtility.HtmlEncode(userEmail));
            html = html.Replace("{{ current_user.role | title }}", "Admin");
            html = html.Replace("{{ current_user.role }}", "admin");
            html = html.Replace("{{ total_leads }}", leads.Count.ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ chatbot_leads }}", leads.Count(x => NormalizeSource(x.Source) == "chatbot").ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ website_leads }}", leads.Count(x => NormalizeSource(x.Source) == "webpage").ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ today_leads }}", leads.Count(x => x.CreatedAt >= today).ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ filter_date }}", HttpUtility.HtmlAttributeEncode(filterDate));
            html = html.Replace("{{ filter_search }}", HttpUtility.HtmlAttributeEncode(filterSearch));
            html = html.Replace("{% if filter_source == 'all' %}selected{% endif %}", filterSource == "all" ? "selected" : string.Empty);
            html = html.Replace("{% if filter_source == 'webpage' %}selected{% endif %}", filterSource == "webpage" ? "selected" : string.Empty);
            html = html.Replace("{% if filter_source == 'chatbot' %}selected{% endif %}", filterSource == "chatbot" ? "selected" : string.Empty);
            html = Regex.Replace(html, @"\{%\s*if\s+current_user\.role\s*==\s*'admin'\s*%\}", string.Empty);
            html = Regex.Replace(html, @"\{%\s*endif\s*%\}", string.Empty);
            html = Regex.Replace(html, @"\{%\s*for\s+l\s+in\s+leads\s*%\}.*?\{%\s*endfor\s*%\}", BuildAdminLeadRows(leads.Take(10).ToList()), RegexOptions.Singleline);
            html = Regex.Replace(html, @"\{%\s*if\s+not\s+leads\s*%\}.*?\{%\s*endif\s*%\}", leads.Any() ? string.Empty : "<tr><td colspan=\"8\" style=\"text-align:center;padding:24px;color:var(--a-muted)\">No leads found.</td></tr>", RegexOptions.Singleline);
            html = ReplaceFirst(html, "&ndash;", leads.Count(x => x.CreatedAt >= weekStart).ToString(CultureInfo.InvariantCulture));
            html = ReplaceFirst(html, "&ndash;", leads.Count(x => x.CreatedAt >= monthStart).ToString(CultureInfo.InvariantCulture));
            html = html.Replace("{{ csrf_token }}", string.Empty);
            html = html.Replace("{{ current_user.picture or '' }}", string.Empty);
            html = Regex.Replace(html, @"\{%\s*[^%]*%\}", string.Empty);
            html = Regex.Replace(html, @"\{\{\s*[^}]+\s*\}\}", string.Empty);
            return html;
        }

        private List<Lead> GetLeads()
        {
            if (StorageMode.UseRuntimeStore())
            {
                return RuntimeStore.GetLeads()
                    .Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }

            try
            {
                return _db.Leads.Where(x => x.Source != "chatbot_draft").OrderByDescending(x => x.CreatedAt).ToList();
            }
            catch
            {
                return RuntimeStore.GetLeads()
                    .Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }
        }

        private List<Lead> FilterLeads(List<Lead> leads)
        {
            var list = leads ?? new List<Lead>();
            var filterDate = (Request.QueryString["date"] ?? string.Empty).Trim();
            var filterSource = (Request.QueryString["source"] ?? "all").Trim().ToLowerInvariant();
            var filterSearch = (Request.QueryString["search"] ?? string.Empty).Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(filterDate) && DateTime.TryParse(filterDate, out var date))
            {
                var dayStart = date.Date;
                var dayEnd = dayStart.AddDays(1);
                list = list.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filterSource) && filterSource != "all")
            {
                list = list.Where(x => NormalizeSource(x.Source) == filterSource).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filterSearch))
            {
                list = list.Where(x =>
                    (x.Name ?? string.Empty).ToLowerInvariant().Contains(filterSearch) ||
                    (x.Email ?? string.Empty).ToLowerInvariant().Contains(filterSearch) ||
                    (x.Phone ?? string.Empty).ToLowerInvariant().Contains(filterSearch) ||
                    (x.Location ?? string.Empty).ToLowerInvariant().Contains(filterSearch)).ToList();
            }

            return list.OrderByDescending(x => x.CreatedAt).ToList();
        }

        private static string BuildStaffLeadRows(List<Lead> leads)
        {
            if (leads == null || !leads.Any()) return string.Empty;
            return string.Join(string.Empty, leads.Select(lead =>
                "<tr>" +
                "<td>" + lead.Id.ToString(CultureInfo.InvariantCulture) + "</td>" +
                "<td style=\"font-weight:500\">" + HttpUtility.HtmlEncode(lead.Name) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(lead.Phone) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(lead.Email) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(lead.Location) ? "-" : lead.Location) + "</td>" +
                "<td><span class=\"badge " + (NormalizeSource(lead.Source) == "chatbot" ? "badge-chatbot\">chatbot" : "badge-webpage\">webpage") + "</span></td>" +
                "<td style=\"white-space:nowrap\">" + HttpUtility.HtmlEncode(TimeZoneHelper.ToDisplayText(lead.CreatedAt)) + "</td>" +
                "</tr>"
            ));
        }

        private static string BuildAdminLeadRows(List<Lead> leads)
        {
            if (leads == null || !leads.Any()) return string.Empty;
            return string.Join(string.Empty, leads.Select(lead =>
                "<tr id=\"row-" + lead.Id.ToString(CultureInfo.InvariantCulture) + "\">" +
                "<td>" + lead.Id.ToString(CultureInfo.InvariantCulture) + "</td>" +
                "<td style=\"font-weight:500\">" + HttpUtility.HtmlEncode(lead.Name) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(lead.Phone) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(lead.Email) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(lead.Location) ? "-" : lead.Location) + "</td>" +
                "<td>" + HttpUtility.HtmlEncode(NormalizeSource(lead.Source)) + "</td>" +
                "<td style=\"white-space:nowrap\">" + HttpUtility.HtmlEncode(TimeZoneHelper.ToDisplayText(lead.CreatedAt)) + "</td>" +
                "<td>-</td>" +
                "</tr>"
            ));
        }

        private static string NormalizeSource(string source)
        {
            var normalized = (source ?? "webpage").Trim().ToLowerInvariant();
            return normalized == "chatbot" ? "chatbot" : "webpage";
        }

        private static string ReplaceFirst(string input, string find, string replace)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(find)) return input;
            var index = input.IndexOf(find, StringComparison.Ordinal);
            if (index < 0) return input;
            return input.Substring(0, index) + replace + input.Substring(index + find.Length);
        }
    }
}
