using System;
using System.IO;
using Newtonsoft.Json;
using Terbin;
using Terbin.Data;

namespace Terbin.Commands;

public class SetupCommand : ICommand
{
    public string Name => "setup";
    public string Description => "Runs all main steps to prepare the mod";

    public void Execution(string[] args)
    {
        // Pass-through flags (e.g., -y/--yes) to sub-commands/dialogs
        Ctx.Log.Section("Setup: full mod preparation");

        // 1. FarlandsPath configuration
        if (Ctx.config == null || string.IsNullOrWhiteSpace(Ctx.config.FarlandsPath))
        {
            Ctx.Log.Info("Step: config fpath");
            new ConfigCommand().Execution(new[] { "fpath" });
            // Reload config from disk to ensure the latest values are in context
            if (Ctx.config != null && File.Exists(Config.configPath))
            {
                var cfgJson = File.ReadAllText(Config.configPath);
                var reloaded = JsonConvert.DeserializeObject<Terbin.Config>(cfgJson);
                if (reloaded != null) Ctx.config = reloaded;
            }
        }
        else
        {
            Ctx.Log.Info($"FarlandsPath already configured: {Ctx.config.FarlandsPath}");
        }

        // 2. Manifest
        Ctx.Log.Info("Step: manifest");
        List<string> manifestArgs = ["-y"];
        if (args.Contains("empty")) manifestArgs.Add("-x");

        new ManifestCommand().Execution(manifestArgs.ToArray());
        // Reload manifest into context in case it was just created
        if (!string.IsNullOrWhiteSpace(Ctx.manifestPath))
        {
            Ctx.existManifest = File.Exists(Ctx.manifestPath);
            if (Ctx.existManifest)
            {
                var manJson = File.ReadAllText(Ctx.manifestPath);
                Ctx.manifest = JsonConvert.DeserializeObject<ProjectManifest>(manJson);
            }
        }

        // 3. Gen
        Ctx.Log.Info("Step: gen");
        new GenerateProject().Execution([]);

        // 4. Inf
        Ctx.Log.Info("Step: inf");
        new InsertFarlandsCommand().Execution(Array.Empty<string>());

        // 5. Bman
        Ctx.Log.Info("Step: bman");

        new BuildManifest().Execution([]);

        Ctx.Log.Success("Setup complete. Mod ready!");
    }
}
