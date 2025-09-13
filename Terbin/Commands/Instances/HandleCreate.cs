
using Newtonsoft.Json;
using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
// ! Necesita refactorización
internal class HandleCreate
{
    public static void Create(Ctx ctx, string[] args)
    {
        if (Checkers.HasNotEnoughArgs(ctx, args, 2)) return;

        var name = args[0];
        var installPath = args[1];

        if (Checkers.IsNullOrWhiteSpace(ctx, name, "Name must be provided")) return;
        if (Checkers.IsNullOrWhiteSpace(ctx, installPath, "Path must be provided")) return;


        // Require FarlandsPath to be set and exist
        var fpath = ctx.config!.FarlandsPath;

        if (Checkers.IsNullOrWhiteSpace(ctx, fpath, "Farlands path is not configured")) return;
        if (Checkers.NotExistingDirectory(ctx, fpath, "Path not exist")) return;

        // Normalize paths
        fpath = Path.GetFullPath(fpath);
        var dest = Path.GetFullPath(installPath);

        if (string.Equals(fpath, dest, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Log.Error("Destination path cannot be the same as the Farlands source path.");
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, "Destination path cannot be the same as the Farlands source path.");
            return;
        }

        // Prevent nesting (dest inside src) which would cause infinite recursion
        if (dest.StartsWith(fpath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            ctx.Log.Error("Destination path cannot be inside the Farlands source path.");
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, "Destination path cannot be inside the Farlands source path.");

            return;
        }

        // Disallow creating if there is already an instance at the requested path (registered or detected by manifest)
        if (ctx.config.ExistInstanceInPath(dest))
        {
            ctx.Log.Error($"There is already an instance registered at '{dest}'.");
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, $"There is already an instance registered at '{dest}'.");
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
                ctx.PipeWrite(null, StatusCode.BAD_REQUEST, $"Destination '{dest}' already contains an instance (manifest.json found).");
                return;
            }

            // If not empty, confirm merge/overwrite
            var hasAny = Directory.EnumerateFileSystemEntries(dest).Any();
            if (hasAny)
            {
                //TODO: Que hacer con esto?
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
            ctx.Log.Info($"Cloning from '{fpath}' to '{dest}'...");
            // Progress bar during clone
            FileOps.CopyDirectoryWithProgress(fpath, dest, overwrite: true, (current, total) =>
            {
                ProgressUtil.DrawProgressBar(current, total);
            });
            // Ensure we end the progress line
            Console.WriteLine();
            ctx.Log.Success("Clone completed.");

            // Install BepInEx
            HandleAddMod.InstallBepInEx(ctx, dest);
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to clone files: {ex.Message}");
            ctx.PipeWrite(null, StatusCode.INTERNAL_SERVER_ERROR, $"Failed to clone files: {ex.Message}");
            return;
        }

        if (ctx.config!.HasInstance(name))
        {
            ctx.Log.Warn($"Instance '{name}' already exists. Updating path.");
        }

        ctx.config!.AddInstance(name, dest);

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
        ctx.PipeWrite(null, StatusCode.OK, $"ok");
    }

    /// <summary>
    /// Genera el manifest.json de la instancia.<br />
    /// sobre escribe todo aquel que exista y pone valores predeterminados:<br />
    /// Version = "1.0.0"<br />
    /// Mods = []<br />
    /// El nombre es el que se le pasa por parámetro.
    /// </summary>
    /// <param name="dest">destino donde generar</param>
    /// <param name="name">nombre del MOD</param>
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
}