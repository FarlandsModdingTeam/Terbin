using System;
using Terbin.Data;

namespace Terbin.Commands;

// * Compatible con Pipe
// * Checks comprobados
public class InfoCommand : ICommand
{
    public string Name => "info";

    public string Description => "This command is used in order to get terbin's info";

    public void Execution(string[] args)
    {
        Ctx.Log.Section("Info");
        Ctx.Log.Info($"Version: undefined");
        Ctx.Log.Info($"Manifest path: {Ctx.manifestPath}");
        Ctx.Log.Info($"Manifest exists?: {(Ctx.existManifest ? "Yes" : "No")}");
        Ctx.Log.Success("Command executed successfully.");

        var dto = new InfoDTO()
        {
            Version = "1.0.0",
            ExistManifest = Ctx.existManifest,
            ManifestPath = Ctx.manifestPath,
        };

        Ctx.PipeWrite(dto, StatusCode.OK, "ok");
    }

    public class InfoDTO
    {
        public string Version;
        public string ManifestPath;
        public bool ExistManifest;
    }
}


