using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Terbin.Commands;

public class Version : ICommand
{
    public string Name => "version";
    public string Description => "Shows, lists, or sets the mod version (uses the latest entry in Versions).";

    public void Execution(Ctx ctx, string[] args)
    {
    ctx.Log.Info("Version");

        if (ctx.manifest == null || string.IsNullOrWhiteSpace(ctx.manifestPath) || !File.Exists(ctx.manifestPath))
        {
            ctx.Log.Error("No manifest loaded. Create one with 'manifest' first.");
            return;
        }

        var versions = ctx.manifest.Versions ?? new System.Collections.Generic.List<string>();
        string Current() => versions.Count > 0 ? versions[^1] : "<none>";

        if (args.Length == 0)
        {
            ctx.Log.Warn("Usage: version show | version list | version upgrade [major|minor|patch|<newVersion>] | version downgrade");
            ctx.Log.Info($"Current: {Current()}");
            return;
        }

        var sub = args[0].Trim();
        if (string.Equals(sub, "show", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Log.Info($"Current: {Current()}");
            return;
        }
    if (string.Equals(sub, "list", StringComparison.OrdinalIgnoreCase))
        {
            if (versions.Count == 0)
            {
                ctx.Log.Info("No versions found.");
                return;
            }
            ctx.Log.Box("Versions", versions.Select((v, i) => $"{i + 1}. {v}"));
            return;
        }

        // Handle downgrade: it never accepts a target version; it only removes the last entry
        if (string.Equals(sub, "downgrade", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length > 1)
            {
                ctx.Log.Error("Downgrade does not take arguments. It removes the last version entry.");
                ctx.Log.Warn("Usage: version downgrade");
                return;
            }
            if (versions.Count == 0)
            {
                ctx.Log.Info("No versions to remove.");
                return;
            }
            var removed = versions[^1];
            versions.RemoveAt(versions.Count - 1);
            ctx.manifest.Versions = versions;
            var jsonPop = JsonConvert.SerializeObject(ctx.manifest, Formatting.Indented);
            File.WriteAllText(ctx.manifestPath!, jsonPop);
            ctx.Log.Success($"Removed version: {removed}");
            if (versions.Count > 0) ctx.Log.Info($"Current: {versions[^1]}");
            else ctx.Log.Info("No versions left.");
            return;
        }

        // Handle upgrade operations (explicit target or bump)
        if (string.Equals(sub, "upgrade", StringComparison.OrdinalIgnoreCase))
        {
            // If an explicit version is provided and not a bump keyword, set to it
            if (args.Length >= 2 && !(args[1].Equals("major", StringComparison.OrdinalIgnoreCase) || args[1].Equals("minor", StringComparison.OrdinalIgnoreCase) || args[1].Equals("patch", StringComparison.OrdinalIgnoreCase)))
            {
                var target = args[1].Trim();
                if (string.IsNullOrWhiteSpace(target) || target.Contains(' '))
                {
                    ctx.Log.Error("Invalid version. It cannot be empty or contain spaces.");
                    return;
                }
                if (!target.All(ch => char.IsLetterOrDigit(ch) || ch == '.' || ch == '-' || ch == '_'))
                {
                    ctx.Log.Warn("Version contains unusual characters. Allowed: letters, digits, '.', '-', '_'.");
                }
                if (versions.Count > 0 && versions[^1] == target)
                {
                    ctx.Log.Info($"Version already current: {target}");
                    return;
                }
                ctx.manifest.Versions = versions;
                versions.Add(target);
                var jsonE = JsonConvert.SerializeObject(ctx.manifest, Formatting.Indented);
                File.WriteAllText(ctx.manifestPath!, jsonE);
                ctx.Log.Success($"Version set to: {target}");
                return;
            }

            // Otherwise use bump logic with level (default patch)
            string level = (args.Length >= 2 ? args[1].Trim().ToLowerInvariant() : "patch");
            if (level != "major" && level != "minor" && level != "patch")
            {
                ctx.Log.Error("Invalid level. Use: major | minor | patch or provide an explicit version.");
                return;
            }

            string basis = versions.Count > 0 ? versions[^1] : "0.0.0";
            (int M, int m, int p) = ParseVersion(basis);

            switch (level)
            {
                case "major": M += 1; m = 0; p = 0; break;
                case "minor": m += 1; p = 0; break;
                default: p += 1; break;
            }

            var bumped = $"{M}.{m}.{p}";
            if (versions.Count > 0 && versions[^1] == bumped)
            {
                ctx.Log.Info($"Version already current: {bumped}");
                return;
            }
            ctx.manifest.Versions = versions;
            versions.Add(bumped);
            var jsonB = JsonConvert.SerializeObject(ctx.manifest, Formatting.Indented);
            File.WriteAllText(ctx.manifestPath!, jsonB);
            ctx.Log.Success($"Version set to: {bumped}");
            return;
        }
        // If reaches here, unrecognized args
    ctx.Log.Warn("Usage: version show | version list | version upgrade [major|minor|patch|<newVersion>] | version downgrade");
        ctx.Log.Info($"Current: {Current()}");
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string s)
    {
        // Extract numeric major.minor.patch from a version string, ignoring prerelease/build
        // Non-numeric parts are treated as 0. Missing parts default to 0.
        int M = 0, m = 0, p = 0;
        try
        {
            var main = s.Split('-', '+')[0];
            var parts = main.Split('.');
            if (parts.Length > 0) int.TryParse(TrimNonDigits(parts[0]), out M);
            if (parts.Length > 1) int.TryParse(TrimNonDigits(parts[1]), out m);
            if (parts.Length > 2) int.TryParse(TrimNonDigits(parts[2]), out p);
        }
        catch { /* fallback to zeros */ }
        return (M, m, p);
    }

    private static string TrimNonDigits(string input)
    {
        var chars = input.TakeWhile(char.IsDigit).ToArray();
        return new string(chars);
    }
}
