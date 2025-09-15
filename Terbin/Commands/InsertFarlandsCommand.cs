using System;

namespace Terbin.Commands;

public class InsertFarlandsCommand : AbstractCommand
{
    public override string Name => "inf";

    public string Description => "Insert farlands libraries";
    public override bool HasErrors()
    {
        if (Checkers.IsConfigNull()) return true;
        if (Checkers.IsNullOrWhiteSpace(Ctx.config.FarlandsPath)) return true;

        var managedPath = Path.Combine(Ctx.config.FarlandsPath!, "Farlands_Data", "Managed");
        if (Checkers.NotExistDirectory(managedPath)) return true;
        return false;
    }
    public override void Execution()
    {

        var managedPath = Path.Combine(Ctx.config.FarlandsPath!, "Farlands_Data", "Managed");
        var libsPath = Path.Combine(Environment.CurrentDirectory, "libs");
        Directory.CreateDirectory(libsPath);

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
