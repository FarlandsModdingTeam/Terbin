using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Terbin.Data;
using Terbin.Commands.Instances;
using Index = Terbin.Data.Index;

namespace Terbin.Commands;

public class InstancesCommand : ICommand
{
    public string Name => "instances";
    public string Description => "Manage game instances: create, list, run";

    /// <summary>
    /// ______( validarUrl )_____<br />
    /// - Devuelve true si la URL es válida.
    /// </summary>
    /// <param name="e_url_s"></param>
    /// <returns></returns>
    private bool laGordaDeTuMadre(string e_url_s)
    {
        return Uri.TryCreate(e_url_s, UriKind.Absolute, out Uri uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }


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
        args = args.Skip(1).ToArray();
        switch (sub)
        {
            case "create":
                HandleCreate.Create(ctx, args);
                break;
            case "list":
                HandleList.List(ctx, args);
                break;
            case "run":
                HandleRun.Run(ctx, args);
                break;
            case "open":
                HandleOpen.Open(ctx, args);
                break;
            case "delete":
                HandleDelete.Delete(ctx, args);
                break;
            case "add":
                HandleAddMod.AddMod(ctx, args);
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
