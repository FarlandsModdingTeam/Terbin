namespace Terbin.Commands;

class RunCommand : ICommand
{
    public string Name => "run";

    public string Description => "Execute a debug instance with the mod downloaded";

    public void Execution(Ctx ctx, string[] args)
    {
        if (!ctx.existManifest || ctx.manifest == null)
        {
            ctx.Log.Error("No exist manifest or manifest is null");
            return;
        }

        if (ctx.config == null)
        {
            ctx.Log.Error("Config is null");
            return;
        }

        string instanceName = $"debug_{ctx.manifest.GUID}";
        string instancePath = Path.Combine(Environment.CurrentDirectory, ".Instance");
        string buildPath = Path.Combine(Environment.CurrentDirectory, "bin", "Debug", "net45");
        string targetPath = Path.Combine(instancePath, "BepInEx", "plugins", ctx.manifest.Name);

        if (!ctx.config.HasInstance(instanceName))
        {
            new InstancesCommand().Execution(ctx, new[] { "create", instanceName, instancePath });
        }

        new BuildCommand().Execution(ctx, []);

        // Ensure the target directory exists
        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        // Move all .dll files from buildPath to targetPath
        foreach (var dllFile in Directory.GetFiles(buildPath, "*.dll"))
        {
            string fileName = Path.GetFileName(dllFile);
            string destinationFile = Path.Combine(targetPath, fileName);

            if (File.Exists(destinationFile))
            {
                File.Delete(destinationFile);
            }

            File.Move(dllFile, destinationFile);
        }

        new InstancesCommand().Execution(ctx, ["run", instanceName]);
    }
}