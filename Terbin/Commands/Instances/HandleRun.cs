using System.Diagnostics;

namespace Terbin.Commands.Instances;

internal class HandleRun
{
    public static void Run(string[] args)
    {
        if (args.Length < 1)
        {
            Ctx.Log.Warn("Usage: terbin instances run <name> [exe]");
            return;
        }

        var name = args[0];
        if (!Ctx.config!.TryGetInstance(name, out var basePath))
        {
            Ctx.Log.Error($"Instance not found: {name}");
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
            Ctx.Log.Error($"Executable not found: {exePath}");
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
            Ctx.Log.Success($"Launched '{name}' -> {exePath}");
        }
        catch (Exception ex)
        {
            Ctx.Log.Error($"Failed to start process: {ex.Message}");
        }

    }
}