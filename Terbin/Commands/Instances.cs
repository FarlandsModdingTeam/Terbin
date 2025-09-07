using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Terbin.Data;
using Index = Terbin.Data.Index;

namespace Terbin.Commands;

public class Instances : ICommand
{
    public string Name => "instances";
    public string Description => "Manage game instances: create, list, run";

    public void Execution(Ctx ctx, string[] args)
    {
        if (ctx.config == null)
        {
            ctx.Log.Error("Config not loaded.");
            return;
        }

        if (args.Length == 0)
        {
            PrintUsage(ctx);
            return;
        }

        var sub = args[0].ToLowerInvariant();
        switch (sub)
        {
            case "create":
                HandleCreate(ctx, args.Skip(1).ToArray());
                break;
            case "list":
                HandleList(ctx);
                break;
            case "run":
                HandleRun(ctx, args.Skip(1).ToArray());
                break;
            case "open":
                HandleOpen(ctx, args.Skip(1).ToArray());
                break;
            case "delete":
                HandleDelete(ctx, args.Skip(1).ToArray());
                break;
            case "add":
                HandleAdd(ctx, args.Skip(1).ToArray());
                break;
            default:
                ctx.Log.Error($"Unknown subcommand: {sub}");
                PrintUsage(ctx);
                break;
        }
    }

    private static void PrintUsage(Ctx ctx)
    {
        ctx.Log.Info("Usage:");
        ctx.Log.Info("  terbin instances create <name> <path>");
        ctx.Log.Info("  terbin instances list");
        ctx.Log.Info("  terbin instances run <name> [exe]");
        ctx.Log.Info("  terbin instances open <name> [subpath]");
        ctx.Log.Info("  terbin --instances open <name> [subpath]");
        ctx.Log.Info("  terbin instances delete <name> [-y]");
        ctx.Log.Info("  terbin instances add <name> <guid|name>");
        ctx.Log.Info("");
        ctx.Log.Info("Notes:");
        ctx.Log.Info("  - <path> is the folder for the instance.");
        ctx.Log.Info("  - [exe] optional relative or absolute path to the game executable; defaults to 'Farlands.exe' inside the instance folder.");
        ctx.Log.Info("  - [subpath] optional folder or file to reveal/open; relative paths are resolved inside the instance.");
        ctx.Log.Info("  - 'add' only records the mod GUID into manifest.json; it does not install files.");
    }

    private static void HandleCreate(Ctx ctx, string[] args)
    {
        if (args.Length < 2)
        {
            ctx.Log.Warn("Not enough arguments. Usage: terbin instances create <name> <path>");
            return;
        }

        var name = args[0];
        var path = args[1];

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
        {
            ctx.Log.Error("Name and path must be provided.");
            return;
        }

        // Require FarlandsPath to be set and exist
        var src = ctx.config!.FarlandsPath;
        if (string.IsNullOrWhiteSpace(src) || !Directory.Exists(src))
        {
            ctx.Log.Error("Farlands path is not configured or does not exist. Set it with 'terbin config fpath <path>'.");
            return;
        }

        // Normalize paths
        src = Path.GetFullPath(src);
        var dest = Path.GetFullPath(path);

        if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Log.Error("Destination path cannot be the same as the Farlands source path.");
            return;
        }

        // Prevent nesting (dest inside src) which would cause infinite recursion
        if (dest.StartsWith(src + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Log.Error("Destination path cannot be inside the Farlands source path.");
            return;
        }

        // Disallow creating if there is already an instance at the requested path (registered or detected by manifest)
        if (ctx.config.Instances.Values.Any(p => string.Equals(Path.GetFullPath(p), dest, StringComparison.OrdinalIgnoreCase)))
        {
            ctx.Log.Error($"There is already an instance registered at '{dest}'.");
            return;
        }

        // Prepare destination directory
        if (!Directory.Exists(dest))
        {
            Directory.CreateDirectory(dest);
        }
        else
        {
            // If destination already looks like an instance, abort with error
            var existingManifest = Path.Combine(dest, "manifest.json");
            if (File.Exists(existingManifest))
            {
                ctx.Log.Error($"Destination '{dest}' already contains an instance (manifest.json found).");
                return;
            }

            // If not empty, confirm merge/overwrite
            var hasAny = Directory.EnumerateFileSystemEntries(dest).Any();
            if (hasAny)
            {
                var ok = ctx.Log.Confirm($"Destination '{dest}' is not empty. Merge and overwrite files?", defaultNo: false);
                if (!ok)
                {
                    ctx.Log.Warn("Aborted by user.");
                    return;
                }
            }
        }

        try
        {
            ctx.Log.Info($"Cloning from '{src}' to '{dest}'...");
            // Progress bar during clone
            FileOps.CopyDirectoryWithProgress(src, dest, overwrite: true, (current, total) =>
            {
                ProgressUtil.DrawProgressBar(current, total);
            });
            // Ensure we end the progress line
            Console.WriteLine();
            ctx.Log.Success("Clone completed.");

            // Install BepInEx
            InstallBepInEx(ctx, dest);
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to clone files: {ex.Message}");
            return;
        }

        if (ctx.config!.Instances.ContainsKey(name))
        {
            ctx.Log.Warn($"Instance '{name}' already exists. Updating path.");
        }

        ctx.config!.Instances[name] = dest;
        ctx.config.save();

        // Generate instance manifest base
        try
        {
            GenerateInstanceManifest(dest, name);
            ctx.Log.Success("Instance manifest created.");
        }
        catch (Exception mex)
        {
            ctx.Log.Warn($"Failed to write instance manifest: {mex.Message}");
        }

        ctx.Log.Success($"Instance '{name}' created at '{dest}'.");
    }

