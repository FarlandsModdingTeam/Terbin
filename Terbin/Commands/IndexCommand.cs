using System;

namespace Terbin.Commands;

public class IndexCommand : ICommand
{
    public string Name => "index";

    public string Description => "Manage index";

    public void Execution(Ctx ctx, string[] args)
    {
        //TODO: Agregar checks
        var sub = args[0];
        args = args.Skip(1).ToArray();

        switch (sub)
        {
            case "update":
                ctx.index.webIndex.DownloadIndex();
                break;
        }
    }
}
