using System;
using System.Net;
using Newtonsoft.Json;
using Terbin.Data;

namespace Terbin;

public class Config
{
    [JsonIgnore]
    public static string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin/config.json");
    private string? farlandsPath;
    // Map of instance name -> path
    private Dictionary<string, string> instances { get; set; } = new();

    public string? FarlandsPath
    {
        get => farlandsPath;
        set
        {
            farlandsPath = value;
            Save();
        }
    }

    public bool TryGetInstance(string name, out string path)
    {
        return instances.TryGetValue(name, out path);
    }

    public bool HasInstance(string name)
    {
        return instances.ContainsKey(name);
    }

    public bool ExistInstanceInPath(string path)
    {
        return instances.Values
            .Any(p => string.Equals(Path.GetFullPath(p), path, StringComparison.OrdinalIgnoreCase));
    }

    public Dictionary<string, string> GetInstances()
    {
        return new Dictionary<string, string>(instances);
    }

    public void AddInstance(string name, string path)
    {
        instances.Add(name, path);
        Save();
    }

    public void RemoveInstance(string name)
    {
        instances.Remove(name);
        Save();
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(configPath, json);
    }

}
