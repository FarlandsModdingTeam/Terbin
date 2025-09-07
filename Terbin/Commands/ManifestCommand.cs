using System;
using System.Linq;
using Terbin.Dialogs;

namespace Terbin.Commands;

class ManifestCommand : ICommand
{
    public string Name => "manifest";

    public string Description => "Check if there is a manifest file";

    public void Execution(Ctx ctx, string[] args)
    {
    // No heavy section needed here

        if (File.Exists(ctx.manifestPath))
        {
            ctx.Log.Info("A manifest file already exists in the project.");
        }
        else
        {
            ctx.Log.Warn("No manifest file exists.");
            var autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
            if (autoYes || ctx.Log.Confirm("Do you want to generate a new one?"))
            {
                new ManifestDialog().run(ctx, args);
            }
            else
            {
                ctx.Log.Info("Operation cancelled. No manifest was generated.");
            }
        }
    }
}