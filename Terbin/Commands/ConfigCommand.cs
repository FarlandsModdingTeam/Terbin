namespace Terbin.Commands;

public class ConfigCommand : AbstractCommand
{
    public override string Name => "config";

    public string Description => "Configure local terbin options";
    public override bool HasErrors()
    {
        if (Checkers.IsArgumentsEmpty(args)) return true;
        if (Checkers.IsConfigNull()) return true;

        return false;
    }
    public override void Execution()
    {
        string module = args[0];

        if (module == "fpath")
        {
            if (args.Length < 2)
            {
                // Ask interactively for the path instead of warning
                var input = Ctx.Log.Ask("Enter Farlands path: ").Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    Ctx.Log.Warn("No path provided. Aborting.");
                    return;
                }
                Ctx.config.FarlandsPath = input;
            }
            else
            {
                Ctx.config.FarlandsPath = args[1];
            }
            Ctx.Log.Success($"Farlands path set to: {Ctx.config.FarlandsPath}");
        }
    }
}