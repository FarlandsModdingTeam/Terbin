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
        // TODO: preparalo para NexxudMod.
        /// <summary>
        /// - Descarga un mod desde su URL de manifiesto y lo guarda en un archivo ZIP temporal.<br />
        /// - Nota: Si no se pasa destino, se crea un archivo temporal en el directorio temporal del sistema.<br />
        /// </summary>
        /// <param name="ctx">Contexto para poder operar</param>
        /// <param name="mod">Referencia al mod en json</param>
        /// <param name="dest">destino donde descargara el mod, Nota: mirar descripcion del motedo</param>
        /// <returns>Tubla<br/>success > si sea descargado con exito <br />place > lugar donde sea descargado</returns>
        public static (bool success, string place) DownloadMod(Reference mod, string? dest = null)
        {
            var res = true;
            dest ??= Path.Combine(Path.GetTempPath(), $"{mod.Name}_{Guid.NewGuid():N}.zip");
            try
            {
                Ctx.Log.Info($"Downloading {mod.Name}... :: {dest}");
                var manifesJson = NetUtil.DownloadString(mod.manifestUrl);
                var manifest = JsonConvert.DeserializeObject<ProjectManifest>(manifesJson);
                var url = Path.Combine(manifest.URL, $"releases/download/v{manifest.Versions.Last()}/{manifest.Name}.zip");
                NetUtil.DownloadFileWithProgress(url, dest);
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Ctx.Log.Error($"Failed to Download {mod.Name}: {ex.Message}");
                res = false;
            }
            finally
            {
                //try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { /* ignore */ }
            }

            return (res, dest);
        }

        private static byte handleDirMod(Reference mod, out string dir)
        {
            byte tipe = 0;
            dir = string.Empty;

            if (mod.manifestUrl == null)
                return tipe;

            if (!mod.manifestUrl.Contains("file:///"))
            {
                dir = Path.Combine(Path.GetTempPath(), $"{mod.Name}_{Guid.NewGuid():N}.zip");
                tipe = 1;
            }
            else
            {
                dir = mod.manifestUrl.Replace("file:///", string.Empty).Replace('/', Path.DirectorySeparatorChar);
                tipe = 2;
            }

            return tipe;
        }

        /// <summary>
        /// Funcion para "instalar" de FCM un mod.
        /// </summary>
        /// <param name="ctx">Contexto necesario para operar</param>
        /// <param name="mod">Referencia del mod en el Manifest</param>
        /// <param name="dest">destino donde extraer mod</param>
        /// <returns>true si se descargo correctamente</returns>
        public static bool InstallMod(Reference mod, string dest)
        {
            var res = true;
            string tmpZip = string.Empty;

            if (handleDirMod(mod, out tmpZip) == 1)
                if (!DownloadMod(mod, tmpZip).success) return false;

            try
            {
                Ctx.Log.Info($"Extracting {mod.Name}...");
                System.IO.Compression.ZipFile.ExtractToDirectory(tmpZip, dest, overwriteFiles: true);

                Ctx.Log.Success($"{mod.Name} installed.");
            }
            catch (Exception ex)
            {
                Ctx.Log.Error($"Failed to install {mod.Name}: {ex.Message}");
                res = false;
            }
            finally
            {
                try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { /* ignore */ }
            }

            return res;
        }

        public static void InstallBepInEx(string dest)
        {
            const string url = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.3/BepInEx_win_x64_5.4.23.3.zip";

            var bepinFolder = Path.Combine(dest, "BepInEx");
            var doorstopCfg = Path.Combine(dest, "doorstop_config.ini");
            var winhttp = Path.Combine(dest, "winhttp.dll");

            bool exists = Directory.Exists(bepinFolder) || File.Exists(doorstopCfg) || File.Exists(winhttp);
            if (exists)
            {
                var overwrite = Ctx.Log.Confirm("BepInEx appears to be installed. Reinstall/overwrite?", defaultNo: false);
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
                try { if (File.Exists(tmpZip)) File.Delete(tmpZip); } catch { /* ignore */ }
            }
        }


        /// <summary>
        /// Maneja la accion de agregar mod.
        /// </summary>
        /// <param name="ctx">Contexto para operar</param>
        /// <param name="instance">instancia (nombre, ruta)</param>
        /// <param name="mod">nombre del mod</param>
        //! SE MODIFICA INDEX
        private static void HandleUnicAdd(KeyValuePair<string, string> instance, string mod)
        {
            try
            {
                Ctx.Log.Info($"Preparing to add mod '{mod}' to instance '{instance.Key}' at '{instance.Value}'.");
                Ctx.Log.Info("Loading mods index...");
                if (Ctx.index == null) Ctx.index.webIndex.DownloadIndex();
                Ctx.Log.Success("Mods index loaded.");

                Reference? reference = Ctx.index[mod];
                string? modGuid = reference.GUID;
                if (string.IsNullOrWhiteSpace(modGuid))
                {
                    Ctx.Log.Error("Selected mod has no GUID or Name.");
                    return;
                }

                InstanceManifest manifest = GetManifest(instance.Value);

                manifest.Mods ??= new List<string>();
                if (manifest.Mods.Contains(modGuid, StringComparer.OrdinalIgnoreCase))
                {
                    Ctx.Log.Error($"Mod already installed: {modGuid}");
                    return;
                }

                HandleInstallMod((instance.Key, instance.Value), modGuid, reference);
            }
            catch (Exception ex)
            {
                Ctx.Log.Error($"Failed to add mod: {ex.Message}");
            }
        }

        /// <summary>
        /// Maneja la accion de agregar mod.<br />
        /// Comprobando antes que la instancia exista y que el mod no este ya instalado.<br />
        /// </summary>
        /// <param name="ctx">Contexto para operar</param>
        /// <param name="args">[nombre de la Instancia, nombre del mod]</param>
        public static void AddMod(string[] args)
        {
            if (args.Length < 2)
            {
                Ctx.Log.Warn("Usage: terbin instances add <instanceName> <mod>");
                return;
            }
            var key = args[0];
            var mod = args[1];
            if (Ctx.config == null)
            {
                Ctx.Log.Error("Config not loaded.");
                return;
            }
            if (!Ctx.config.TryGetInstance(key, out var instPath))
            {
                Ctx.Log.Error($"Instance not found: {key}");
                return;
            }
            HandleUnicAdd(new KeyValuePair<string, string>(key, instPath), mod);
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
        /// Compruebe si el mod ya está registrado en el manifiesto de instancia antes de instalarlo<br />
        /// </summary>
        /// <param name="eDirInstance"></param>
        /// <returns></returns>
        public static InstanceManifest GetManifest(string eDirInstance)
        {
            string? manifestPath = Path.Combine(eDirInstance, "manifest.json");
            InstanceManifest manifest;
            try
            {
                if (File.Exists(manifestPath))
                    manifest = JsonConvert.DeserializeObject<InstanceManifest>(File.ReadAllText(manifestPath)) ?? new InstanceManifest();
                else
                    manifest = new InstanceManifest();
            }
            catch
            {
                manifest = new InstanceManifest();
            }
            return manifest;
        }

        /// <summary>
        /// - Intenta instalar el mod en BepInEx de la instancia.<br />
        /// - Si la instalación es exitosa, agrega el GUID del mod al manifiesto de la instancia.
        /// </summary>
        /// <param name="ctx">Contexto necesario para gestionar</param>
        /// <param name="instance"></param>
        /// <param name="modGuid"></param>
        /// <param name="reference"></param>
        private static void HandleInstallMod((string Key, string Value) instance, string modGuid, Reference reference)
        {
            string dest = Path.Combine(instance.Value, "BepInEx");
            if (InstallMod(reference, dest))
            {
                AddModGuidToManifest(instance.Value, instance.Key, modGuid);
                Ctx.Log.Success($"Added mod GUID to manifest: {modGuid}");
            }
        }
    }
}
