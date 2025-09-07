using System;
using Newtonsoft.Json;
using Terbin.Commands;

namespace Terbin.Data;


public class Index
{
    const string indexUrl = "https://raw.githubusercontent.com/FarlandsModdingTeam/mods/refs/heads/main/mods.json";
    public List<Reference> references = new();

    public Reference Get(string reference) => references.First(r => r.GUID == reference || r.Name == reference);
    public Reference this[string guid] => Get(guid);

    public static void Download(Ctx ctx)
    {
        var log = new Logger();
        try
        {
            log.Info($"Downloading mods index from: {indexUrl}");
            var idx = NetUtil.DownloadString(indexUrl);
            
            var result = JsonConvert.DeserializeObject<List<Reference>>(idx);
            if (result == null)
            {
                log.Error("Failed to parse mods index: deserialized result is null.");
                throw new Exception("Mods index deserialized to null");
            }

            var count = result?.Count ?? 0;
            
            // Ensure config exists
            ctx.config ??= new Config();
            ctx.config.index = new Index() { references = result ?? new List<Reference>() };
            ctx.config.save();
        }
        catch (Exception ex)
        {
            log.Error($"Failed to download or parse mods index: {ex.Message}");
            throw;
        }
    }
}
