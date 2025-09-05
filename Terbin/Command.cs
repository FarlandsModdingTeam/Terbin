using System.Reflection;

namespace Terbin;

class CommandList
{
    private List<ICommand> commands = new();
    public void register(Type t)
    {
        if (typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
        {
            var obj = Activator.CreateInstance(t);
            if (obj is ICommand instance)
            {
                commands.Add(instance);
            }
        }
    }

    public Action<Ctx, string[]> getExecution(string cmd)
    {
        var found = commands.FirstOrDefault(c => c.Name.Equals(cmd, StringComparison.OrdinalIgnoreCase));
        if (found == null)
        {
            return (ctx, _) =>
            {
                ctx.Log.Error($"Unknown command: {cmd}");
                ctx.Log.Info("Available commands (use 'terbin help <command>' for details):");
                foreach (var c in commands.OrderBy(c => c.Name))
                {
                    ctx.Log.Info($"  {c.Name} - {c.Description}");
                }
            };
        }
        return found.Execution;
    }

    public Action<Ctx, string[]> this[string cmd] => getExecution(cmd);


    public void init()
    {
    commands = new List<ICommand>();
    register(typeof(Terbin.Commands.SetupAll));

        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(ICommand)))
            .ToList()
            .ForEach(register);
    }

    public IEnumerable<ICommand> All => commands;
}

interface ICommand
{
    string Name { get; }
    string Description { get; }
    void Execution(Ctx ctx, string[] args);
}