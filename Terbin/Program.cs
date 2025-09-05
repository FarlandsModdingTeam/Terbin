using Newtonsoft.Json;
using Terbin;


var ctx = new Ctx();
ctx.manifestPath = Path.Combine(Environment.CurrentDirectory, "manifest.json");
ctx.existManifest = File.Exists(ctx.manifestPath);
if (ctx.existManifest) ctx.manifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(ctx.manifestPath));

ctx.config = new();
ctx.config.configPath = Path.Combine(Environment.CurrentDirectory, ".terbin");
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

commands[args[0]](ctx, args.Skip(1).ToArray());