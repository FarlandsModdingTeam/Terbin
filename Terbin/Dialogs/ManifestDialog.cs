using System;
using System.Linq;
using Newtonsoft.Json;

namespace Terbin.Dialogs;

public class ManifestDialog
{
    public void run(Ctx ctx, string[] args)
    {
        var autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
        ctx.Log.Section("Project creation");

        string name;
        do
        {
            name = ctx.Log.Ask("Project name: ").Trim();
            if (string.IsNullOrWhiteSpace(name))
                ctx.Log.Warn("Name cannot be empty. Please try again.");
            else if (name.Contains(" "))
                ctx.Log.Warn("Name cannot contain spaces. Please try again.");
        } while (string.IsNullOrWhiteSpace(name) || name.Contains(" "));

        string guid;
        do
        {
            guid = ctx.Log.Ask("Unique identifier (GUID): ").Trim();
            if (string.IsNullOrWhiteSpace(guid))
                ctx.Log.Warn("GUID cannot be empty. Please try again.");
            else if (guid.Contains(" "))
                ctx.Log.Warn("GUID cannot contain spaces. Please try again.");
        } while (string.IsNullOrWhiteSpace(guid) || guid.Contains(" "));

    string version;
        do
        {
            version = ctx.Log.Ask("Initial version: ").Trim();
            if (string.IsNullOrWhiteSpace(version))
                ctx.Log.Warn("Version cannot be empty. Please try again.");
            else if (version.Contains(" "))
                ctx.Log.Warn("Version cannot contain spaces. Please try again.");
        } while (string.IsNullOrWhiteSpace(version) || version.Contains(" "));

        string url;
        do
        {
            url = ctx.Log.Ask("Project URL (e.g., https://github.com/user/repo): ").Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                ctx.Log.Warn("URL cannot be empty. Please try again.");
                continue;
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                ctx.Log.Warn("Invalid URL. Use http or https.");
                url = string.Empty;
            }
        } while (string.IsNullOrWhiteSpace(url));

        ctx.Log.Box("Project summary", new []
        {
            $"Name    : {name}",
            $"GUID    : {guid}",
            $"Version : {version}",
            $"URL     : {url}"
        });

    if (autoYes || ctx.Log.Confirm("Do you want to create the project?"))
        {
            var manifest = new Manifest()
            {
                Name = name!,
                GUID = guid!,
                Versions = new System.Collections.Generic.List<string> { version! },
                url = url!,
                Dependencies = ["fm.fcm"]
            };

            var path = ctx.manifestPath ?? Path.Combine(Environment.CurrentDirectory, "manifest.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(manifest, Formatting.Indented));
            ctx.Log.Success("Project created successfully.");
        }
    else
        {
            ctx.Log.Info("Operation cancelled. No project was created.");
        }
    }
}
