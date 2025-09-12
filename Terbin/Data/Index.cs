using System;
using Newtonsoft.Json;
using Terbin.Commands;

namespace Terbin.Data;


public class Index
{
    public WebIndex webIndex = new();
    public LocalIndex localIndex = new();

    public Reference? Get(string reference)
    {
        var localResult = localIndex[reference];

        if (localResult != null) return localResult;

        return webIndex[reference];
    }

    public void Setup()
    {
        localIndex = new();
        webIndex = new();

        if (!localIndex.ExistLocalPath) localIndex.Init();
        else localIndex.LoadFromLocal();

        if (!webIndex.ExistLocalPath) webIndex.DownloadIndex();
        else webIndex.LoadFromLocal();
    }

    public void LoadFromLocal()
    {
        localIndex.LoadFromLocal();
        webIndex.LoadFromLocal();
    }

    public Reference? this[string guid] => Get(guid);
}

public abstract class BasicIndex
{
    public abstract string localPath { get; }

    public string LocalPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), localPath);
    public bool ExistLocalPath => File.Exists(LocalPath);

    public List<Reference> references = new();

    public void Init()
    {
        references = new();
        Save();
    }

    public Reference? Get(string reference)
    {
        foreach (var r in references)
        {
            if (r.GUID == reference || r.Name == reference)
                return r;
        }

        return null;
    }
    public Reference? this[string guid] => Get(guid);

    public void LoadFromLocal()
    {

        var json = File.ReadAllText(LocalPath);
        references = JsonConvert.DeserializeObject<List<Reference>>(json);
    }

    public void Save()
    {
        var json = JsonConvert.SerializeObject(references);
        File.WriteAllText(LocalPath, json);
    }
}

public class WebIndex : BasicIndex
{
    [JsonIgnore]
    const string indexUrl = "https://raw.githubusercontent.com/FarlandsModdingTeam/mods/refs/heads/main/mods.json";

    public override string localPath => ".terbin/web.index";

    /// <summary>
    /// Descargar el indice de mods de FarlandsCoreMod.
    /// </summary>
    public void DownloadIndex()
    {
        var indexJson = NetUtil.DownloadString(indexUrl);
        references = JsonConvert.DeserializeObject<List<Reference>>(indexJson);

        Save();
    }

}

public class LocalIndex : BasicIndex
{
    public override string localPath => ".terbin/local.index";

    public void AddReference(Reference reference)
    {
        references.Add(reference);
        Save();
    }
}