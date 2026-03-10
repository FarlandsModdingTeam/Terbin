using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terbin.Data;

namespace Terbin.Commands.Mods
{
    /// <summary>
    /// Encargado de "mover" mods.
    /// </summary>
    public class DistributorMods
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
    }
}
