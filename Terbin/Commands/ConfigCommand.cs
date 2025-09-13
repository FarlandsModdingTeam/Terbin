namespace Terbin.Commands;

public class ConfigCommand : ICommand
{
    public string Name => "config";

    public string Description => "Configure local terbin options";

    public void Execution(string[] args)
    {
        if (args.Length < 1)
        {
            Ctx.Log.Warn("Not enough arguments. Usage: config <module> [value]");
            return;
        }

        string module = args[0];

        if (Ctx.config == null)
        {
            Ctx.Log.Error("No config loaded. Please initialize configuration first.");
            return;
        }

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