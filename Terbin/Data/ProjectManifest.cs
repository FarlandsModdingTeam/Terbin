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

    private string name = "";
    private ManifestType type = ManifestType.NORMAL;
    private string guid = "";
    private List<string> versions = [];
    private string url = "";
    private List<string> dependencies = [];

    public string Name { get => name; set { name = value; Save(); } }
    public ManifestType Type { get => type; set { type = value; Save(); } }
    public string GUID { get => guid; set { guid = value; Save(); } }
    public List<string> Versions { get => versions; set { versions = value; Save(); } }
    public string URL { get => url; set { url = value; Save(); } }
    public List<string> Dependencies { get => dependencies; set { dependencies = value; Save(); } }
    public void Save()
    {
        var json = JsonConvert.SerializeObject(this);
        File.WriteAllText(ManifestPath, json);
    }
}