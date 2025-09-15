using System;

namespace Terbin.Commands;

public class IndexCommand : AbstractCommand
{
    public override string Name => "index";

    public string Description => "Manage index";
    public override bool HasErrors()
    {
        //TODO: Agregar checks
        return false;
    }
    public override void Execution()
    {
        var sub = args[0];
        args = args.Skip(1).ToArray();

        switch (sub)
        {
            case "update":
                Ctx.index.webIndex.DownloadIndex();
                break;
        }
    }
}
