using System;
using System.IO;
using Newtonsoft.Json;
using Terbin;

namespace Terbin.Commands;

public class SetupAll : ICommand
{
    public string Name => "setup";
    public string Description => "Runs all main steps to prepare the mod";

    public void Execution(Ctx ctx, string[] args)
    {
        // Pass-through flags (e.g., -y/--yes) to sub-commands/dialogs
    ctx.Log.Section("Setup: full mod preparation");

        // 1. FarlandsPath configuration
        if (ctx.config == null || string.IsNullOrWhiteSpace(ctx.config.FarlandsPath))
        {
            ctx.Log.Info("Step: config fpath");
            new Config().Execution(ctx, new[] { "fpath" });
            // Reload config from disk to ensure the latest values are in context
            if (ctx.config != null && File.Exists(ctx.config.configPath))
            {
                var cfgJson = File.ReadAllText(ctx.config.configPath);
                var reloaded = JsonConvert.DeserializeObject<Terbin.Config>(cfgJson);
                if (reloaded != null) ctx.config = reloaded;
            }
        }
        else
        {
            ctx.Log.Info($"FarlandsPath already configured: {ctx.config.FarlandsPath}");
        }

        // 2. Manifest
    ctx.Log.Info("Step: manifest");
        new ManifestCommand().Execution(ctx, ["-y"]);
        // Reload manifest into context in case it was just created
        if (!string.IsNullOrWhiteSpace(ctx.manifestPath))
        {
            ctx.existManifest = File.Exists(ctx.manifestPath);
            if (ctx.existManifest)
            {
                var manJson = File.ReadAllText(ctx.manifestPath);
                ctx.manifest = JsonConvert.DeserializeObject<Manifest>(manJson);
            }
        }

        // 3. Gen
    ctx.Log.Info("Step: gen");
        new GenerateProject().Execution(ctx, []);

        // 4. Inf
    ctx.Log.Info("Step: inf");
        new InsertFarlands().Execution(ctx, Array.Empty<string>());

        // 5. Bman
    ctx.Log.Info("Step: bman");
        new BuildManifest().Execution(ctx, []);

        ctx.Log.Success("Setup complete. Mod ready!");
    }
}
