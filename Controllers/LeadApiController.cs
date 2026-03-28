using Coepd.Web.Models;
using Coepd.Web.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace Coepd.Web.Controllers
{
    public class LeadApiController : Controller
    {
        private readonly CoepdDbContext _db = new CoepdDbContext();
        private static readonly ConcurrentDictionary<string, ChatSessionState> ChatSessions = new ConcurrentDictionary<string, ChatSessionState>();
        private static readonly Regex PhoneRegex = new Regex(@"^\d{10}$", RegexOptions.Compiled);
        private static readonly Regex EmailRegex = new Regex(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", RegexOptions.Compiled);

        private const string InitialGreeting = "Hi, welcome to COEPD. How can I help you?";
        private const string AskName = "Please share your name.";
        private const string AskPhone = "Please share your phone number.";
        private const string AskEmail = "Please share your email address.";
        private const string AskLocation = "Please share your location.";
        private const string FinalMessage = "Thank you for sharing your details. Our team will contact you shortly.\nFor more queries call +91 88850 24387 or visit our office at Hyderabad:\nhttps://www.google.com/maps/search/?api=1&query=IIIrd+Floor+Besides+Police+Station+SR+Nagar+Main+Rd+Srinivasa+Nagar+Sanjeeva+Reddy+Nagar+Hyderabad+Telangana+500038";

        [HttpGet]
        public ActionResult Index()
        {
            if (StorageMode.UseRuntimeStore())
            {
                var runtimeLeads = RuntimeStore.GetLeads()
                    .Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        phone = x.Phone,
                        email = x.Email,
                        location = x.Location ?? "",
                        interested_domain = x.InterestedDomain ?? "",
                        whatsapp = x.Whatsapp ?? "",
                        source = x.Source ?? "webpage",
                        created_at = x.CreatedAt
                    }).ToList();
                DiagnosticLogger.Info("LeadApi.Index", "Runtime store returned " + runtimeLeads.Count.ToString(CultureInfo.InvariantCulture) + " leads.");
                return Json(runtimeLeads, JsonRequestBehavior.AllowGet);
            }

            try
            {
                DiagnosticLogger.Info("LeadApi.Index", "Fetching leads from SQL database.");
                var leads = _db.Leads
                    .Where(x => x.Source != "chatbot_draft")
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        phone = x.Phone,
                        email = x.Email,
                        location = x.Location ?? "",
                        interested_domain = x.InterestedDomain ?? "",
                        whatsapp = x.Whatsapp ?? "",
                        source = x.Source ?? "webpage",
                        created_at = x.CreatedAt
                    }).ToList();
                DiagnosticLogger.Info("LeadApi.Index", "SQL returned " + leads.Count.ToString(CultureInfo.InvariantCulture) + " leads.");
                return Json(leads, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var leads = RuntimeStore.GetLeads()
                    .Where(x => !string.Equals(x.Source, "chatbot_draft", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.Name,
                        phone = x.Phone,
                        email = x.Email,
                        location = x.Location ?? "",
                        interested_domain = x.InterestedDomain ?? "",
                        whatsapp = x.Whatsapp ?? "",
                        source = x.Source ?? "webpage",
                        created_at = x.CreatedAt
                    }).ToList();
                DiagnosticLogger.Error("LeadApi.Index", "SQL fetch failed. Falling back to runtime store with " + leads.Count.ToString(CultureInfo.InvariantCulture) + " leads.", ex);
                return Json(leads, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public ActionResult Index(LeadCreateRequest payload)
        {
            if (payload == null)
            {
                payload = JsonConvert.DeserializeObject<LeadCreateRequest>(new System.IO.StreamReader(Request.InputStream).ReadToEnd());
            }

            var source = NormalizeSource(payload?.Source);
            var lead = new Lead
            {
                Name = (payload?.Name ?? string.Empty).Trim(),
                Phone = (payload?.Phone ?? string.Empty).Trim(),
                Email = (payload?.Email ?? string.Empty).Trim().ToLowerInvariant(),
                Location = (payload?.Location ?? string.Empty).Trim(),
                InterestedDomain = (payload?.InterestedDomain ?? string.Empty).Trim(),
                Whatsapp = (payload?.Whatsapp ?? string.Empty).Trim(),
                Source = source,
                CreatedAt = DateTime.UtcNow
            };

            if (StorageMode.UseRuntimeStore())
            {
                var fallbackId = RuntimeStore.AddLead(lead);
                return Json(new
                {
                    ok = true,
                    success = true,
                    id = fallbackId,
                    source = lead.Source,
                    created_at = lead.CreatedAt,
                    persisted = true,
                    storage = "runtime-store"
                });
            }

            try
            {
                _db.Leads.Add(lead);
                _db.SaveChanges();
                DiagnosticLogger.Info("LeadApi.Create", "Lead inserted into SQL. Id=" + lead.Id.ToString(CultureInfo.InvariantCulture) + ", source=" + (lead.Source ?? string.Empty));
                return Json(new
                {
                    ok = true,
                    success = true,
                    id = lead.Id,
                    source = lead.Source,
                    created_at = lead.CreatedAt
                });
            }
            catch (Exception ex)
            {
                var fallbackId = RuntimeStore.AddLead(lead);
                DiagnosticLogger.Error("LeadApi.Create", "SQL insert failed. Falling back to runtime store. RuntimeId=" + fallbackId.ToString(CultureInfo.InvariantCulture), ex);
                return Json(new
                {
                    ok = true,
                    success = true,
                    id = fallbackId > 0 ? fallbackId.ToString() : ("offline_" + DateTime.UtcNow.Ticks.ToString()),
                    source = lead.Source,
                    created_at = lead.CreatedAt,
                    persisted = false,
                    warning = "Database unavailable"
                });
            }
        }

        [HttpPost]
        public ActionResult Lead(LeadCreateRequest payload)
        {
            return Index(payload);
        }

        [HttpPost]
        public ActionResult Contact(LeadCreateRequest payload)
        {
            return Index(payload);
        }

        [HttpPost]
        public ActionResult Enquiry(LeadCreateRequest payload)
        {
            return Index(payload);
        }

        [HttpPost]
        public ActionResult Chat()
        {
            ChatRequest payload;
            try
            {
                var raw = new System.IO.StreamReader(Request.InputStream).ReadToEnd();
                payload = string.IsNullOrWhiteSpace(raw) ? new ChatRequest() : JsonConvert.DeserializeObject<ChatRequest>(raw) ?? new ChatRequest();
            }
            catch
            {
                payload = new ChatRequest();
            }
            var userId = string.IsNullOrWhiteSpace(payload.UserId) ? "web_user" : payload.UserId.Trim();
            var message = (payload.Message ?? string.Empty).Trim();

            if (message == "__restart__")
            {
                ChatSessions[userId] = new ChatSessionState();
                return BuildChatResponse(InitialGreeting, "Say hi or hello", 10, "greeting");
            }

            var session = ChatSessions.GetOrAdd(userId, _ => new ChatSessionState());

            if (message == "__init__" || message == string.Empty)
            {
                switch (session.Stage)
                {
                    case "collect_name":
                        return BuildChatResponse(AskName, "Enter your name", 25, "collect_name");
                    case "collect_phone":
                        return BuildChatResponse(AskPhone, "Enter your phone number", 50, "collect_phone");
                    case "collect_email":
                        return BuildChatResponse(AskEmail, "Enter your email", 75, "collect_email");
                    case "collect_location":
                        return BuildChatResponse(AskLocation, "Enter your location", 90, "collect_location");
                    case "completed":
                        return BuildChatResponse(FinalMessage, "Type your message...", 100, "completed");
                    default:
                        session.Stage = "greeting";
                        return BuildChatResponse(InitialGreeting, "Say hi or hello", 10, "greeting");
                }
            }

            if (session.Stage == "greeting")
            {
                var normalized = message.ToLowerInvariant();
                if (normalized != "hi" && normalized != "hello")
                {
                    return BuildChatResponse("Please say hi or hello to continue.", "Say hi or hello", 10, "greeting");
                }

                session.Stage = "collect_name";
                return BuildChatResponse(AskName, "Enter your name", 25, "collect_name");
            }

            if (session.Stage == "collect_name")
            {
                if (message.Length < 2) return BuildChatResponse("Please enter a valid name.", "Enter your full name", 20, "collect_name");
                session.Name = ToTitleCase(message);
                SaveChatLead(session, false);
                session.Stage = "collect_phone";
                return BuildChatResponse(AskPhone, "Enter your phone number", 50, "collect_phone");
            }

            if (session.Stage == "collect_phone")
            {
                var phone = NormalizePhone(message);
                if (!PhoneRegex.IsMatch(phone)) return BuildChatResponse("Please enter a valid 10-digit phone number.", "Enter your phone number", 50, "collect_phone");
                session.Phone = phone;
                SaveChatLead(session, false);
                session.Stage = "collect_email";
                return BuildChatResponse(AskEmail, "Enter your email", 75, "collect_email");
            }

            if (session.Stage == "collect_email")
            {
                var email = message.ToLowerInvariant();
                if (!EmailRegex.IsMatch(email)) return BuildChatResponse("Please enter a valid email address.", "Enter your email", 75, "collect_email");
                session.Email = email;
                SaveChatLead(session, false);
                session.Stage = "collect_location";
                return BuildChatResponse(AskLocation, "Enter your location", 90, "collect_location");
            }

            if (session.Stage == "collect_location")
            {
                if (message.Length < 2) return BuildChatResponse("Please enter your location.", "Enter your location", 90, "collect_location");
                session.Location = ToTitleCase(message);
                SaveChatLead(session, true);
                session.Stage = "completed";

                return Json(new
                {
                    reply = FinalMessage,
                    options = new string[] { },
                    placeholder = "Type your message...",
                    meta = new { progress = 100, stage = "completed" }
                });
            }

            return BuildChatResponse(FinalMessage, "Type your message...", 100, "completed");
        }

        private ActionResult BuildChatResponse(string reply, string placeholder, int progress, string stage)
        {
            return Json(new
            {
                reply,
                options = new string[] { },
                placeholder,
                meta = new { progress, stage }
            });
        }

        private static string NormalizeSource(string source)
        {
            var normalized = (source ?? "webpage").Trim().ToLowerInvariant();
            if (normalized == "website" || normalized == "website_form") return "webpage";
            return normalized == "chatbot" ? "chatbot" : "webpage";
        }

        private static string NormalizePhone(string value)
        {
            return new string((value ?? "").Where(char.IsDigit).ToArray());
        }

        private static string ToTitleCase(string value)
        {
            var text = (value ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(text)) return "";
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        private void SaveChatLead(ChatSessionState session, bool finalize)
        {
            if (session == null) return;

            var lead = new Lead
            {
                Id = session.LeadId,
                Name = session.Name ?? string.Empty,
                Phone = session.Phone ?? string.Empty,
                Email = session.Email ?? string.Empty,
                Location = session.Location ?? string.Empty,
                InterestedDomain = string.Empty,
                Whatsapp = string.Empty,
                Source = finalize ? "chatbot" : "chatbot_draft",
                CreatedAt = session.CreatedAt == default(DateTime) ? DateTime.UtcNow : session.CreatedAt
            };

            if (StorageMode.UseRuntimeStore())
            {
                if (lead.Id > 0 && RuntimeStore.UpdateLead(lead))
                {
                    session.CreatedAt = lead.CreatedAt;
                    return;
                }

                session.LeadId = RuntimeStore.AddLead(lead);
                session.CreatedAt = lead.CreatedAt;
                return;
            }

            try
            {
                if (lead.Id > 0)
                {
                    var existing = _db.Leads.FirstOrDefault(x => x.Id == lead.Id);
                    if (existing != null)
                    {
                        existing.Name = lead.Name;
                        existing.Phone = lead.Phone;
                        existing.Email = lead.Email;
                        existing.Location = lead.Location;
                        existing.InterestedDomain = lead.InterestedDomain;
                        existing.Whatsapp = lead.Whatsapp;
                        existing.Source = lead.Source;
                        if (existing.CreatedAt == default(DateTime)) existing.CreatedAt = lead.CreatedAt;
                        _db.SaveChanges();
                        DiagnosticLogger.Info("LeadApi.ChatSave", "Updated SQL chat lead. Id=" + existing.Id.ToString(CultureInfo.InvariantCulture) + ", source=" + (existing.Source ?? string.Empty));
                        session.CreatedAt = existing.CreatedAt;
                        return;
                    }
                }

                _db.Leads.Add(lead);
                _db.SaveChanges();
                DiagnosticLogger.Info("LeadApi.ChatSave", "Inserted SQL chat lead. Id=" + lead.Id.ToString(CultureInfo.InvariantCulture) + ", source=" + (lead.Source ?? string.Empty));
                session.LeadId = lead.Id;
                session.CreatedAt = lead.CreatedAt;
            }
            catch (Exception ex)
            {
                if (lead.Id > 0 && RuntimeStore.UpdateLead(lead))
                {
                    DiagnosticLogger.Error("LeadApi.ChatSave", "SQL chat save failed. Updated runtime lead instead. Id=" + lead.Id.ToString(CultureInfo.InvariantCulture), ex);
                    session.CreatedAt = lead.CreatedAt;
                    return;
                }

                session.LeadId = RuntimeStore.AddLead(lead);
                DiagnosticLogger.Error("LeadApi.ChatSave", "SQL chat save failed. Inserted runtime lead instead. RuntimeId=" + session.LeadId.ToString(CultureInfo.InvariantCulture), ex);
                session.CreatedAt = lead.CreatedAt;
            }
        }

        private sealed class ChatSessionState
        {
            public string Stage { get; set; } = "greeting";
            public int LeadId { get; set; }
            public DateTime CreatedAt { get; set; }
            public string Name { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Location { get; set; }
        }

        private sealed class ChatRequest
        {
            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("user_id")]
            public string UserId { get; set; }
        }
    }
}
