using System.IO.Pipes;
using System.Net.Security;
using System.Runtime.InteropServices.Marshalling;
using Newtonsoft.Json;
using Terbin;
using Terbin.Data;
using Index = Terbin.Data.Index;

// Carga contexto
var dotTerbin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");
var configPath = Config.configPath;

if (!Directory.Exists(dotTerbin))
{
    Directory.CreateDirectory(dotTerbin);
}

// Carga Manifest local
Ctx.existManifest = File.Exists(ProjectManifest.ManifestPath);
if (Ctx.existManifest)
{
    Ctx.manifest = JsonConvert.DeserializeObject<ProjectManifest>(File.ReadAllText(ProjectManifest.ManifestPath));
}

// Carga configuración
Ctx.config = new();
if (File.Exists(configPath))
{
    Ctx.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
}

//TODO: Carga índice 
Ctx.index = new();
Ctx.index.Setup();

var commands = new CommandList();
commands.init();

if (args[0].Trim() == "--it")
{
    string[] arguments = [""];
    while (arguments.Length < 0 || arguments[0].Trim() != "exit")
    {
        Console.Write("> ");
        arguments = Console.ReadLine().Split(" ");
        Iteration(arguments);
    }
}
else if (args[0].Trim() == "--pipe")
{
    bool exitFlag = false;

    using (var pipe = new NamedPipeServerStream("terbin", PipeDirection.InOut))
    {
        pipe.WaitForConnection();
        using (var reader = new StreamReader(pipe))
        using (var write = new StreamWriter(pipe) { AutoFlush = true })
        {
            try
            {
                while (!exitFlag)
                {
                    var arguments = reader.ReadLine().Split(" ");
                    Iteration(arguments);
                }
            }
            catch(IOException ex)
            {
                Ctx.Log.Error("Error de comunicación: " + ex.Message);
            }
        }

    }
}
else Iteration(args);

void Iteration(string[] args)
{
    if (args.Length < 1)
    {
        commands["help"](Array.Empty<string>());
        return;
    }

    var raw = args[0];
    string cmdToken = raw;
    if (raw.StartsWith("--"))
    {
        cmdToken = raw.Substring(2);
    }
    else if (raw.StartsWith("-"))
    {
        cmdToken = raw.Substring(1);
    }

    commands[cmdToken](args.Skip(1).ToArray());
}