    private static void GenerateInstanceManifest(string dest, string name)
    {
        var manifestPath = Path.Combine(dest, "manifest.json");
        var instanceManifest = new InstanceManifest
        {
            Name = name,
            Version = "1.0.0",
            Mods = new List<string>()
        };
        var json = JsonConvert.SerializeObject(instanceManifest, Formatting.Indented);
        File.WriteAllText(manifestPath, json);
    }
    private static bool InstallMod(Ctx ctx, Reference mod, string dest)
    {
        var res = true;
        string tmpZip = Path.Combine(Path.GetTempPath(), $"{mod.Name}_{Guid.NewGuid():N}.zip");
        try
        {
            ctx.Log.Info($"Downloading {mod.Name}... :: {tmpZip}");
            var manifesJson = NetUtil.DownloadString(mod.manifestUrl);
            var manifest = JsonConvert.DeserializeObject<ProjectManifest>(manifesJson);
            var url = Path.Combine(manifest.url, $"releases/download/v{manifest.Versions.Last()}/{manifest.Name}.zip");
            NetUtil.DownloadFileWithProgress(url, tmpZip);
            Console.WriteLine("");
            ctx.Log.Info($"Extracting {mod.Name}...");
            System.IO.Compression.ZipFile.ExtractToDirectory(tmpZip, dest, overwriteFiles: true);
            ctx.Log.Success($"{mod.Name} installed.");
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to install {mod.Name}: {ex.Message}");
            res = false;
        }
        finally
        {
            try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { /* ignore */ }
        }

        return res;
    }
    private static void InstallBepInEx(Ctx ctx, string dest)
    {
        const string url = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip";

        var bepinFolder = Path.Combine(dest, "BepInEx");
        var doorstopCfg = Path.Combine(dest, "doorstop_config.ini");
        var winhttp = Path.Combine(dest, "winhttp.dll");

        bool exists = Directory.Exists(bepinFolder) || File.Exists(doorstopCfg) || File.Exists(winhttp);
        if (exists)
        {
            var overwrite = ctx.Log.Confirm("BepInEx appears to be installed. Reinstall/overwrite?", defaultNo: false);
            if (!overwrite)
            {
                ctx.Log.Info("Skipping BepInEx installation.");
                return;
            }
        }

        string tmpZip = Path.Combine(Path.GetTempPath(), $"bepinex_{Guid.NewGuid():N}.zip");
        try
        {
            ctx.Log.Info("Downloading BepInEx...");
            NetUtil.DownloadFileWithProgress(url, tmpZip);
            Console.WriteLine();

            ctx.Log.Info("Extracting BepInEx...");
            System.IO.Compression.ZipFile.ExtractToDirectory(tmpZip, dest, overwriteFiles: true);
            ctx.Log.Success("BepInEx installed.");
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to install BepInEx: {ex.Message}");
        }
        finally
        {
            try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { /* ignore */ }
        }
    }

    private static void HandleAdd(Ctx ctx, string[] args)
    {
        if (args.Length < 2)
        {
            ctx.Log.Warn("Usage: terbin instances add <instanceName> <mod>");
            return;
        }
        var key = args[0];
        var mod = args[1];
        if (ctx.config == null)
        {
            ctx.Log.Error("Config not loaded.");
            return;
        }
        if (!ctx.config.Instances.TryGetValue(key, out var instPath))
        {
            ctx.Log.Error($"Instance not found: {key}");
            return;
        }
        HandleUnicAdd(ctx, new KeyValuePair<string, string>(key, instPath), mod);
    }

