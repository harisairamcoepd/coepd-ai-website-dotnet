using BCrypt.Net;
using Coepd.Web.Infrastructure;
using Coepd.Web.Models;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Coepd.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly CoepdDbContext _db = new CoepdDbContext();

        [HttpGet]
        public ActionResult Staff()
        {
            return RenderTemplate("staff_login.html", null);
        }

        [HttpPost]
        public ActionResult Staff(string email, string password)
        {
            var user = AuthenticateUser(email, password, "staff");
            if (user == null) return Redirect("/staff");
            SetSession(user);
            return Redirect("/dashboard");
        }

        [HttpGet]
        public ActionResult Admin()
        {
            return RenderTemplate("admin_login.html", null);
        }

        [HttpPost]
        public ActionResult Admin(string email, string password)
        {
            var user = AuthenticateUser(email, password, "admin");
            if (user == null) return Redirect("/admin");
            SetSession(user);
            return Redirect("/admin/dashboard");
        }

        [HttpPost]
        public ActionResult ApiLogin()
        {
            var payload = ReadBody<LoginRequest>();
            var user = AuthenticateUser(payload?.Email, payload?.Password, null);
            if (user == null) return Json(new { error = "Invalid email or password" });
            SetSession(user);
            return Json(new { success = true, role = user.Role });
        }

        [HttpPost]
        public ActionResult ApiAdminLogin()
        {
            var payload = ReadBody<LoginRequest>();
            var user = AuthenticateUser(payload?.Email, payload?.Password, "admin");
            if (user == null) return Json(new { error = "Invalid email or password" });
            SetSession(user);
            return Json(new { success = true, role = user.Role });
        }

        [HttpPost]
        public ActionResult ApiStaffLogin()
        {
            var payload = ReadBody<LoginRequest>();
            var user = AuthenticateUser(payload?.Email, payload?.Password, "staff");
            if (user == null) return Json(new { error = "Invalid email or password" });
            SetSession(user);
            return Json(new { success = true, role = user.Role });
        }

        [HttpGet]
        public ActionResult Me()
        {
            return Json(new
            {
                user = new
                {
                    id = Session["user_id"],
                    email = Session["email"],
                    name = Session["name"],
                    role = Session["role"]
                }
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Logout()
        {
            Session.Clear();
            return Redirect("/");
        }

        [HttpPost]
        public ActionResult AuthLogout()
        {
            Session.Clear();
            return Redirect("/");
        }

        private Staff AuthenticateUser(string email, string password, string requiredRole)
        {
            var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
            var adminEmail = (ConfigurationManager.AppSettings["ADMIN_LOGIN_EMAIL"] ?? "admin").Trim().ToLowerInvariant();
            var adminPassword = (ConfigurationManager.AppSettings["ADMIN_LOGIN_PASSWORD"] ?? "admin").Trim();

            if (normalizedEmail == adminEmail && (password ?? string.Empty) == adminPassword)
            {
                if (!string.IsNullOrWhiteSpace(requiredRole) && requiredRole != "admin") return null;
                return new Staff { Id = 0, Name = "Admin", Email = adminEmail, Role = "admin", Status = "active" };
            }

            if (StorageMode.UseRuntimeStore())
            {
                return AuthenticateFromRuntimeStore(normalizedEmail, password, requiredRole);
            }

            try
            {
                var staff = _db.Staff.FirstOrDefault(x => x.Email.ToLower() == normalizedEmail);
                if (staff == null || (staff.Status ?? "").ToLower() != "active") return null;
                if (!BCrypt.Net.BCrypt.Verify(password ?? string.Empty, staff.PasswordHash ?? string.Empty)) return null;
                if (!string.IsNullOrWhiteSpace(requiredRole) && !string.Equals(staff.Role, requiredRole, StringComparison.OrdinalIgnoreCase)) return null;
                return staff;
            }
            catch
            {
                return AuthenticateFromRuntimeStore(normalizedEmail, password, requiredRole);
            }
        }

        private static Staff AuthenticateFromRuntimeStore(string normalizedEmail, string password, string requiredRole)
        {
            var staff = RuntimeStore.FindStaffByEmail(normalizedEmail);
            if (staff == null || (staff.Status ?? "").ToLower() != "active") return null;
            if (!BCrypt.Net.BCrypt.Verify(password ?? string.Empty, staff.PasswordHash ?? string.Empty)) return null;
            if (!string.IsNullOrWhiteSpace(requiredRole) && !string.Equals(staff.Role, requiredRole, StringComparison.OrdinalIgnoreCase)) return null;
            return staff;
        }

        private void SetSession(Staff user)
        {
            Session["user_id"] = user.Id;
            Session["email"] = user.Email;
            Session["name"] = user.Name;
            Session["role"] = (user.Role ?? "staff").ToLowerInvariant();
        }

        private T ReadBody<T>() where T : class
        {
            var json = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonConvert.DeserializeObject<T>(json);
        }

        private ActionResult RenderTemplate(string fileName, string error)
        {
            var path = Server.MapPath("~/Content/templates/" + fileName);
            var html = System.IO.File.ReadAllText(path);
            html = html.Replace("{{ error or '' }}", HttpUtility.HtmlEncode(error ?? string.Empty));
            html = html.Replace("/static/", "/Content/static/");
            return Content(html, "text/html");
        }
    }
}
