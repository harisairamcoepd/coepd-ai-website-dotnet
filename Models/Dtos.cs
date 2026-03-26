namespace Coepd.Web.Models
{
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class LeadCreateRequest
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Location { get; set; }
        public string InterestedDomain { get; set; }
        public string Whatsapp { get; set; }
        public string Source { get; set; }
    }
}
