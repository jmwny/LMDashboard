using System.Text.Json;
using LMDashboard.Models;

namespace LMDashboard.Services;

public class LinkStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly string _prefsPath;
    private readonly object _lock = new();
    private readonly ILogger<LinkStore> _logger;
    private List<SiteLink> _links = [];
    private DashboardPreferences _prefs = new();

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
            _links.Add(link);
            Save();
        }
        OnChange?.Invoke();
    }

    public bool Update(SiteLink link)
    {
        bool updated = false;
        lock (_lock)
        {
            var index = _links.FindIndex(l => l.Id == link.Id);
            if (index >= 0)
            {
                link.LastStatusCode = _links[index].LastStatusCode;
                link.LastStatusDescription = _links[index].LastStatusDescription;
                link.LastPingMs = _links[index].LastPingMs;
                link.LastChecked = _links[index].LastChecked;
                link.IsPinging = _links[index].IsPinging;
                link.LastPingStarted = _links[index].LastPingStarted;
                _links[index] = link;
                Save();
                updated = true;
            }
        }
        if (updated)
            OnChange?.Invoke();
        return updated;
    }

    public void Remove(Guid id)
    {
        bool removed = false;
        lock (_lock)
        {
            removed = _links.RemoveAll(l => l.Id == id) > 0;
            if (removed)
                Save();
        }
        if (removed)
            OnChange?.Invoke();
    }

    public void UpdateStatus(Guid id, int? statusCode, string? statusDescription, long? pingMs)
    {
        bool found = false;
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);
            if (link is not null)
            {
                link.LastStatusCode = statusCode;
                link.LastStatusDescription = statusDescription;
                link.LastPingMs = pingMs;
                link.LastChecked = DateTime.UtcNow;
                link.IsPinging = false;
                found = true;
            }
        }
        if (found)
            OnChange?.Invoke();
    }

    public void SetPinging(Guid id)
    {
        bool found = false;
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);
            if (link is not null)
            {
                link.IsPinging = true;
                link.LastPingStarted = DateTime.UtcNow;
                found = true;
            }
        }
        if (found)
            OnChange?.Invoke();
    }

    public void TogglePingEnabled(Guid id)
    {
        bool found = false;
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);
            if (link is not null)
            {
                link.PingEnabled = !link.PingEnabled;
                if (!link.PingEnabled)
                {
                    link.IsPinging = false;
                    link.LastStatusCode = null;
                    link.LastStatusDescription = null;
                    link.LastPingMs = null;
                    link.LastChecked = null;
                    link.LastPingStarted = null;
                }
                Save();
                found = true;
            }
        }
        if (found)
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
            _prefs = prefs.Clone();
            var json = JsonSerializer.Serialize(_prefs, s_jsonOptions);
            WriteAtomic(_prefsPath, json);
        }
        OnPreferencesChange?.Invoke();
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

    private void Save()
    {
        var json = JsonSerializer.Serialize(_links, s_jsonOptions);
        WriteAtomic(_filePath, json);
    }

    private static void WriteAtomic(string path, string contents)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, contents);
        File.Move(tmp, path, overwrite: true);
    }
}
