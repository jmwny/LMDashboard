namespace LMDashboard.Models;

public class DashboardPreferences
{
    public string Filter { get; set; } = "All";
    public bool Grouped { get; set; }
    public bool Scanlines { get; set; } = true;
    public bool Vignette { get; set; } = true;
    public bool ShowFailingOnly { get; set; }
    public bool Glitch { get; set; } = true;
    public bool Matrix { get; set; } = true;
    public double Brightness { get; set; } = 1.0;
}
