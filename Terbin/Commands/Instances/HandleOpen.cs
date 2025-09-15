using System.Diagnostics;

namespace Terbin.Commands.Instances;

internal class HandleOpen : IExecutable
{
    public override string Section => "INSTANCES OPEN";

    public override void Execution()
    {
        if (args.Length < 1)
        {
            Ctx.Log.Warn("Usage: terbin instances open <name> [subpath]");
            return;
        }

        var name = args[0];
        if (!Ctx.config!.TryGetInstance(name, out var basePath))
        {
            Ctx.Log.Error($"Instance not found: {name}");
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
            Ctx.Log.Error($"Path not found: {target}");
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

            Ctx.Log.Success($"Opened: {target}");
        }
        catch (Exception ex)
        {
            Ctx.Log.Error($"Failed to open: {ex.Message}");
        }

    }
}