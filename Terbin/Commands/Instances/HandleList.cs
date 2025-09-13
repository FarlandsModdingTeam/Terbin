using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
internal class HandleList
{
    public static void List(string[] args)
    {
        if (Checkers.IsConfigUnloaded()) return;

        var items = Ctx.config!.GetInstances().OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"- {kv.Key}: {kv.Value}");

        Ctx.Log.Box("Instances", items);

        //? Debería tener ordenación? Cúal?
        Ctx.PipeWrite(Ctx.config!.GetInstances(), StatusCode.OK);
    }
}