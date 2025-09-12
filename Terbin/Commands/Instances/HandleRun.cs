using System.Diagnostics;

namespace Terbin.Commands.Instances;

internal class HandleRun
{
    public static void Run(Ctx ctx, string[] args)
    {
        if (args.Length < 1)
        {
            ctx.Log.Warn("Usage: terbin instances run <name> [exe]");
            return;
        }

        var name = args[0];
        if (!ctx.config!.TryGetInstance(name, out var basePath))
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
}