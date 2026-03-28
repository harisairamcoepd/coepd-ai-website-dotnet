namespace Coepd.Mobile.Models;

public class DashboardStats
{
    public int TotalLeads { get; set; }
    public int TodayLeads { get; set; }
    public int ChatbotLeads { get; set; }
    public int WebsiteLeads { get; set; }
    public string SummaryText =>
        $"Live view: {TotalLeads} total leads, {TodayLeads} today, {ChatbotLeads} chatbot, {WebsiteLeads} website.";
}
