using System;
using System.Linq;
using Index = Terbin.Data.Index;
namespace Terbin.Commands;

public class ModsCommand : ICommand
{
    public string Name => "mods";

    public string Description => "Manage the mods index: list | update";

    public void Execution(Ctx ctx, string[] args)
    {
        if (ctx.config == null)
        {
            ctx.Log.Error("Config not loaded.");
            return;
        }

        var sub = args.Length > 0 ? args[0].Trim().ToLowerInvariant() : "list";
        try
        {
            switch (sub)
            {
                case "list":
                    // Use cached index if present, otherwise download it
                    if (ctx.config.index == null)
                    {
                        Index.Download(ctx);
                    }
                    else if (ctx.config.index?.references?.Count > 0)
                    {
                        ctx.Log.Info("Using cached mods index. Run 'terbin mods update' to refresh.");
                    }

                    var index = ctx.config.index;
                    if (index == null || index.references == null || index.references.Count == 0)
                    {
                        ctx.Log.Warn("No mods found in index.");
                        return;
                    }

                    var items = index.references
                        .OrderBy(r => r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                        .Select(r => $"- {r.Name ?? "(unknown)"} [{r.GUID ?? "?"}] -> {r.manifestUrl ?? "-"}");

                    ctx.Log.Box("Mods index", items);
                    break;

                case "update":
                    Index.Download(ctx);
                    var count = ctx.config.index?.references?.Count ?? 0;
                    ctx.Log.Success($"Mods index updated. Items: {count}.");
                    break;

                default:
                    ctx.Log.Warn("Usage: terbin mods list | terbin mods update");
                    break;
            }
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to process mods index: {ex.Message}");
        }
    }
}
