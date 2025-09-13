using System;
using Terbin.Data;

namespace Terbin.Commands;

// * Compatible con Pipe
// * Checks comprobados
public class InfoCommand : ICommand
{
    public string Name => "info";

    public string Description => "This command is used in order to get terbin's info";

    public void Execution(Ctx ctx, string[] args)
    {
        ctx.Log.Section("Info");
        ctx.Log.Info($"Version: undefined");
        ctx.Log.Info($"Manifest path: {ctx.manifestPath}");
        ctx.Log.Info($"Manifest exists?: {(ctx.existManifest ? "Yes" : "No")}");
        ctx.Log.Success("Command executed successfully.");

        var dto = new InfoDTO()
        {
            Version = "1.0.0",
            ExistManifest = ctx.existManifest,
            ManifestPath = ctx.manifestPath,
        };

        ctx.PipeWrite(dto, StatusCode.OK, "ok");
    }

    public class InfoDTO
    {
        public string Version;
        public string ManifestPath;
        public bool ExistManifest;
    }
}


