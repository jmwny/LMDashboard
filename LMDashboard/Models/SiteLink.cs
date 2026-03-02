using System.Text.Json.Serialization;

namespace LMDashboard.Models;

public class SiteLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
    public int PingIntervalSeconds { get; set; }

    [JsonIgnore]
    public int? LastStatusCode { get; set; }

    [JsonIgnore]
    public string? LastStatusDescription { get; set; }

    [JsonIgnore]
    public long? LastPingMs { get; set; }

    [JsonIgnore]
    public DateTime? LastChecked { get; set; }

    [JsonIgnore]
    public bool IsPinging { get; set; }
}
