using System;

namespace Terbin.Commands;

public class InsertFarlandsCommand : ICommand
{
    public string Name => "inf";

    public string Description => "Insert farlands libraries";

    public void Execution(string[] args)
    {
        if (Ctx.config == null)
        {
            Ctx.Log.Error("No config loaded. Please initialize configuration first.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Ctx.config.FarlandsPath))
        {
            Ctx.Log.Error("Farlands path is not configured. Use: config fpath <path>");
            return;
        }

        var managedPath = Path.Combine(Ctx.config.FarlandsPath!, "Farlands_Data", "Managed");
        var libsPath = Path.Combine(Environment.CurrentDirectory, "libs");
        Directory.CreateDirectory(libsPath);

        if (!Directory.Exists(managedPath))
        {
            Ctx.Log.Error($"Managed folder not found: {managedPath}");
            return;
        }

        var dllFiles = Directory.GetFiles(managedPath, "*.dll");
        int copied = 0;
        foreach (var dll in dllFiles)
        {
            var fileName = Path.GetFileName(dll);
            if (fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                continue;
            else if (fileName.StartsWith("mscorlib.", StringComparison.OrdinalIgnoreCase))
                continue;

            var destFile = Path.Combine(libsPath, fileName);
            File.Copy(dll, destFile, true);
            copied++;
        }
        Ctx.Log.Success($"Copied {copied} DLLs from Managed to libs (excluding System.*).");
    }
}
