using System;
using System.IO;
using Newtonsoft.Json;
using Terbin;
using Terbin.Data;

namespace Terbin.Commands;

public class SetupCommand : AbstractCommand
{
    public override string Name => "setup";
    public string Description => "Runs all main steps to prepare the mod";
    public override bool HasErrors()
    {
        return false;
    }
    public override void Execution()
    {
        // 1. FarlandsPath configuration
        if (Checkers.IsConfigNull() || Checkers.IsNullOrWhiteSpace(Ctx.config.FarlandsPath))
        {
            Ctx.Log.Info("Step: config fpath");
            new ConfigCommand().ExecuteCommand(["fpath"]);
        }
        else
        {
            Ctx.Log.Info($"FarlandsPath already configured: {Ctx.config.FarlandsPath}");
        }

        // 2. Manifest
        Ctx.Log.Info("Step: manifest");
        List<string> manifestArgs = ["-y"];
        if (args.Contains("empty")) manifestArgs.Add("-x");

        new ManifestCommand().ExecuteCommand(manifestArgs.ToArray());
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
        new GenerateProject().ExecuteCommand([]);

        // 4. Inf
        Ctx.Log.Info("Step: inf");
        new InsertFarlandsCommand().ExecuteCommand([]);

        // 5. Bman
        Ctx.Log.Info("Step: bman");

        new BuildManifest().ExecuteCommand([]);

        Ctx.Log.Success("Setup complete. Mod ready!");
    }
}