    private static void HandleUnicAdd(Ctx ctx, KeyValuePair<string, string> instance, string mod)
    {
        try
        {
            ctx.Log.Info($"Preparing to add mod '{mod}' to instance '{instance.Key}' at '{instance.Value}'.");
            ctx.Log.Info("Loading mods index...");
            if (ctx.config.index == null) Index.Download(ctx);
            ctx.Log.Success("Mods index loaded.");

            var reference = ctx.config.index[mod];
            var modGuid = reference.GUID;
            if (string.IsNullOrWhiteSpace(modGuid))
            {
                ctx.Log.Error("Selected mod has no GUID or Name.");
                return;
            }

            // Check if mod is already registered in the instance manifest before installing
            var manifestPath = Path.Combine(instance.Value, "manifest.json");
            InstanceManifest manifest;
            try
            {
                if (File.Exists(manifestPath))
                {
                    manifest = JsonConvert.DeserializeObject<InstanceManifest>(File.ReadAllText(manifestPath)) ?? new InstanceManifest();
                }
                else
                {
                    manifest = new InstanceManifest();
                }
            }
            catch
            {
                manifest = new InstanceManifest();
            }
            manifest.Mods ??= new List<string>();
            if (manifest.Mods.Contains(modGuid, StringComparer.OrdinalIgnoreCase))
            {
                ctx.Log.Error($"Mod already installed: {modGuid}");
                return;
            }

            string dest = Path.Combine(instance.Value, "BepInEx");
            if (InstallMod(ctx, reference, dest)) 
            {
                AddModGuidToManifest(instance.Value, instance.Key, modGuid);
                ctx.Log.Success($"Added mod GUID to manifest: {modGuid}");
            }
            
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to add mod: {ex.Message}");
        }
    }

    private static void AddModGuidToManifest(string instanceRoot, string defaultName, string modGuid)
    {
        var manifestPath = Path.Combine(instanceRoot, "manifest.json");
        InstanceManifest manifest;
        try
        {
            if (File.Exists(manifestPath))
            {
                manifest = JsonConvert.DeserializeObject<InstanceManifest>(File.ReadAllText(manifestPath)) ?? new InstanceManifest();
            }
            else
            {
                manifest = new InstanceManifest();
            }
        }
        catch
        {
            manifest = new InstanceManifest();
        }

        manifest.Name = string.IsNullOrWhiteSpace(manifest.Name) ? defaultName : manifest.Name;
        manifest.Version = string.IsNullOrWhiteSpace(manifest.Version) ? "1.0.0" : manifest.Version;
        manifest.Mods ??= new List<string>();

        if (!manifest.Mods.Contains(modGuid, StringComparer.OrdinalIgnoreCase))
        {
            manifest.Mods.Add(modGuid);
        }

        var json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
        File.WriteAllText(manifestPath, json);

    }

