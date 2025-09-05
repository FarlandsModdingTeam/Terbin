namespace Terbin.Commands;

public class Config : ICommand
{
    public string Name => "config";

    public string Description => "Configure local terbin options";

    public void Execution(Ctx ctx, string[] args)
    {
        if (args.Length < 1)
        {
            ctx.Log.Warn("[Config] Not enough arguments. Usage: config <module> [value]");
            return;
        }

        string module = args[0];

        if (ctx.config == null)
        {
            ctx.Log.Error("[Config] No config loaded. Please initialize configuration first.");
            return;
        }

        if (module == "fpath")
        {
            if (args.Length < 2)
            {
                // Ask interactively for the path instead of warning
                var input = ctx.Log.Ask("Enter Farlands path: ").Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    ctx.Log.Warn("[Config] No path provided. Aborting.");
                    return;
                }
                ctx.config.FarlandsPath = input;
            }
            else
            {
                ctx.config.FarlandsPath = args[1];
            }
            ctx.Log.Success($"[Config] Farlands path set to: {ctx.config.FarlandsPath}");
        }
    }
}