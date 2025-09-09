using System.Reflection;

namespace Terbin;

class CommandList
{
    private List<ICommand> commands = new();
    private readonly Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase);
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
        if (aliases.TryGetValue(cmd, out var canonical))
        {
            cmd = canonical;
        }
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

        // Short aliases (-x) map to canonical command names
        AddAlias("i", "instances");
        AddAlias("h", "help");
        AddAlias("v", "version");
    }

    public IEnumerable<ICommand> All => commands;

    public void AddAlias(string alias, string target)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(target)) return;
        aliases[alias] = target;
    }
}

/// <summary>
/// Intefaz que representa un comando.
/// </summary>
interface ICommand
{
    /// <summary>
    /// Nombre del comando a ejecutar.
    /// </summary>
    string Name { get; }
    /// <summary>
    /// Descripción del comando.
    /// </summary>
    string Description { get; }
    /// <summary>
    /// Función que se ejecuta al invocar el comando.
    /// </summary>
    /// <param name="ctx">Contexto necesario para operar</param>
    /// <param name="args">Valores pasado por comando</param>
    void Execution(Ctx ctx, string[] args);
}