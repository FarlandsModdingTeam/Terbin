using System;
using System.Net;
using Newtonsoft.Json;
using Terbin.Data;

namespace Terbin;

public class Config
{
    [JsonIgnore]
    public string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin/config.json");
    private string? farlandsPath;
    // Map of instance name -> path
    public Dictionary<string, string> Instances { get; set; } = new();
    
    public string? FarlandsPath
    {
        get => farlandsPath;
        set
        {
            farlandsPath = value;
            save();
        }
    }

    public void save()
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(configPath, json);
    }

}
