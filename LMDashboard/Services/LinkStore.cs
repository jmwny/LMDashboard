using System.Text.Json;
using LMDashboard.Models;

namespace LMDashboard.Services;

// Mutations build the new state, write it to disk, and only then swap it in, so a failed
// write (IOException/UnauthorizedAccessException, rethrown to the caller) changes nothing.
public class LinkStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private const int HistoryLength = 20;

    private readonly string _filePath;
    private readonly string _prefsPath;
    private readonly object _lock = new();
    private readonly ILogger<LinkStore> _logger;
    private List<SiteLink> _links = [];
    private DashboardPreferences _prefs = new();
    private long _lastPingId;

    public event Action? OnChange;
    public event Action? OnPreferencesChange;

    public LinkStore(IWebHostEnvironment env, ILogger<LinkStore> logger)
    {
        _logger = logger;
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "links.json");
        _prefsPath = Path.Combine(dataDir, "preferences.json");
        Load();
        LoadPrefs();
    }

    public IReadOnlyList<SiteLink> Links
    {
        get
        {
            lock (_lock)
            {
                return _links.Select(l => l.Clone()).ToList();
            }
        }
    }

    public void Add(SiteLink link)
    {
        lock (_lock)
        {
            var links = new List<SiteLink>(_links) { link.Clone() };
            SaveLinks(links);
            _links = links;
        }
        OnChange?.Invoke();
    }

    public bool Update(SiteLink link)
    {
        lock (_lock)
        {
            var index = _links.FindIndex(l => l.Id == link.Id);
            if (index < 0)
                return false;

            var existing = _links[index];
            var updated = link.Clone();

            // Keep the live status only when the check itself is unchanged; otherwise start
            // fresh so a ping still in flight against the old target is ignored.
            if (updated.PingEnabled && existing.PingEnabled
                && updated.Url == existing.Url && updated.IsExternal == existing.IsExternal)
            {
                updated.LastStatusCode = existing.LastStatusCode;
                updated.LastStatusDescription = existing.LastStatusDescription;
                updated.LastPingMs = existing.LastPingMs;
                updated.LastChecked = existing.LastChecked;
                updated.IsPinging = existing.IsPinging;
                updated.LastPingStarted = existing.LastPingStarted;
                updated.PingId = existing.PingId;
                updated.History = existing.History;
            }
            else
            {
                ResetPingState(updated);
            }
            updated.Version = existing.Version + 1;

            var links = new List<SiteLink>(_links);
            links[index] = updated;
            SaveLinks(links);
            _links = links;
        }
        OnChange?.Invoke();
        return true;
    }

    public void Remove(Guid id)
    {
        lock (_lock)
        {
            var links = _links.Where(l => l.Id != id).ToList();
            if (links.Count == _links.Count)
                return;

            SaveLinks(links);
            _links = links;
        }
        OnChange?.Invoke();
    }

    // Returns an id for the new ping, or null if the link is gone, disabled or already
    // being pinged. Re-checked here because the caller's snapshot may be stale.
    public long? TryStartPing(Guid id)
    {
        long? pingId = null;
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);
            if (link is { PingEnabled: true, IsPinging: false })
            {
                pingId = ++_lastPingId;
                link.PingId = pingId.Value;
                link.IsPinging = true;
                link.LastPingStarted = DateTime.UtcNow;
                link.Version++;
            }
        }
        if (pingId is not null)
            OnChange?.Invoke();
        return pingId;
    }

    public void UpdateStatus(Guid id, long pingId, int? statusCode, string? statusDescription, long? pingMs)
    {
        bool found = false;
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);

            // Drop results from a ping that was superseded by a disable or an edit.
            if (link is not null && link.IsPinging && link.PingId == pingId)
            {
                link.LastStatusCode = statusCode;
                link.LastStatusDescription = statusDescription;
                link.LastPingMs = pingMs;
                link.LastChecked = DateTime.UtcNow;
                link.IsPinging = false;
                link.History = [.. link.History.TakeLast(HistoryLength - 1),
                    new PingSample(pingMs, statusCode is >= 200 and < 400)];
                link.Version++;
                found = true;
            }
        }
        if (found)
            OnChange?.Invoke();
    }

    public void TogglePingEnabled(Guid id)
    {
        lock (_lock)
        {
            var index = _links.FindIndex(l => l.Id == id);
            if (index < 0)
                return;

            var link = _links[index].Clone();
            link.PingEnabled = !link.PingEnabled;
            if (!link.PingEnabled)
                ResetPingState(link);
            link.Version++;

            var links = new List<SiteLink>(_links);
            links[index] = link;
            SaveLinks(links);
            _links = links;
        }
        OnChange?.Invoke();
    }

    public DashboardPreferences LoadPreferences()
    {
        lock (_lock)
        {
            return _prefs.Clone();
        }
    }

    public void SavePreferences(DashboardPreferences prefs)
    {
        lock (_lock)
        {
            var copy = prefs.Clone();
            Write(_prefsPath, JsonSerializer.Serialize(copy, s_jsonOptions));
            _prefs = copy;
        }
        OnPreferencesChange?.Invoke();
    }

    private static void ResetPingState(SiteLink link)
    {
        link.IsPinging = false;
        link.PingId = 0;
        link.LastStatusCode = null;
        link.LastStatusDescription = null;
        link.LastPingMs = null;
        link.LastChecked = null;
        link.LastPingStarted = null;
        link.History = [];
    }

    private void LoadPrefs()
    {
        if (!File.Exists(_prefsPath))
            return;

        try
        {
            var json = File.ReadAllText(_prefsPath);
            _prefs = JsonSerializer.Deserialize<DashboardPreferences>(json) ?? new();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            _logger.LogWarning(ex, "Could not read {Path}; falling back to defaults", _prefsPath);
            _prefs = new();
        }
    }

    private void Load()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            var json = File.ReadAllText(_filePath);
            _links = JsonSerializer.Deserialize<List<SiteLink>>(json) ?? [];
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Keep the unreadable file for inspection so the next Save doesn't overwrite it.
            var backup = _filePath + ".corrupt";
            _logger.LogError(ex, "Could not read {Path}; moving it to {Backup} and starting empty", _filePath, backup);
            try
            {
                File.Move(_filePath, backup, overwrite: true);
            }
            catch (IOException moveEx)
            {
                _logger.LogWarning(moveEx, "Could not move corrupt file {Path}", _filePath);
            }
            _links = [];
        }
    }

    private void SaveLinks(List<SiteLink> links) =>
        Write(_filePath, JsonSerializer.Serialize(links, s_jsonOptions));

    private void Write(string path, string contents)
    {
        try
        {
            WriteAtomic(path, contents);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Could not save {Path}", path);
            throw;
        }
    }

    private static void WriteAtomic(string path, string contents)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, contents);
        File.Move(tmp, path, overwrite: true);
    }
}
