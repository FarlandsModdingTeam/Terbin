using Newtonsoft.Json;
using Terbin;

var ctx = new Ctx();
ctx.manifestPath = Path.Combine(Environment.CurrentDirectory, "manifest.json");
ctx.existManifest = File.Exists(ctx.manifestPath);
if (ctx.existManifest) ctx.manifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(ctx.manifestPath));

ctx.config = new();
if (File.Exists(ctx.config.configPath))
{
    ctx.config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ctx.config.configPath));
}


var commands = new CommandList();
commands.init();

if (args.Length < 1)
{
    // Default to help when no command provided
    commands["help"](ctx, Array.Empty<string>());
    return;
}

// Accept either plain command names or short aliases with '-' (e.g., -i)
var raw = args[0];
string cmdToken = raw;
if (raw.StartsWith("--"))
{
    // Allow but not required
    cmdToken = raw.Substring(2);
}
else if (raw.StartsWith("-"))
{
    cmdToken = raw.Substring(1);
}

commands[cmdToken](ctx, args.Skip(1).ToArray());