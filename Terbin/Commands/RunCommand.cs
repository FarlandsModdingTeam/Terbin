namespace Terbin.Commands;

class RunCommand : AbstractCommand
{

    public override string Name => "run";

    public string Description => "Execute a debug instance with the mod downloaded";
    public bool HasErrors()
    {
        if (Checkers.IsManifestNull()) return true;
        if (Checkers.IsConfigNull()) return true;

        return false;
    }
    public override void Execution()
    {
        string instanceName = $"debug_{Ctx.manifest.GUID}";
        string instancePath = Path.Combine(Environment.CurrentDirectory, ".Instance");
        string buildPath = Path.Combine(Environment.CurrentDirectory, "bin", "Debug", "net45");
        string targetPath = Path.Combine(instancePath, "BepInEx", "plugins", Ctx.manifest.Name);

        if (!Ctx.config.HasInstance(instanceName))
        {
            new InstancesCommand().ExecuteCommand(["create", instanceName, instancePath]);
        }

        new BuildCommand().ExecuteCommand([]);

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

        new InstancesCommand().ExecuteCommand(["run", instanceName]);
    }

}