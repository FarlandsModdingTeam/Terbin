using System;
using System.Diagnostics;

namespace Terbin.Commands;

public class Build : ICommand
{
    public string Name => "build";
    public string Description => "Generates plugin.cs from manifest and runs 'dotnet build'";

    public void Execution(Ctx ctx, string[] args)
    {
    ctx.Log.Info("Build started...");

        // 1) Generate plugin.cs from manifest
        new BuildManifest().Execution(ctx, Array.Empty<string>());

        // Ensure we have a manifest and corresponding project file
        if (ctx.manifest == null)
        {
            ctx.Log.Error("No manifest loaded. Create one with 'manifest' and 'gen' before building.");
            return;
        }
        var projectPath = Path.Combine(Environment.CurrentDirectory, $"{ctx.manifest.Name}.csproj");
        if (!File.Exists(projectPath))
        {
            ctx.Log.Error($"Project file not found: {projectPath}. Run 'gen' first.");
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

        if (!string.IsNullOrWhiteSpace(stdout)) ctx.Log.Info(stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr)) ctx.Log.Warn(stderr.Trim());

        if (proc?.ExitCode == 0)
        {
            ctx.Log.Success("Build completed successfully.");
        }
        else
        {
            ctx.Log.Error($"Build failed with exit code {proc?.ExitCode}.");
        }
    }
}
