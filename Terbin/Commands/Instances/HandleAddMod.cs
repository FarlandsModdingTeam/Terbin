using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terbin.Data;

namespace Terbin.Commands.Instances
{
    /// <summary>
    /// ______( Manejar Agregar Mod )______<br />
    /// - Gestiona la adición de mods.
    /// </summary>
    internal class HandleAddMod
    {

        /// <summary>
        /// Funcion para instalar un mod en una instancia.
        /// </summary>
        /// <param name="ctx">Contexto necesario para operar</param>
        /// <param name="mod">Renfia del mod en el Manifest</param>
        /// <param name="dest">destino del json</param>
        /// <returns></returns>
        public static bool InstallMod(Ctx ctx, Reference mod, string dest)
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="dest"></param>
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

        /// <summary>
        /// Maneja la accion de agregar mod.<br />
        /// Comprobando antes que la instancia exista y que el mod no este ya instalado.<br />
        /// </summary>
        /// <param name="ctx">Contexto para operar</param>
        /// <param name="args">[nombre de la Instancia, nombre del mod]</param>
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
        /// <summary>
        /// Maneja la accion de agregar mod.
        /// </summary>
        /// <param name="ctx">Contexto para operar</param>
        /// <param name="instance">instancia (nombre, ruta)</param>
        /// <param name="mod">nombre del mod</param>
        private static void HandleUnicAdd(Ctx ctx, KeyValuePair<string, string> instance, string mod)
        {
            try
            {
                ctx.Log.Info($"Preparing to add mod '{mod}' to instance '{instance.Key}' at '{instance.Value}'.");
                ctx.Log.Info("Loading mods index...");
                if (ctx.index == null) ctx.index.webIndex.DownloadIndex();
                ctx.Log.Success("Mods index loaded.");

                var reference = ctx.index[mod];
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
    }
}
