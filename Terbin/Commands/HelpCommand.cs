using System;
using System.Linq;
using System.Reflection;

namespace Terbin.Commands;

public class HelpCommand : AbstractCommand
{

    public override string Name => "help";
    public string Description => "Shows general help or details for a specific command (help <command>).";

    public override void Execution()
    {
        var all = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(AbstractCommand).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (AbstractCommand)Activator.CreateInstance(t)!)
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (args.Length == 0)
        {
            Ctx.Log.Section("Help");
            Ctx.Log.Info("Usage:");
            Ctx.Log.Info("  terbin <command> [args]");
            Ctx.Log.Info("  terbin help [command]");
            Ctx.Log.Info("");
            Ctx.Log.Info("Available commands:");
            foreach (var c in all)
            {
                Ctx.Log.Info($"  {c.Name} - {c.Description}");
            }
            Ctx.Log.Info("");
            Ctx.Log.Info("Short aliases: e.g. 'terbin -i' for 'terbin instances', 'terbin -h' for help.");
            return;
        }

        var name = args[0];
        var cmd = all.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (cmd == null)
        {
            Ctx.Log.Error($"Unknown command: {name}");
            Ctx.Log.Info("Run 'terbin help' to see available commands.");
            return;
        }

        Ctx.Log.Section($"Help: {cmd.Name}");
        Ctx.Log.Info(cmd.Description);
        Ctx.Log.Info("Usage:");
        Ctx.Log.Info($"  terbin {cmd.Name} [args]");
        Ctx.Log.Info("Notes: Some commands print usage hints when called without the required arguments.");
    }
}
