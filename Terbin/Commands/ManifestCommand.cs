using System;
using System.Linq;
using Terbin.Dialogs;

namespace Terbin.Commands;

class ManifestCommand : ICommand
{
    public string Name => "manifest";

    public string Description => "Check if there is a manifest file";

    public void Execution(string[] args)
    {
    // No heavy section needed here

        if (File.Exists(Ctx.manifestPath))
        {
            Ctx.Log.Info("A manifest file already exists in the project.");
        }
        else
        {
            Ctx.Log.Warn("No manifest file exists.");
            var autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
            if (autoYes || Ctx.Log.Confirm("Do you want to generate a new one?"))
            {
                new ManifestDialog().run(args);
            }
            else
            {
                Ctx.Log.Info("Operation cancelled. No manifest was generated.");
            }
        }
    }
}