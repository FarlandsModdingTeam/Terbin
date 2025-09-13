using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
internal class HandleList
{
    public static void List(Ctx ctx, string[] args)
    {
        if (Checkers.IsConfigUnloaded(ctx)) return;

        var items = ctx.config!.GetInstances().OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"- {kv.Key}: {kv.Value}");

        ctx.Log.Box("Instances", items);

        //? Debería tener ordenación? Cúal?
        ctx.PipeWrite(ctx.config!.GetInstances(), StatusCode.OK);
    }
}