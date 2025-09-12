using System;
using System.Linq;
using System.Reflection;

namespace Terbin.Commands;

public class HelpCommand : ICommand
{
    public string Name => "help";
    public string Description => "Shows general help or details for a specific command (help <command>).";

    public void Execution(Ctx ctx, string[] args)
    {
        var all = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (ICommand)Activator.CreateInstance(t)!)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (args.Length == 0)
        {
            ctx.Log.Section("Help");
            ctx.Log.Info("Usage:");
            ctx.Log.Info("  terbin <command> [args]");
            ctx.Log.Info("  terbin help [command]");
            ctx.Log.Info("");
            ctx.Log.Info("Available commands:");
            foreach (var c in all)
            {
                ctx.Log.Info($"  {c.Name} - {c.Description}");
            }
            ctx.Log.Info("");
            ctx.Log.Info("Short aliases: e.g. 'terbin -i' for 'terbin instances', 'terbin -h' for help.");
            return;
        }

        var name = args[0];
        var cmd = all.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (cmd == null)
        {
            ctx.Log.Error($"Unknown command: {name}");
            ctx.Log.Info("Run 'terbin help' to see available commands.");
            return;
        }

    ctx.Log.Section($"Help: {cmd.Name}");
        ctx.Log.Info(cmd.Description);
        ctx.Log.Info("Usage:");
    ctx.Log.Info($"  terbin {cmd.Name} [args]");
        ctx.Log.Info("Notes: Some commands print usage hints when called without the required arguments.");
    }
}
