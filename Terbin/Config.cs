using System;
using Newtonsoft.Json;

namespace Terbin;

public class Config
{
    [JsonIgnore]
    public string configPath = Path.Combine(Environment.CurrentDirectory, ".terbin");
    private string? farlandsPath;
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
