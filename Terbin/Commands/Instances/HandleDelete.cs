using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
// ! Necesita refactorización
internal class HandleDelete
{
    public static void Delete(Ctx ctx, string[] args)
    {

        if (Checkers.IsArgumentsEmpty(null, args)) return;

        bool autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
        // First non-flag argument is treated as the instance name
        var name = args.FirstOrDefault(a => !a.StartsWith("-")) ?? string.Empty;

        if (Checkers.IsNullOrWhiteSpace(ctx, name)) return;


        if (Checkers.IsConfigUnloaded(ctx)) return;

        if (!ctx.config.TryGetInstance(name, out var path))
        {
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, $"Instance not found: {name}");

            return;
        }

        if (!autoYes)
        {
            //TODO pensar como funcionará esto
            var ok = ctx.Log.Confirm($"Remove instance '{name}' from config? Files will not be deleted. Path: '{path}'.", defaultNo: true);
            if (!ok)
            {
                ctx.Log.Info("Cancelled.");
                return;
            }
        }

        ctx.config.RemoveInstance(name);
        ctx.Log.Success($"Instance '{name}' removed.");
        ctx.PipeWrite(null, StatusCode.OK);
    }
}