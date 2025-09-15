using System.Reflection;
using System.Windows.Input;

namespace Terbin;

class CommandList
{
    private List<AbstractCommand> commands = new();
    private readonly Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase);
    public void register(Type t)
    {
        if (typeof(AbstractCommand).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
        {
            var obj = Activator.CreateInstance(t);
            if (obj is AbstractCommand instance)
            {
                commands.Add(instance);
            }
        }
    }

    public AbstractCommand getExecution(string cmd)
    {
        if (aliases.TryGetValue(cmd, out var canonical))
        {
            cmd = canonical;
        }
        var found = commands.FirstOrDefault(c => c.Name.Equals(cmd, StringComparison.OrdinalIgnoreCase));
        if (found == null)
        {
            return null;
        }
        return found;
    }

    public AbstractCommand this[string cmd] => getExecution(cmd);


    public void init()
    {
        commands = new List<AbstractCommand>();
        // register(typeof(Terbin.Commands.SetupCommand));

        Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(AbstractCommand)))
            .ToList()
            .ForEach(register);

        // Short aliases (-x) map to canonical command names
        AddAlias("i", "instances");
        AddAlias("h", "help");
        AddAlias("v", "version");
    }

    public IEnumerable<AbstractCommand> All => commands;

    public void AddAlias(string alias, string target)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(target)) return;
        aliases[alias] = target;
    }
}

/// <summary>
/// Intefaz que representa un comando.
/// </summary>
public abstract class AbstractCommand : IExecutable
{

    /// <summary>
    /// Nombre del comando a ejecutar.
    /// </summary>
    public abstract string Name { get; }
    public override string Section => Name.ToUpper();
    /// <summary>
    /// Descripción del comando.
    /// </summary>
    public string Description { get; }
}

public abstract class IExecutable
{
    public abstract string Section { get; }
    public string[] args;
    public IExecutable()
    {
        args = [];
    }

    public virtual bool HasErrors()
    {
        return false;
    }

    /// <summary>
    /// Función que se ejecuta al invocar el comando.
    /// </summary>
    /// <param name="ctx">Contexto necesario para operar</param>
    /// <param name="args">Valores pasado por comando</param>
    public abstract void Execution();

    public void ExecuteCommand()
    {
        Ctx.Log.Section(Section);

        if (HasErrors()) return;

        Execution();
    }

    public void ExecuteCommand(string[] args)
    {
        this.args = args;

        Ctx.Log.Section(Section);

        if (HasErrors()) return;

        Execution();
    }
}