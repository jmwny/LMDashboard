namespace LMDashboard.Models;

public class DashboardPreferences
{
    public string Filter { get; set; } = "All";
    public bool Grouped { get; set; }
    public string Theme { get; set; } = "green";
    public bool ShowFailingOnly { get; set; }
}
