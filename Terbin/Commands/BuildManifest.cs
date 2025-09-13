using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Terbin.Data;

namespace Terbin.Commands;

class BuildManifest : ICommand
{
    public string Name => "bman";

    public string Description => "This command creates the plugin file based on manifest file";

    public void Execution(string[] args)
    {
        Ctx.Log.Info("Generating plugin file...");

        if (!Ctx.existManifest || Ctx.manifest == null)
        {
            Ctx.Log.Error("Manifest file does not exist or couldn't be read. Aborting generation.");
            return;
        }

        // Basic manifest validations
        if (string.IsNullOrWhiteSpace(Ctx.manifest.Name))
        {
            Ctx.Log.Error("Manifest Name is missing or empty.");
            return;
        }
        if (string.IsNullOrWhiteSpace(Ctx.manifest.GUID))
        {
            Ctx.Log.Error("Manifest GUID is missing or empty.");
            return;
        }

        if (Ctx.manifest.Type == ProjectManifest.ManifestType.EMPTY)
        {
            executeEmpty( Ctx.manifest);
            return;
        }

        var modVersion = (Ctx.manifest.Versions != null && Ctx.manifest.Versions.Count > 0)
            ? Ctx.manifest.Versions[^1]
            : "0.0.0";
        Ctx.Log.Info($"Using version: {modVersion}");

        // Prepare dependency attributes and download libs
        var dependencyAttributes = new StringBuilder();
        var deps = Ctx.manifest.Dependencies ?? new List<string>();
        if (deps.Count == 0)
        {
            Ctx.Log.Warn("No dependencies declared in the manifest.");
        }
        else
        {
            if (Ctx.config == null)
            {
                Ctx.Log.Warn("Config not loaded. Skipping dependencies import.");
            }
            else if (Ctx.index == null)
            {
                Ctx.Log.Warn("Config index not available. Skipping dependencies import.");
            }
            else
            {
                Ctx.Log.Section("Resolving dependencies");
                int added = 0, failed = 0;
                foreach (var d in deps)
                {
                    if (string.IsNullOrWhiteSpace(d))
                    {
                        Ctx.Log.Warn("Encountered empty dependency id. Skipping.");
                        failed++;
                        continue;
                    }

                    Reference? reference = null;
                    try
                    {
                        // Access using indexer as per existing contract. If missing, catch and warn.
                        reference = Ctx.index[d];
                    }
                    catch
                    {
                        Ctx.Log.Warn($"Dependency '{d}' not found in index. Skipping.");
                        failed++;
                        continue;
                    }

                    if (reference == null)
                    {
                        Ctx.Log.Warn($"Dependency '{d}' resolved to null reference. Skipping.");
                        failed++;
                        continue;
                    }

                    Ctx.Log.Info($"Processing dependency '{d}' ({reference.Name ?? "<no-name>"})...");
                    try
                    {
                        downloadLib(reference);
                        dependencyAttributes.AppendLine($"[BepInDependency(\"{d}\")]");
                        Ctx.Log.Success($"Dependency '{d}' imported successfully.");
                        added++;
                    }
                    catch (Exception ex)
                    {
                        Ctx.Log.Error($"Failed to import dependency '{d}': {ex.Message}");
                        failed++;
                    }
                }

                Ctx.Log.Info($"Dependencies processed. Added: {added}, Failed/Skipped: {failed}.");
            }
        }

        var plugin = $$"""
        using BepInEx;
        using BepInEx.Logging;
        using HarmonyLib;

        namespace {{Ctx.manifest.Name}};
        
        [BepInPlugin("{{Ctx.manifest.GUID}}", "{{Ctx.manifest.Name}}", "{{modVersion}}")]
        {{dependencyAttributes.ToString()}}public class {{Ctx.manifest.Name}}Plugin : BaseUnityPlugin
        {
            internal static new ManualLogSource Logger;
            public Harmony harmony;

            private void Awake()
            {
                // Plugin startup logic
                Logger = base.Logger;
                Logger.LogInfo($"Plugin '{{Ctx.manifest.GUID}}' is loaded!");

                harmony = new Harmony("{{Ctx.manifest.GUID}}");
                harmony.PatchAll();

                Logger.LogInfo("Patches applied");

                gameObject.AddComponent<testMod>().RegisterMod(this);
            }
        }
        """;

        var mod = $$"""
        using BepInEx;
        using BepInEx.Logging;
        using FarlandsCoreMod;

        namespace {{Ctx.manifest.Name}};
        
        public class {{Ctx.manifest.Name}}Mod : Mod
        {
            public void Start()
            {

            }

            public void Update()
            {
            
            }
        }
        """;


        var outputPath = Path.Combine(Environment.CurrentDirectory, "plugin.cs");
        var modPath = Path.Combine(Environment.CurrentDirectory, "mod.cs");
        try
        {
            File.WriteAllText(outputPath, plugin);

            if(!File.Exists(modPath)) File.WriteAllText(modPath, mod);

            Ctx.Log.Success($"Plugin generated successfully at: {outputPath}");
        }
        catch (Exception ex)
        {
            Ctx.Log.Error($"Failed to write plugin file: {ex.Message}");
        }
    }

