using Newtonsoft.Json;

namespace Terbin.Data;

public class ProjectManifest
{
    [JsonIgnore]
    public static string ManifestPath = Path.Combine(Environment.CurrentDirectory, "manifest.json");

    public enum ManifestType
    {
        NORMAL,
        EMPTY
    }

    [JsonProperty("Name")] private string name = "";
    [JsonProperty("Type")] private ManifestType type = ManifestType.NORMAL;
    [JsonProperty("GUID")] private string guid = "";
    [JsonProperty("Versions")] private List<string> versions = [];
    [JsonProperty] private string url = "";
    [JsonProperty("Dependencies")] private List<string> dependencies = [];

    [JsonIgnore] public string Name { get => name; set { name = value; Save(); } }

    [JsonIgnore] public ManifestType Type { get => type; set { type = value; Save(); } }

    [JsonIgnore] public string GUID { get => guid; set { guid = value; Save(); } }

    [JsonIgnore] public List<string> Versions { get => versions; set { versions = value; Save(); } }
    [JsonIgnore] public string URL { get => url; set { url = value; Save(); } }
    [JsonIgnore] public List<string> Dependencies { get => dependencies; set { dependencies = value; Save(); } }
    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(ManifestPath, json);
    }
}