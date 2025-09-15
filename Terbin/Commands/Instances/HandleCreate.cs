using Newtonsoft.Json;
using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
// ! Necesita refactorización
internal class HandleCreate : IExecutable
{
    public override string Section => "INSTANCES CREATE";

    public override bool HasErrors()
    {
        if (Checkers.HasNotEnoughArgs(args, 2))
            return true;
        var name = args[0];
        var installPath = args[1];

        if (Checkers.IsNullOrWhiteSpace(name, "Name must be provided"))
            return true;
        if (Checkers.IsNullOrWhiteSpace(installPath, "Path must be provided"))
            return true;
        var fpath = Ctx.config!.FarlandsPath;

        if (Checkers.IsNullOrWhiteSpace(fpath, "Farlands path is not configured"))
            return true;
        if (Checkers.NotExistDirectory(fpath, "Farlands Path not exist"))
            return true;

        return false;
    }

    public override void Execution()
    {
        var name = args[0];
        var installPath = args[1];

        // Require FarlandsPath to be set and exist
        var fpath = Ctx.config!.FarlandsPath;

        // Normalize paths
        fpath = Path.GetFullPath(fpath);
        var dest = Path.GetFullPath(installPath);

        if (string.Equals(fpath, dest, StringComparison.OrdinalIgnoreCase))
        {
            Ctx.Log.Error("Destination path cannot be the same as the Farlands source path.");
            Ctx.PipeWrite(
                null,
                StatusCode.BAD_REQUEST,
                "Destination path cannot be the same as the Farlands source path."
            );
            return;
        }

        // Prevent nesting (dest inside src) which would cause infinite recursion
        if (
            dest.StartsWith(fpath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        )
        {
            Ctx.Log.Error("Destination path cannot be inside the Farlands source path.");
            Ctx.PipeWrite(
                null,
                StatusCode.BAD_REQUEST,
                "Destination path cannot be inside the Farlands source path."
            );

            return;
        }

        // Disallow creating if there is already an instance at the requested path (registered or detected by manifest)
        if (Ctx.config.ExistInstanceInPath(dest))
        {
            Ctx.Log.Error($"There is already an instance registered at '{dest}'.");
            Ctx.PipeWrite(
                null,
                StatusCode.BAD_REQUEST,
                $"There is already an instance registered at '{dest}'."
            );
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
                Ctx.Log.Error(
                    $"Destination '{dest}' already contains an instance (manifest.json found)."
                );
                Ctx.PipeWrite(
                    null,
                    StatusCode.BAD_REQUEST,
                    $"Destination '{dest}' already contains an instance (manifest.json found)."
                );
                return;
            }

            // If not empty, confirm merge/overwrite
            var hasAny = Directory.EnumerateFileSystemEntries(dest).Any();
            if (hasAny)
            {
                //TODO: Que hacer con esto?
                var ok = Ctx.Log.Confirm(
                    $"Destination '{dest}' is not empty. Merge and overwrite files?",
                    defaultNo: false
                );
                if (!ok)
                {
                    Ctx.Log.Warn("Aborted by user.");
                    return;
                }
            }
        }

        try
        {
            Ctx.Log.Info($"Cloning from '{fpath}' to '{dest}'...");
            // Progress bar during clone
            FileOps.CopyDirectoryWithProgress(
                fpath,
                dest,
                overwrite: true,
                (current, total) =>
                {
                    ProgressUtil.DrawProgressBar(current, total);
                }
            );
            // Ensure we end the progress line
            Console.WriteLine();
            Ctx.Log.Success("Clone completed.");

            // Install BepInEx
            InstallBepInEx(dest);
        }
        catch (Exception ex)
        {
            Ctx.Log.Error($"Failed to clone files: {ex.Message}");
            Ctx.PipeWrite(
                null,
                StatusCode.INTERNAL_SERVER_ERROR,
                $"Failed to clone files: {ex.Message}"
            );
            return;
        }

        if (Ctx.config!.HasInstance(name))
        {
            Ctx.Log.Warn($"Instance '{name}' already exists. Updating path.");
        }

        Ctx.config!.AddInstance(name, dest);

        // Generate instance manifest base
        try
        {
            GenerateInstanceManifest(dest, name);
            Ctx.Log.Success("Instance manifest created.");
        }
        catch (Exception mex)
        {
            Ctx.Log.Warn($"Failed to write instance manifest: {mex.Message}");
        }

        Ctx.Log.Success($"Instance '{name}' created at '{dest}'.");
        Ctx.PipeWrite(null, StatusCode.OK, $"ok");
    }

    public void InstallBepInEx(string dest)
    {
        const string url =
            "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip";

        var bepinFolder = Path.Combine(dest, "BepInEx");
        var doorstopCfg = Path.Combine(dest, "doorstop_config.ini");
        var winhttp = Path.Combine(dest, "winhttp.dll");

        bool exists =
            Directory.Exists(bepinFolder) || File.Exists(doorstopCfg) || File.Exists(winhttp);
        if (exists)
        {
            var overwrite = Ctx.Log.Confirm(
                "BepInEx appears to be installed. Reinstall/overwrite?",
                defaultNo: false
            );
            if (!overwrite)
            {
                Ctx.Log.Info("Skipping BepInEx installation.");
                return;
            }
        }

        string tmpZip = Path.Combine(Path.GetTempPath(), $"bepinex_{Guid.NewGuid():N}.zip");
        try
        {
            Ctx.Log.Info("Downloading BepInEx...");
            NetUtil.DownloadFileWithProgress(url, tmpZip);
            Console.WriteLine();

            Ctx.Log.Info("Extracting BepInEx...");
            System.IO.Compression.ZipFile.ExtractToDirectory(tmpZip, dest, overwriteFiles: true);
            Ctx.Log.Success("BepInEx installed.");
        }
        catch (Exception ex)
        {
            Ctx.Log.Error($"Failed to install BepInEx: {ex.Message}");
        }
        finally
        {
            try
            {
                if (File.Exists(tmpZip))
                    File.Delete(tmpZip);
            }
            catch
            { /* ignore */
            }
        }
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
            Mods = new List<string>(),
        };
        var json = JsonConvert.SerializeObject(instanceManifest, Formatting.Indented);
        File.WriteAllText(manifestPath, json);
    }
}
