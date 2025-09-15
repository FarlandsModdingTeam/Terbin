using Terbin.Data;

namespace Terbin.Commands.Instances;

// * Compatible con Pipe
// * Checks comprobados
// ! Necesita refactorización
internal class HandleDelete : IExecutable
{

    public override string Section => "INSTANCE DELETE";
    public override bool HasErrors()
    {
        if (Checkers.IsArgumentsEmpty(args)) return true;
        var name = args.FirstOrDefault(a => !a.StartsWith("-")) ?? string.Empty;

        if (Checkers.IsNullOrWhiteSpace(name)) return true;
        if (Checkers.IsConfigNull()) return true;

        if(Checkers.NotExistInstance(name)) return true;

        return false;
    }

    public override void Execution()
    {
        bool autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
        // First non-flag argument is treated as the instance name
        var name = args.FirstOrDefault(a => !a.StartsWith("-")) ?? string.Empty;
        var path = Ctx.config.GetInstancePath(name);
        
        if (!autoYes)
        {
            //TODO pensar como funcionará esto
            var ok = Ctx.Log.Confirm($"Remove instance '{name}' from config? Files will not be deleted. Path: '{path}'.", defaultNo: true);
            if (!ok)
            {
                Ctx.Log.Info("Cancelled.");
                return;
            }
        }

        Ctx.config.RemoveInstance(name);
        Ctx.Log.Success($"Instance '{name}' removed.");
        Ctx.PipeWrite(null, StatusCode.OK);
    }
}