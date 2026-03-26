using Coepd.Web.Models;
using Coepd.Web.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
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

        private const string InitialGreeting = "Welcome to COEPD\nHow can I help you know?";
        private const string DetailsPrompt = "Thank you for reaching out\nTo assist you better, please share your details.";
        private const string AskName = "May I know your name?";
        private const string AskPhone = "Please share your phone number.";
        private const string AskEmail = "Please provide your email address.";
        private const string AskLocation = "Your current location?";
        private const string FinalMessage = "Thank you for sharing your details.\nOur team will reach out to you shortly.";

        [HttpGet]
        public ActionResult Index()
        {
            try
            {
                var leads = _db.Leads
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
                return Json(leads, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                var leads = RuntimeStore.GetLeads()
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

            try
            {
                _db.Leads.Add(lead);
                _db.SaveChanges();
                return Json(new
                {
                    ok = true,
                    success = true,
                    id = lead.Id,
                    source = lead.Source,
                    created_at = lead.CreatedAt
                });
            }
            catch
            {
                var fallbackId = RuntimeStore.AddLead(lead);
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
        public ActionResult Lead(LeadCreateRequest payload) => Index(payload);

        [HttpPost]
        public ActionResult Contact(LeadCreateRequest payload) => Index(payload);

        [HttpPost]
        public ActionResult Enquiry(LeadCreateRequest payload) => Index(payload);

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
                return BuildChatResponse(InitialGreeting, "Type your message...", 5, "greeting");
            }

            var session = ChatSessions.GetOrAdd(userId, _ => new ChatSessionState());

            if (message == "__init__" || message == string.Empty)
            {
                switch (session.Stage)
                {
                    case "collect_name":
                        return BuildChatResponse(AskName, "Enter your full name", 20, "collect_name");
                    case "collect_phone":
                        return BuildChatResponse(AskPhone, "10-digit phone number", 40, "collect_phone");
                    case "collect_email":
                        return BuildChatResponse(AskEmail, "name@example.com", 60, "collect_email");
                    case "collect_location":
                        return BuildChatResponse(AskLocation, "e.g. Hyderabad", 80, "collect_location");
                    case "completed":
                        return BuildChatResponse(FinalMessage, "Type your message...", 100, "completed");
                    default:
                        return BuildChatResponse(InitialGreeting, "Type your message...", 5, "greeting");
                }
            }

            if (session.Stage == "greeting")
            {
                session.Stage = "collect_name";
                return BuildChatResponse(DetailsPrompt + "\n\n" + AskName, "Enter your full name", 20, "collect_name");
            }

            if (session.Stage == "collect_name")
            {
                if (message.Length < 2) return BuildChatResponse("Please enter a valid name.", "Enter your full name", 20, "collect_name");
                session.Name = ToTitleCase(message);
                session.Stage = "collect_phone";
                return BuildChatResponse(AskPhone, "10-digit phone number", 40, "collect_phone");
            }

            if (session.Stage == "collect_phone")
            {
                var phone = NormalizePhone(message);
                if (!PhoneRegex.IsMatch(phone)) return BuildChatResponse("Please enter a valid 10-digit phone number.", "e.g. 9876543210", 40, "collect_phone");
                session.Phone = phone;
                session.Stage = "collect_email";
                return BuildChatResponse(AskEmail, "name@example.com", 60, "collect_email");
            }

            if (session.Stage == "collect_email")
            {
                var email = message.ToLowerInvariant();
                if (!EmailRegex.IsMatch(email)) return BuildChatResponse("Please enter a valid email address.", "name@example.com", 60, "collect_email");
                session.Email = email;
                session.Stage = "collect_location";
                return BuildChatResponse(AskLocation, "e.g. Hyderabad", 80, "collect_location");
            }

            if (session.Stage == "collect_location")
            {
                if (message.Length < 2) return BuildChatResponse("Please enter your current location.", "e.g. Hyderabad", 80, "collect_location");
                session.Location = ToTitleCase(message);
                session.Stage = "completed";

                return Json(new
                {
                    reply = FinalMessage,
                    options = new string[] { },
                    placeholder = "Type your message...",
                    meta = new { progress = 100, stage = "completed" },
                    lead_payload = new
                    {
                        name = session.Name ?? "",
                        phone = session.Phone ?? "",
                        email = session.Email ?? "",
                        location = session.Location ?? "",
                        interested_domain = "",
                        whatsapp = "",
                        source = "chatbot"
                    }
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

        private static string NormalizePhone(string value) => new string((value ?? "").Where(char.IsDigit).ToArray());

        private static string ToTitleCase(string value)
        {
            var text = (value ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(text)) return "";
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        private sealed class ChatSessionState
        {
            public string Stage { get; set; } = "greeting";
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
