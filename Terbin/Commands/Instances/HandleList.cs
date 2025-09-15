using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
internal class HandleList : IExecutable
{

    public override string Section => "INSTANCE LIST";

    public bool HasErrors()
    {
        if (Checkers.IsConfigNull()) return true;

        return false;
    }

    public override void Execution()
    {
        var items = Ctx.config!.GetInstances().OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"- {kv.Key}: {kv.Value}");

        Ctx.Log.Box("Instances", items);

        //? Debería tener ordenación? Cúal?
        Ctx.PipeWrite(Ctx.config!.GetInstances(), StatusCode.OK);
    }
}