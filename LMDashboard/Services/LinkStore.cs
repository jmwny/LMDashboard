using System.Text.Json;
using LMDashboard.Models;

namespace LMDashboard.Services;

public class LinkStore
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly string _prefsPath;
    private readonly object _lock = new();
    private List<SiteLink> _links = [];

    public event Action? OnChange;

    public LinkStore(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);
        _filePath = Path.Combine(dataDir, "links.json");
        _prefsPath = Path.Combine(dataDir, "preferences.json");
        Load();
    }

    public IReadOnlyList<SiteLink> Links
    {
        get
        {
            lock (_lock)
            {
                return _links.ToList();
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

    public void Update(SiteLink link)
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
                _links[index] = link;
                Save();
                updated = true;
            }
        }
        if (updated)
            OnChange?.Invoke();
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
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);
            if (link is not null)
            {
                link.LastStatusCode = statusCode;
                link.LastStatusDescription = statusDescription;
                link.LastPingMs = pingMs;
                link.LastChecked = DateTime.Now;
                link.IsPinging = false;
            }
        }
        OnChange?.Invoke();
    }

    public void SetPinging(Guid id)
    {
        lock (_lock)
        {
            var link = _links.Find(l => l.Id == id);
            if (link is not null)
            {
                link.IsPinging = true;
            }
        }
        OnChange?.Invoke();
    }

    public DashboardPreferences LoadPreferences()
    {
        lock (_lock)
        {
            if (File.Exists(_prefsPath))
            {
                var json = File.ReadAllText(_prefsPath);
                return JsonSerializer.Deserialize<DashboardPreferences>(json) ?? new();
            }
            return new();
        }
    }

    public void SavePreferences(DashboardPreferences prefs)
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(prefs, s_jsonOptions);
            File.WriteAllText(_prefsPath, json);
        }
    }

    private void Load()
    {
        if (File.Exists(_filePath))
        {
            var json = File.ReadAllText(_filePath);
            _links = JsonSerializer.Deserialize<List<SiteLink>>(json) ?? [];
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_links, s_jsonOptions);
        File.WriteAllText(_filePath, json);
    }
}
