using System.Runtime.InteropServices.Marshalling;
using Newtonsoft.Json;
using Terbin;
using Terbin.Data;
using Index = Terbin.Data.Index;

// Carga contexto
var ctx = new Ctx();

var manifestPath = Path.Combine(Environment.CurrentDirectory, "manifest.json");
var dotTerbin = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");
var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin/config.json");

if (!Directory.Exists(dotTerbin))
{
    Directory.CreateDirectory(dotTerbin);
}

// Carga Manifest local
ctx.existManifest = File.Exists(manifestPath);
if (ctx.existManifest)
{
    ctx.manifest = JsonConvert.DeserializeObject<ProjectManifest>(File.ReadAllText(manifestPath));
}

// Carga configuración
ctx.config = new();
if (File.Exists(configPath))
{
    ctx.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
}

//TODO: Carga índice 
ctx.index = new();
ctx.index.Setup();


var commands = new CommandList();
commands.init();

if (args.Length < 1)
{

    commands["help"](ctx, Array.Empty<string>());
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

commands[cmdToken](ctx, args.Skip(1).ToArray());