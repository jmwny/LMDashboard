using System.Text.Json.Serialization;

namespace LMDashboard.Models;

public class SiteLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
    public int PingIntervalSeconds { get; set; }
    public bool PingEnabled { get; set; } = true;

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

    // When the last ping started; scheduling keys off this rather than
    // LastChecked (completion time) so slow pings don't stretch the interval.
    [JsonIgnore]
    public DateTime? LastPingStarted { get; set; }

    // Identifies the ping in flight; a result is only recorded if this still matches.
    [JsonIgnore]
    public long PingId { get; set; }

    // Recent results, oldest first. Replaced rather than mutated, so the shallow
    // Clone below can share the array safely.
    [JsonIgnore]
    public PingSample[] History { get; set; } = [];

    // Bumped by LinkStore on every change so the UI can skip re-rendering unchanged rows.
    [JsonIgnore]
    public long Version { get; set; }

    public SiteLink Clone() => (SiteLink)MemberwiseClone();
}

public readonly record struct PingSample(long? Ms, bool Ok);
