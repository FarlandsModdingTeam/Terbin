namespace Terbin.Commands.Instances;

internal class HandleDelete
{
    public static void Delete(Ctx ctx, string[] args)
    {
                if (args.Length < 1)
        {
            ctx.Log.Warn("Usage: terbin instances delete <name> [-y]");
            return;
        }

        bool autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
        // First non-flag argument is treated as the instance name
        var name = args.FirstOrDefault(a => !a.StartsWith("-")) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            ctx.Log.Warn("Usage: terbin instances delete <name> [-y]");
            return;
        }

        if (ctx.config == null)
        {
            ctx.Log.Error("Config not loaded.");
            return;
        }

        if (!ctx.config.TryGetInstance(name, out var path))
        {
            ctx.Log.Error($"Instance not found: {name}");
            return;
        }

        if (!autoYes)
        {
            var ok = ctx.Log.Confirm($"Remove instance '{name}' from config? Files will not be deleted. Path: '{path}'.", defaultNo: true);
            if (!ok)
            {
                ctx.Log.Info("Cancelled.");
                return;
            }
        }

        ctx.config.RemoveInstance(name);
        ctx.Log.Success($"Instance '{name}' removed.");

    }
}