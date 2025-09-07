using Newtonsoft.Json;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Terbin.Commands;

class BuildManifest : ICommand
{
    public string Name => "bman";

    public string Description => "This command creates the plugin file based on manifest file";

    public void Execution(Ctx ctx, string[] args)
    {
        ctx.Log.Info("Generating plugin file...");

        if (!ctx.existManifest || ctx.manifest == null)
        {
            ctx.Log.Error("Manifest file does not exist or couldn't be read. Aborting generation.");
            return;
        }

        // Basic manifest validations
        if (string.IsNullOrWhiteSpace(ctx.manifest.Name))
        {
            ctx.Log.Error("Manifest Name is missing or empty.");
            return;
        }
        if (string.IsNullOrWhiteSpace(ctx.manifest.GUID))
        {
            ctx.Log.Error("Manifest GUID is missing or empty.");
            return;
        }

        var modVersion = (ctx.manifest.Versions != null && ctx.manifest.Versions.Count > 0)
            ? ctx.manifest.Versions[^1]
            : "0.0.0";
        ctx.Log.Info($"Using version: {modVersion}");

        // Prepare dependency attributes and download libs
        var dependencyAttributes = new StringBuilder();
        var deps = ctx.manifest.Dependencies ?? new List<string>();
        if (deps.Count == 0)
        {
            ctx.Log.Warn("No dependencies declared in the manifest.");
        }
        else
        {
            if (ctx.config == null)
            {
                ctx.Log.Warn("Config not loaded. Skipping dependencies import.");
            }
            else if (ctx.config.index == null)
            {
                ctx.Log.Warn("Config index not available. Skipping dependencies import.");
            }
            else
            {
                ctx.Log.Section("Resolving dependencies");
                int added = 0, failed = 0;
                foreach (var d in deps)
                {
                    if (string.IsNullOrWhiteSpace(d))
                    {
                        ctx.Log.Warn("Encountered empty dependency id. Skipping.");
                        failed++;
                        continue;
                    }

                    Reference? reference = null;
                    try
                    {
                        // Access using indexer as per existing contract. If missing, catch and warn.
                        reference = ctx.config.index[d];
                    }
                    catch
                    {
                        ctx.Log.Warn($"Dependency '{d}' not found in index. Skipping.");
                        failed++;
                        continue;
                    }

                    if (reference == null)
                    {
                        ctx.Log.Warn($"Dependency '{d}' resolved to null reference. Skipping.");
                        failed++;
                        continue;
                    }

                    ctx.Log.Info($"Processing dependency '{d}' ({reference.Name ?? "<no-name>"})...");
                    try
                    {
                        downloadLib(ctx, reference);
                        dependencyAttributes.AppendLine($"[BepInDependency(\"{d}\")]");
                        ctx.Log.Success($"Dependency '{d}' imported successfully.");
                        added++;
                    }
                    catch (Exception ex)
                    {
                        ctx.Log.Error($"Failed to import dependency '{d}': {ex.Message}");
                        failed++;
                    }
                }

                ctx.Log.Info($"Dependencies processed. Added: {added}, Failed/Skipped: {failed}.");
            }
        }

        var plugin = $$"""
        using BepInEx;
        using BepInEx.Logging;

        namespace {{ctx.manifest.Name}};
        
        [BepInPlugin("{{ctx.manifest.GUID}}", "{{ctx.manifest.Name}}", "{{modVersion}}")]
        {{dependencyAttributes.ToString()}}public class Plugin : BaseUnityPlugin
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
        try
        {
            File.WriteAllText(outputPath, plugin);
            ctx.Log.Success($"Plugin generated successfully at: {outputPath}");
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to write plugin file: {ex.Message}");
        }
    }

    private void downloadLib(Ctx ctx, Reference reference)
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

        ctx.Log.Info($"Downloading manifest for {reference.Name ?? reference.GUID} from {reference.manifestUrl}...");
        string manifestJson;
        try
        {
            manifestJson = NetUtil.DownloadString(reference.manifestUrl);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to download manifest: {ex.Message}");
        }

        Manifest? manifest;
        try
        {
            manifest = JsonConvert.DeserializeObject<Manifest>(manifestJson);
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
        if (string.IsNullOrWhiteSpace(manifest.url))
        {
            throw new Exception($"Invalid base url in manifest for {reference.Name} ({reference.GUID})");
        }

        var latestVersion = manifest.Versions.Last();
        var baseUrl = manifest.url.TrimEnd('/');
        var guid = reference.GUID;
        var downloadUrl = $"{baseUrl}/releases/download/v{latestVersion}/{guid}.zip";

        var libsDir = Path.Combine(Environment.CurrentDirectory, "libs");
        var zipPath = Path.Combine(libsDir, $"{guid}.zip");
        Directory.CreateDirectory(libsDir);

        ctx.Log.Info($"Downloading {guid} v{latestVersion} from {downloadUrl}...");
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

        ctx.Log.Info($"Extracting {guid} to '{libsDir}'...");
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

        ctx.Log.Success($"{guid} v{latestVersion} installed.");
    }
}