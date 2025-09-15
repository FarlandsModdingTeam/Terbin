using System;
using System.Diagnostics;

namespace Terbin.Commands;

public class BuildCommand : AbstractCommand
{

    public override string Name => "build";
    public string Description => "Generates plugin.cs from manifest and runs 'dotnet build'";
    public override bool HasErrors()
    {
        //TODO: Put checkers here
        return false;
    }
    public override void Execution()
    {
        Ctx.Log.Info("Build started...");

        // 1) Generate plugin.cs from manifest
        new BuildManifest().ExecuteCommand([]);

        // Ensure we have a manifest and corresponding project file
        if (Ctx.manifest == null)
        {
            Ctx.Log.Error("No manifest loaded. Create one with 'manifest' and 'gen' before building.");
            return;
        }
        var projectPath = Path.Combine(Environment.CurrentDirectory, $"{Ctx.manifest.Name}.csproj");
        if (!File.Exists(projectPath))
        {
            Ctx.Log.Error($"Project file not found: {projectPath}. Run 'gen' first.");
            return;
        }

        // 2) Run dotnet build
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{projectPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        string stdout = proc?.StandardOutput.ReadToEnd() ?? string.Empty;
        string stderr = proc?.StandardError.ReadToEnd() ?? string.Empty;
        proc?.WaitForExit();

        if (!string.IsNullOrWhiteSpace(stdout)) Ctx.Log.Info(stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr)) Ctx.Log.Warn(stderr.Trim());

        if (proc?.ExitCode == 0)
        {
            Ctx.Log.Success("Build completed successfully.");
        }
        else
        {
            Ctx.Log.Error($"Build failed with exit code {proc?.ExitCode}.");
        }
    }
}
