using System;

namespace Terbin.Commands;

public class InsertFarlands : ICommand
{
    public string Name => "inf";

    public string Description => "Insert farlands libraries";

    public void Execution(Ctx ctx, string[] args)
    {
        if (ctx.config == null)
        {
            ctx.Log.Error("[InsertFarlands] No config loaded. Please initialize configuration first.");
            return;
        }

    if (string.IsNullOrWhiteSpace(ctx.config.FarlandsPath))
    {
        ctx.Log.Error("[InsertFarlands] Farlands path is not configured. Use: config fpath <path>");
        return;
    }

    var managedPath = Path.Combine(ctx.config.FarlandsPath!, "Farlands_Data", "Managed");
        var libsPath = Path.Combine(Environment.CurrentDirectory, "libs");
        Directory.CreateDirectory(libsPath);

        if (!Directory.Exists(managedPath))
        {
            ctx.Log.Error($"[InsertFarlands] Managed folder not found: {managedPath}");
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
            else if (fileName.StartsWith("netstandard.", StringComparison.OrdinalIgnoreCase))
                continue;
            var destFile = Path.Combine(libsPath, fileName);
            File.Copy(dll, destFile, true);
            copied++;
        }
        ctx.Log.Success($"[InsertFarlands] Copied {copied} DLLs from Managed to libs (excluding System.*).");
    }
}