    private static void HandleList(Ctx ctx)
    {
        var items = ctx.config!.Instances.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"- {kv.Key}: {kv.Value}");
        ctx.Log.Box("Instances", items);
    }

    private static void HandleRun(Ctx ctx, string[] args)
    {
        if (args.Length < 1)
        {
            ctx.Log.Warn("Usage: terbin instances run <name> [exe]");
            return;
        }

        var name = args[0];
        if (!ctx.config!.Instances.TryGetValue(name, out var basePath))
        {
            ctx.Log.Error($"Instance not found: {name}");
            return;
        }

        string? exeArg = args.Length >= 2 ? args[1] : null;

        string exePath;
        if (!string.IsNullOrWhiteSpace(exeArg))
        {
            exePath = Path.IsPathRooted(exeArg) ? exeArg : Path.Combine(basePath, exeArg);
        }
        else
        {
            exePath = Path.Combine(basePath, "Farlands.exe");
        }

        if (!File.Exists(exePath))
        {
            ctx.Log.Error($"Executable not found: {exePath}");
            return;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = basePath,
                UseShellExecute = true
            };
            Process.Start(psi);
            ctx.Log.Success($"Launched '{name}' -> {exePath}");
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to start process: {ex.Message}");
        }
    }

    private static void HandleDelete(Ctx ctx, string[] args)
    {
        if (args.Length < 1)
        {
            ctx.Log.Warn("Usage: terbin instances delete <name> [-y]");
            return;
        }

        bool autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
        // First non-flag argument is treated as the instance name
        var name = args.FirstOrDefault(a => !a.StartsWith("-")) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            ctx.Log.Warn("Usage: terbin instances delete <name> [-y]");
            return;
        }

        if (ctx.config == null)
        {
            ctx.Log.Error("Config not loaded.");
            return;
        }

        if (!ctx.config.Instances.TryGetValue(name, out var path))
        {
            ctx.Log.Error($"Instance not found: {name}");
            return;
        }

        if (!autoYes)
        {
            var ok = ctx.Log.Confirm($"Remove instance '{name}' from config? Files will not be deleted. Path: '{path}'.", defaultNo: true);
            if (!ok)
            {
                ctx.Log.Info("Cancelled.");
                return;
            }
        }

        ctx.config.Instances.Remove(name);
        ctx.config.save();
        ctx.Log.Success($"Instance '{name}' removed.");
    }

    private static void HandleOpen(Ctx ctx, string[] args)
    {
        if (args.Length < 1)
        {
            ctx.Log.Warn("Usage: terbin instances open <name> [subpath]");
            return;
        }

        var name = args[0];
        if (!ctx.config!.Instances.TryGetValue(name, out var basePath))
        {
            ctx.Log.Error($"Instance not found: {name}");
            return;
        }

        string target = basePath;
        if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
        {
            var sub = args[1];
            target = Path.IsPathRooted(sub) ? sub : Path.Combine(basePath, sub);
        }

        target = Path.GetFullPath(target);

        // If target doesn't exist, try parent if it's a file path; otherwise warn
        bool isDir = Directory.Exists(target);
        bool isFile = File.Exists(target);

        if (!isDir && !isFile)
        {
            ctx.Log.Error($"Path not found: {target}");
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
            {
                if (isFile)
                {
                    // Reveal file in Explorer
                    var psi = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"/select,\"{target}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                else
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{target}\"",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = isFile ? $"-R \"{target}\"" : $"\"{target}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            else if (OperatingSystem.IsLinux())
            {
                var toOpen = isFile ? Path.GetDirectoryName(target)! : target;
                var psi = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{toOpen}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            else
            {
                // Fallback: try opening directly
                var toOpen = isFile ? Path.GetDirectoryName(target)! : target;
                var psi = new ProcessStartInfo { FileName = toOpen, UseShellExecute = true };
                Process.Start(psi);
            }

            ctx.Log.Success($"Opened: {target}");
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to open: {ex.Message}");
        }
    }
}

internal static class FileOps
{
    public static void CopyDirectoryWithProgress(string sourceDir, string destDir, bool overwrite, Action<int, int>? onProgress)
    {
        // Gather all files to compute progress
        var allFiles = Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories).ToList();
        var total = allFiles.Count;

        // Create root target
        Directory.CreateDirectory(destDir);

        // Copy files with progress
        int current = 0;
        foreach (var file in allFiles)
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var destFile = Path.Combine(destDir, rel);
            var destFolder = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(destFolder)) Directory.CreateDirectory(destFolder);

            File.Copy(file, destFile, overwrite);
            current++;
            onProgress?.Invoke(current, total);
        }

        // Create any empty directories that had no files
        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, dir);
            var destSub = Path.Combine(destDir, rel);
            if (!Directory.Exists(destSub)) Directory.CreateDirectory(destSub);
        }
    }

    public static void CopyDirectory(string sourceDir, string destDir, bool overwrite)
    {
        // Create target
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(file);
            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, overwrite);
        }

        foreach (var dir in Directory.EnumerateDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
        {
            var dirName = Path.GetFileName(dir);
            var destSub = Path.Combine(destDir, dirName);
            CopyDirectory(dir, destSub, overwrite);
        }
    }
}

internal static class ProgressUtil
{
    public static void DrawProgressBar(int current, int total)
    {
        // Avoid division by zero
        double ratio = total <= 0 ? 1.0 : (double)current / total;
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        int width = 30;
        int filled = (int)Math.Round(ratio * width);
        if (filled > width) filled = width;
        var bar = new string('#', filled) + new string('-', width - filled);
        var percent = (int)Math.Round(ratio * 100);
        Console.Write($"\r[{bar}] {current}/{total} ({percent}%)");
    }

    public static void DrawByteProgress(long current, long total)
    {
        double ratio = total <= 0 ? 0 : (double)current / total;
        ratio = Math.Clamp(ratio, 0.0, 1.0);
        int width = 30;
        int filled = (int)Math.Round(ratio * width);
        if (filled > width) filled = width;
        var bar = new string('#', filled) + new string('-', width - filled);
        var percent = (int)Math.Round(ratio * 100);
        Console.Write($"\r[{bar}] {FormatBytes(current)}/{FormatBytes(total)} ({percent}%)");
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

internal static class NetUtil
{
    public static void DownloadFileWithProgress(string url, string destination)
    {
        using var client = new HttpClient();
        using var response = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var fs = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            fs.Write(buffer, 0, read);
            totalRead += read;
            if (total > 0)
                ProgressUtil.DrawByteProgress(totalRead, total);
        }
    }

    public static string DownloadString(string url)
    {
        using var client = new HttpClient();
        using var response = client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }
}
