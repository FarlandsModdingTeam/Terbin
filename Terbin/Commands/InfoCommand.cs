using System;

namespace Terbin.Commands;

public class InfoCommand : ICommand
{
    public string Name => "info";

    public string Description => "This command is used in order to get terbin's info";

    public void Execution(Ctx ctx, string[] args)
    {
        ctx.Log.Section("Terbin information");
    ctx.Log.Info($"Version: undefined");
        ctx.Log.Info($"Manifest path: {ctx.manifestPath}");
        ctx.Log.Info($"Manifest exists?: {(ctx.existManifest ? "Yes" : "No")}");
        ctx.Log.Success("Command executed successfully.");
    }
}
