using System;
using System.Linq;
using Newtonsoft.Json;
using Terbin.Data;

namespace Terbin.Dialogs;

public class ManifestDialog
{
    public void run(string[] args)
    {
        var autoYes = args.Any(a => string.Equals(a, "-y", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--yes", StringComparison.OrdinalIgnoreCase));
        var empty = args.Any(a => string.Equals(a, "-x", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "--empty", StringComparison.OrdinalIgnoreCase));
        // Keep prompts lightweight; no section header

        string name;
        do
        {
            name = Ctx.Log.Ask("Project name: ").Trim();
            if (string.IsNullOrWhiteSpace(name))
                Ctx.Log.Warn("Name cannot be empty. Please try again.");
            else if (name.Contains(" "))
                Ctx.Log.Warn("Name cannot contain spaces. Please try again.");
        } while (string.IsNullOrWhiteSpace(name) || name.Contains(" "));

        string guid;
        do
        {
            guid = Ctx.Log.Ask("Unique identifier (GUID): ").Trim();
            if (string.IsNullOrWhiteSpace(guid))
                Ctx.Log.Warn("GUID cannot be empty. Please try again.");
            else if (guid.Contains(" "))
                Ctx.Log.Warn("GUID cannot contain spaces. Please try again.");
        } while (string.IsNullOrWhiteSpace(guid) || guid.Contains(" "));

        string version;
        do
        {
            version = Ctx.Log.Ask("Initial version: ").Trim();
            if (string.IsNullOrWhiteSpace(version))
                Ctx.Log.Warn("Version cannot be empty. Please try again.");
            else if (version.Contains(" "))
                Ctx.Log.Warn("Version cannot contain spaces. Please try again.");
        } while (string.IsNullOrWhiteSpace(version) || version.Contains(" "));

        string url;
        do
        {
            url = Ctx.Log.Ask("Project URL (e.g., https://github.com/user/repo): ").Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                Ctx.Log.Warn("URL cannot be empty. Please try again.");
                continue;
            }
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
            {
                Ctx.Log.Warn("Invalid URL. Use http or https.");
                url = string.Empty;
            }
        } while (string.IsNullOrWhiteSpace(url));

        Ctx.Log.Box("Project summary", new[]
        {
            $"Name    : {name}",
            $"GUID    : {guid}",
            $"Version : {version}",
            $"URL     : {url}"
        });

        if (autoYes || Ctx.Log.Confirm("Do you want to create the project?"))
        {
            var manifest = new ProjectManifest()
            {
                Name = name!,
                Type = empty ? ProjectManifest.ManifestType.EMPTY : ProjectManifest.ManifestType.NORMAL,
                GUID = guid!,
                Versions = new System.Collections.Generic.List<string> { version! },
                URL = url!,
                Dependencies = empty ? [] : ["fm.fcm"]
            };

            var path = Ctx.manifestPath ?? Path.Combine(Environment.CurrentDirectory, "manifest.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(manifest, Formatting.Indented));
            Ctx.Log.Success("Project created successfully.");
        }
        else
        {
            Ctx.Log.Info("Operation cancelled. No project was created.");
        }
    }
}
