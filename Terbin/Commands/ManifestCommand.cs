using System;
using System.Linq;
using Terbin.Dialogs;

namespace Terbin.Commands;

class ManifestCommand : AbstractCommand
{

    public override string Name => "manifest";

    public string Description => "Check if there is a manifest file";
    public override bool HasErrors()
    {
        if (Checkers.ExistFile(Ctx.manifestPath)) return true;

        return false;
    }
    public override void Execution()
    {
        // No heavy section needed here

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