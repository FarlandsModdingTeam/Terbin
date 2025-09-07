using System;
using System.Net;
using Newtonsoft.Json;
using Terbin.Data;
using Index = Terbin.Data.Index;

namespace Terbin;

public class Config
{
    [JsonIgnore]
    public string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");
    private string? farlandsPath;
    // Map of instance name -> path
    public Dictionary<string, string> Instances { get; set; } = new();
    public Index? index = null;
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