    private void executeEmpty(ProjectManifest manifest)
    {
        var pluginInfo = $$"""
        using BepInEx;
        using BepInEx.Logging;

        namespace {{manifest.Name}};
        public class PluginInfo
        {
            public const string GUID = "{{manifest.GUID}}"; 
            public const string Name = "{{manifest.Name}}"; 
            public const string Version = "{{manifest.Versions.Last()}}";
        }
        """;

        var pluginInfoPath = Path.Combine(Environment.CurrentDirectory, "PluginInfo.cs");
        try
        {
            File.WriteAllText(pluginInfoPath, pluginInfo);
            Ctx.Log.Success($"Plugin info generated successfully at: {pluginInfoPath}");
        }
        catch (Exception ex)
        {
            Ctx.Log.Error($"Failed to write plugin file: {ex.Message}");
            return;
        }
        var pluginPath = Path.Combine(Environment.CurrentDirectory, "Plugin.cs");

        if (!File.Exists(pluginPath))
        {
            var plugin = $$"""
                using BepInEx;
                using BepInEx.Logging;
                using HarmonyLib;

                namespace {{Ctx.manifest.Name}};
                
                [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
                public class Plugin : BaseUnityPlugin
                {
                    internal static new ManualLogSource Logger;
                    private Harmony harmony;
                    
                    private void Awake()
                    {
                        // Plugin startup logic
                        Logger = base.Logger;
                        Logger.LogInfo($"Plugin '{{Ctx.manifest.GUID}}' is loaded!");

                        harmony = new Harmony(PluginInfo.GUID);
                        harmony.PatchAll();

                        Logger.LogInfo("Patches applied");
                    }
                }
                """;

            try
            {
                File.WriteAllText(pluginPath, plugin);
                Ctx.Log.Success($"Plugin generated successfully at: {pluginPath}");
            }
            catch (Exception ex)
            {
                Ctx.Log.Error($"Failed to write plugin file: {ex.Message}");
                return;
            }
        }

    }

    private void downloadLib(Reference reference)
    {
        // Validate reference fields
        if (reference == null)
        {
            throw new ArgumentNullException(nameof(reference));
        }
        if (string.IsNullOrWhiteSpace(reference.GUID))
        {
            throw new Exception("Reference GUID is missing.");
        }
        if (string.IsNullOrWhiteSpace(reference.manifestUrl))
        {
            throw new Exception($"Manifest URL missing for {reference.Name ?? reference.GUID}.");
        }

        Ctx.Log.Info($"Downloading manifest for {reference.Name ?? reference.GUID} from {reference.manifestUrl}...");
        string manifestJson;
        try
        {
            manifestJson = NetUtil.DownloadString(reference.manifestUrl);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download manifest: {ex.Message}");
        }

        ProjectManifest? manifest;
        try
        {
            manifest = JsonConvert.DeserializeObject<ProjectManifest>(manifestJson);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to parse manifest JSON: {ex.Message}");
        }

        if (manifest == null)
        {
            throw new Exception($"Failed to parse manifest for {reference.Name} ({reference.GUID})");
        }
        if (manifest.Versions == null || manifest.Versions.Count == 0)
        {
            throw new Exception($"No versions found in manifest for {reference.Name} ({reference.GUID})");
        }
        if (string.IsNullOrWhiteSpace(manifest.URL))
        {
            throw new Exception($"Invalid base url in manifest for {reference.Name} ({reference.GUID})");
        }

        var latestVersion = manifest.Versions.Last();
        var baseUrl = manifest.URL.TrimEnd('/');
        var guid = reference.GUID;
        var downloadUrl = $"{baseUrl}/releases/download/v{latestVersion}/{guid}.zip";

        var libsDir = Path.Combine(Environment.CurrentDirectory, "libs");
        var zipPath = Path.Combine(libsDir, $"{guid}.zip");
        Directory.CreateDirectory(libsDir);

        Ctx.Log.Info($"Downloading {guid} v{latestVersion} from {downloadUrl}...");
        try
        {
            NetUtil.DownloadFileWithProgress(downloadUrl, zipPath);
        }
        catch (Exception ex)
        {
            // Clean up partial download if any
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { /* ignore */ }
            throw new Exception($"Failed to download dependency archive: {ex.Message}");
        }

        Ctx.Log.Info($"Extracting {guid} to '{libsDir}'...");
        try
        {
            ZipFile.ExtractToDirectory(zipPath, libsDir, true);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to extract archive: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { /* ignore */ }
        }

        Ctx.Log.Success($"{guid} v{latestVersion} installed.");
    }
}