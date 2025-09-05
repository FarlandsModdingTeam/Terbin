using Newtonsoft.Json;

namespace Terbin.Commands;

class BuildManifest : ICommand
{
    public string Name => "bman";

    public string Description => "This command creates the plugin file based on manifest file";

    public void Execution(Ctx ctx, string[] args)
    {
        ctx.Log.Section("Plugin file generation");

        if (!ctx.existManifest || ctx.manifest == null)
        {
            ctx.Log.Error("Manifest file does not exist or couldn't be read. Aborting generation.");
            return;
        }

        var modVersion = (ctx.manifest.Versions != null && ctx.manifest.Versions.Count > 0)
            ? ctx.manifest.Versions[^1]
            : "0.0.0";

        var plugin = $$"""
        using BepInEx;
        using BepInEx.Logging;

        namespace {{ctx.manifest.Name}};
        
        [BepInPlugin("{{ctx.manifest.GUID}}", "{{ctx.manifest.Name}}", "{{modVersion}}")]
        public class Plugin : BaseUnityPlugin
        {
            internal static new ManualLogSource Logger;
                
            private void Awake()
            {
                // Plugin startup logic
                Logger = base.Logger;
                Logger.LogInfo($"Plugin '{{ctx.manifest.GUID}}' is loaded!");
            }
        }
        """;

        var outputPath = Path.Combine(Environment.CurrentDirectory, "plugin.cs");
    File.WriteAllText(outputPath, plugin);
    ctx.Log.Success($"Plugin generated successfully at: {outputPath}");
    }
}