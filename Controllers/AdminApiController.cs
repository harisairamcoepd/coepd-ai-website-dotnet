using BCrypt.Net;
using Coepd.Web.Infrastructure;
using Coepd.Web.Models;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Coepd.Web.Controllers
{
    public class AdminApiController : Controller
    {
        private readonly CoepdDbContext _db = new CoepdDbContext();

        [HttpGet]
        [RoleAuthorize("admin", "staff")]
        public ActionResult Leads(string date = null, string source = null, string search = null)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var list = RuntimeStore.GetLeads().Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase)).AsQueryable();

                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var runtimeDate))
                {
                    var dayStart = runtimeDate.Date;
                    var dayEnd = dayStart.AddDays(1);
                    list = list.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
                }

                if (!string.IsNullOrWhiteSpace(source) && source.ToLowerInvariant() != "all")
                {
                    var s = source.Trim().ToLowerInvariant();
                    if (s == "webpage") list = list.Where(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form");
                    else list = list.Where(x => (x.Source ?? "").ToLower() == s);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim().ToLowerInvariant();
                    list = list.Where(x =>
                        (x.Name ?? "").ToLower().Contains(q) ||
                        (x.Email ?? "").ToLower().Contains(q) ||
                        (x.Phone ?? "").ToLower().Contains(q) ||
                        (x.Location ?? "").ToLower().Contains(q));
                }

                var runtimeLeads = list
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(2000)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        phone = x.Phone,
                        email = x.Email,
                        location = x.Location ?? "",
                        source = (x.Source ?? "webpage").ToLower() == "website_form" ? "webpage" : (x.Source ?? "webpage"),
                        created_at = x.CreatedAt,
                        datetime_display = x.CreatedAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
                    })
                    .ToList();

                return Json(new { leads = runtimeLeads }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var query = _db.Leads.Where(x => x.Source != "chatbot_draft").AsQueryable();

            if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var d))
            {
                var dayStart = d.Date;
                var dayEnd = dayStart.AddDays(1);
                query = query.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
            }

            if (!string.IsNullOrWhiteSpace(source) && source.ToLowerInvariant() != "all")
            {
                var s = source.Trim().ToLowerInvariant();
                if (s == "webpage") query = query.Where(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form");
                else query = query.Where(x => (x.Source ?? "").ToLower() == s);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                query = query.Where(x =>
                    (x.Name ?? "").ToLower().Contains(q) ||
                    (x.Email ?? "").ToLower().Contains(q) ||
                    (x.Phone ?? "").ToLower().Contains(q) ||
                    (x.Location ?? "").ToLower().Contains(q));
            }

                var leads = query
                .OrderByDescending(x => x.CreatedAt)
                .Take(2000)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    phone = x.Phone,
                    email = x.Email,
                    location = x.Location ?? "",
                    source = (x.Source ?? "webpage").ToLower() == "website_form" ? "webpage" : (x.Source ?? "webpage"),
                    created_at = x.CreatedAt,
                    datetime_display = x.CreatedAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
                })
                .ToList();

                return Json(new { leads }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                var list = RuntimeStore.GetLeads().AsQueryable();
                list = list.Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var d))
                {
                    var dayStart = d.Date;
                    var dayEnd = dayStart.AddDays(1);
                    list = list.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
                }

                if (!string.IsNullOrWhiteSpace(source) && source.ToLowerInvariant() != "all")
                {
                    var s = source.Trim().ToLowerInvariant();
                    if (s == "webpage") list = list.Where(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form");
                    else list = list.Where(x => (x.Source ?? "").ToLower() == s);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim().ToLowerInvariant();
                    list = list.Where(x =>
                        (x.Name ?? "").ToLower().Contains(q) ||
                        (x.Email ?? "").ToLower().Contains(q) ||
                        (x.Phone ?? "").ToLower().Contains(q) ||
                        (x.Location ?? "").ToLower().Contains(q));
                }

                var leads = list
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(2000)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        phone = x.Phone,
                        email = x.Email,
                        location = x.Location ?? "",
                        source = (x.Source ?? "webpage").ToLower() == "website_form" ? "webpage" : (x.Source ?? "webpage"),
                        created_at = x.CreatedAt,
                        datetime_display = x.CreatedAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
                    })
                    .ToList();

                return Json(new { leads }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpDelete]
        [RoleAuthorize("admin")]
        public ActionResult Leads(int id)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var removed = RuntimeStore.RemoveLead(id);
                return Json(new { success = removed, error = removed ? (string)null : "Lead not found" });
            }

            try
            {
                var lead = _db.Leads.FirstOrDefault(x => x.Id == id);
                if (lead == null) return Json(new { success = false, error = "Lead not found" });
                _db.Leads.Remove(lead);
                _db.SaveChanges();
                return Json(new { success = true });
            }
            catch
            {
                var removed = RuntimeStore.RemoveLead(id);
                return Json(new { success = removed, error = removed ? (string)null : "Lead not found" });
            }
        }

        [HttpGet]
        [RoleAuthorize("admin")]
        public ActionResult Stats()
        {
            if (StorageMode.UseRuntimeStore())
            {
                var leads = RuntimeStore.GetLeads().Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase)).ToList();
                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekStart = today.AddDays(-7);
                var monthStart = new DateTime(now.Year, now.Month, 1);
                return Json(new
                {
                    total_leads = leads.Count,
                    today_leads = leads.Count(x => x.CreatedAt >= today),
                    week_leads = leads.Count(x => x.CreatedAt >= weekStart),
                    month_leads = leads.Count(x => x.CreatedAt >= monthStart),
                    chatbot_leads = leads.Count(x => x.Source == "chatbot"),
                    website_leads = leads.Count(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form")
                }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var now = DateTime.UtcNow;
            var today = now.Date;
            var weekStart = today.AddDays(-7);
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var leadQuery = _db.Leads.Where(x => x.Source != "chatbot_draft");
            var total = leadQuery.Count();
            var todayCount = leadQuery.Count(x => x.CreatedAt >= today);
            var weekCount = leadQuery.Count(x => x.CreatedAt >= weekStart);
            var monthCount = leadQuery.Count(x => x.CreatedAt >= monthStart);
            var chatbot = leadQuery.Count(x => x.Source == "chatbot");
            var webpage = leadQuery.Count(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form");

                return Json(new
            {
                total_leads = total,
                today_leads = todayCount,
                week_leads = weekCount,
                month_leads = monthCount,
                chatbot_leads = chatbot,
                website_leads = webpage
                }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                var leads = RuntimeStore.GetLeads().Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase)).ToList();
                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekStart = today.AddDays(-7);
                var monthStart = new DateTime(now.Year, now.Month, 1);
                return Json(new
                {
                    total_leads = leads.Count,
                    today_leads = leads.Count(x => x.CreatedAt >= today),
                    week_leads = leads.Count(x => x.CreatedAt >= weekStart),
                    month_leads = leads.Count(x => x.CreatedAt >= monthStart),
                    chatbot_leads = leads.Count(x => x.Source == "chatbot"),
                    website_leads = leads.Count(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form")
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [RoleAuthorize("admin")]
        public ActionResult LeadGrowth()
        {
            if (StorageMode.UseRuntimeStore())
            {
                var rows = RuntimeStore.GetLeads();
                var start = DateTime.UtcNow.Date.AddDays(-29);
                var labels = Enumerable.Range(0, 30).Select(i => start.AddDays(i).ToString("dd MMM")).ToList();
                var data = Enumerable.Range(0, 30)
                    .Select(i =>
                    {
                        var d = start.AddDays(i);
                        return rows.Count(x => x.CreatedAt >= d && x.CreatedAt < d.AddDays(1));
                    })
                    .ToList();
                return Json(new { labels, data }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var start = DateTime.UtcNow.Date.AddDays(-29);
            var rows = _db.Leads.Where(x => x.CreatedAt >= start).ToList();
            var labels = Enumerable.Range(0, 30).Select(i => start.AddDays(i).ToString("dd MMM")).ToList();
            var data = Enumerable.Range(0, 30)
                .Select(i =>
                {
                    var d = start.AddDays(i);
                    return rows.Count(x => x.CreatedAt >= d && x.CreatedAt < d.AddDays(1));
                })
                .ToList();
                return Json(new { labels, data }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                var rows = RuntimeStore.GetLeads();
                var start = DateTime.UtcNow.Date.AddDays(-29);
                var labels = Enumerable.Range(0, 30).Select(i => start.AddDays(i).ToString("dd MMM")).ToList();
                var data = Enumerable.Range(0, 30)
                    .Select(i =>
                    {
                        var d = start.AddDays(i);
                        return rows.Count(x => x.CreatedAt >= d && x.CreatedAt < d.AddDays(1));
                    })
                    .ToList();
                return Json(new { labels, data }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [RoleAuthorize("admin")]
        public ActionResult SourceBreakdown()
        {
            if (StorageMode.UseRuntimeStore())
            {
                var runtimeGrouped = RuntimeStore.GetLeads()
                    .GroupBy(x => (x.Source == null || x.Source == "website" || x.Source == "website_form") ? "webpage" : x.Source)
                    .ToDictionary(g => g.Key, g => g.Count());
                return Json(runtimeGrouped, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var grouped = _db.Leads
                .GroupBy(x => (x.Source == null || x.Source == "website" || x.Source == "website_form") ? "webpage" : x.Source)
                .Select(g => new { key = g.Key, count = g.Count() })
                .ToList()
                .ToDictionary(x => x.key, x => x.count);
                return Json(grouped, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                var grouped = RuntimeStore.GetLeads()
                    .GroupBy(x => (x.Source == null || x.Source == "website" || x.Source == "website_form") ? "webpage" : x.Source)
                    .ToDictionary(g => g.Key, g => g.Count());
                return Json(grouped, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        [RoleAuthorize("admin", "staff")]
        public ActionResult Export(string date = null, string source = null, string search = null)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var runtimeLeads = RuntimeStore.GetLeads()
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                var runtimeSb = new StringBuilder();
                runtimeSb.AppendLine("Id,Name,Phone,Email,Location,InterestedDomain,Whatsapp,Source,CreatedAtUtc");
                foreach (var row in runtimeLeads)
                {
                    runtimeSb.AppendLine(string.Join(",",
                        Csv(row.Id.ToString()),
                        Csv(row.Name),
                        Csv(row.Phone),
                        Csv(row.Email),
                        Csv(row.Location),
                        Csv(row.InterestedDomain),
                        Csv(row.Whatsapp),
                        Csv((row.Source == "website" || row.Source == "website_form") ? "webpage" : row.Source),
                        Csv(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                    ));
                }

                var runtimeBytes = Encoding.UTF8.GetBytes(runtimeSb.ToString());
                var runtimeFileName = "coepd_leads_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv";
                return File(runtimeBytes, "text/csv", runtimeFileName);
            }

            try
            {
                var query = _db.Leads.AsQueryable();

                if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var d))
                {
                    var dayStart = d.Date;
                    var dayEnd = dayStart.AddDays(1);
                    query = query.Where(x => x.CreatedAt >= dayStart && x.CreatedAt < dayEnd);
                }

                if (!string.IsNullOrWhiteSpace(source) && source.ToLowerInvariant() != "all")
                {
                    var s = source.Trim().ToLowerInvariant();
                    if (s == "webpage") query = query.Where(x => x.Source == "webpage" || x.Source == "website" || x.Source == "website_form");
                    else query = query.Where(x => (x.Source ?? "").ToLower() == s);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var q = search.Trim().ToLowerInvariant();
                    query = query.Where(x =>
                        (x.Name ?? "").ToLower().Contains(q) ||
                        (x.Email ?? "").ToLower().Contains(q) ||
                        (x.Phone ?? "").ToLower().Contains(q) ||
                        (x.Location ?? "").ToLower().Contains(q));
                }

                var leads = query
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(20000)
                    .Select(x => new
                    {
                        x.Id,
                        x.Name,
                        x.Phone,
                        x.Email,
                        x.Location,
                        x.InterestedDomain,
                        x.Whatsapp,
                        x.Source,
                        x.CreatedAt
                    })
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine("Id,Name,Phone,Email,Location,InterestedDomain,Whatsapp,Source,CreatedAtUtc");

                foreach (var row in leads)
                {
                    sb.AppendLine(string.Join(",",
                        Csv(row.Id.ToString()),
                        Csv(row.Name),
                        Csv(row.Phone),
                        Csv(row.Email),
                        Csv(row.Location),
                        Csv(row.InterestedDomain),
                        Csv(row.Whatsapp),
                        Csv((row.Source == "website" || row.Source == "website_form") ? "webpage" : row.Source),
                        Csv(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                    ));
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var fileName = "coepd_leads_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv";
                return File(bytes, "text/csv", fileName);
            }
            catch
            {
                var leads = RuntimeStore.GetLeads()
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                var sb = new StringBuilder();
                sb.AppendLine("Id,Name,Phone,Email,Location,InterestedDomain,Whatsapp,Source,CreatedAtUtc");
                foreach (var row in leads)
                {
                    sb.AppendLine(string.Join(",",
                        Csv(row.Id.ToString()),
                        Csv(row.Name),
                        Csv(row.Phone),
                        Csv(row.Email),
                        Csv(row.Location),
                        Csv(row.InterestedDomain),
                        Csv(row.Whatsapp),
                        Csv((row.Source == "website" || row.Source == "website_form") ? "webpage" : row.Source),
                        Csv(row.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                    ));
                }

                var bytes = Encoding.UTF8.GetBytes(sb.ToString());
                var fileName = "coepd_leads_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".csv";
                return File(bytes, "text/csv", fileName);
            }
        }

        [HttpGet]
        [RoleAuthorize("admin")]
        public ActionResult Staff()
        {
            if (StorageMode.UseRuntimeStore())
            {
                var runtimeStaff = RuntimeStore.GetStaff()
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        email = x.Email,
                        role = x.Role,
                        status = x.Status,
                        created_at = x.CreatedAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
                    })
                    .ToList();
                return Json(new { staff = runtimeStaff }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var staff = _db.Staff
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Name,
                    email = x.Email,
                    role = x.Role,
                    status = x.Status,
                    created_at = x.CreatedAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
                })
                .ToList();
                return Json(new { staff }, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                var staff = RuntimeStore.GetStaff()
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        email = x.Email,
                        role = x.Role,
                        status = x.Status,
                        created_at = x.CreatedAt.ToString("dd MMM yyyy hh:mm tt", CultureInfo.InvariantCulture)
                    })
                    .ToList();
                return Json(new { staff }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [RoleAuthorize("admin")]
        public ActionResult Staff(StaffCreateRequest payload)
        {
            if (StorageMode.UseRuntimeStore())
            {
                payload = payload ?? ReadBody<StaffCreateRequest>();
                if (payload == null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Password) || string.IsNullOrWhiteSpace(payload.Name))
                    return Json(new { success = false, error = "Invalid payload" });

                var email = payload.Email.Trim().ToLowerInvariant();
                if (RuntimeStore.GetStaff().Any(x => (x.Email ?? "").ToLowerInvariant() == email))
                    return Json(new { success = false, error = "Email already exists" });

                var role = (payload.Role ?? "staff").Trim().ToLowerInvariant();
                if (role != "admin" && role != "staff") role = "staff";

                var user = new Staff
                {
                    Name = payload.Name.Trim(),
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password.Trim()),
                    Role = role,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };
                var id = RuntimeStore.AddStaff(user);
                return Json(new { success = true, id, persisted = true, storage = "runtime-store" });
            }

            try
            {
                payload = payload ?? ReadBody<StaffCreateRequest>();
            if (payload == null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Password) || string.IsNullOrWhiteSpace(payload.Name))
                return Json(new { success = false, error = "Invalid payload" });

            var email = payload.Email.Trim().ToLowerInvariant();
            if (_db.Staff.Any(x => x.Email.ToLower() == email))
                return Json(new { success = false, error = "Email already exists" });

            var role = (payload.Role ?? "staff").Trim().ToLowerInvariant();
            if (role != "admin" && role != "staff") role = "staff";

            var staff = new Staff
            {
                Name = payload.Name.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password.Trim()),
                Role = role,
                Status = "active",
                CreatedAt = DateTime.UtcNow
            };

            _db.Staff.Add(staff);
            _db.SaveChanges();
            return Json(new { success = true, id = staff.Id });
            }
            catch
            {
                if (payload == null || string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Password) || string.IsNullOrWhiteSpace(payload.Name))
                    return Json(new { success = false, error = "Invalid payload" });

                var email = payload.Email.Trim().ToLowerInvariant();
                if (RuntimeStore.GetStaff().Any(x => (x.Email ?? "").ToLowerInvariant() == email))
                    return Json(new { success = false, error = "Email already exists" });

                var role = (payload.Role ?? "staff").Trim().ToLowerInvariant();
                if (role != "admin" && role != "staff") role = "staff";

                var user = new Staff
                {
                    Name = payload.Name.Trim(),
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(payload.Password.Trim()),
                    Role = role,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };
                var id = RuntimeStore.AddStaff(user);
                return Json(new { success = true, id, persisted = false });
            }
        }

        [HttpPut]
        [RoleAuthorize("admin")]
        public ActionResult Activate(int id)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var ok = RuntimeStore.UpdateStaffStatus(id, "active");
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }

            try
            {
                var user = _db.Staff.FirstOrDefault(x => x.Id == id);
            if (user == null) return Json(new { success = false, error = "User not found" });
            user.Status = "active";
            _db.SaveChanges();
            return Json(new { success = true });
            }
            catch
            {
                var ok = RuntimeStore.UpdateStaffStatus(id, "active");
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }
        }

        [HttpPut]
        [RoleAuthorize("admin")]
        public ActionResult Deactivate(int id)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var ok = RuntimeStore.UpdateStaffStatus(id, "inactive");
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }

            try
            {
                var user = _db.Staff.FirstOrDefault(x => x.Id == id);
            if (user == null) return Json(new { success = false, error = "User not found" });
            user.Status = "inactive";
            _db.SaveChanges();
            return Json(new { success = true });
            }
            catch
            {
                var ok = RuntimeStore.UpdateStaffStatus(id, "inactive");
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }
        }

        [HttpPut]
        [RoleAuthorize("admin")]
        public ActionResult SetRole(int id)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var runtimePayload = ReadBody<SetRoleRequest>();
                var runtimeRole = (runtimePayload?.Role ?? "staff").Trim().ToLowerInvariant();
                if (runtimeRole != "admin" && runtimeRole != "staff") return Json(new { success = false, error = "Invalid role" });
                var ok = RuntimeStore.UpdateStaffRole(id, runtimeRole);
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }

            try
            {
                var user = _db.Staff.FirstOrDefault(x => x.Id == id);
            if (user == null) return Json(new { success = false, error = "User not found" });
            var payload = ReadBody<SetRoleRequest>();
            var role = (payload?.Role ?? "staff").Trim().ToLowerInvariant();
            if (role != "admin" && role != "staff") return Json(new { success = false, error = "Invalid role" });
            user.Role = role;
            _db.SaveChanges();
            return Json(new { success = true });
            }
            catch
            {
                var payload = ReadBody<SetRoleRequest>();
                var role = (payload?.Role ?? "staff").Trim().ToLowerInvariant();
                if (role != "admin" && role != "staff") return Json(new { success = false, error = "Invalid role" });
                var ok = RuntimeStore.UpdateStaffRole(id, role);
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }
        }

        [HttpDelete]
        [RoleAuthorize("admin")]
        public ActionResult Staff(int id)
        {
            if (StorageMode.UseRuntimeStore())
            {
                var ok = RuntimeStore.RemoveStaff(id);
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }

            try
            {
                var user = _db.Staff.FirstOrDefault(x => x.Id == id);
            if (user == null) return Json(new { success = false, error = "User not found" });
            _db.Staff.Remove(user);
            _db.SaveChanges();
            return Json(new { success = true });
            }
            catch
            {
                var ok = RuntimeStore.RemoveStaff(id);
                return Json(new { success = ok, error = ok ? (string)null : "User not found" });
            }
        }

        private T ReadBody<T>() where T : class
        {
            try
            {
                var json = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
                if (string.IsNullOrWhiteSpace(json)) return null;
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return null;
            }
        }

        public sealed class StaffCreateRequest
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public string Role { get; set; }
        }

        public sealed class SetRoleRequest
        {
            public string Role { get; set; }
        }

        private static string Csv(string value)
        {
            var s = (value ?? string.Empty).Replace("\"", "\"\"");
            return "\"" + s + "\"";
        }
    }
}
