using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Terbin.Commands;

public class CompletionCommand : ICommand
{
    public string Name => "completion";
    public string Description => "Outputs or installs PowerShell tab-completion for Terbin commands (Tab suggests commands).";

    public void Execution(Ctx ctx, string[] args)
    {
    ctx.Log.Info("Completion setup");

        // Discover all command names via reflection
        var commands = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(ICommand).IsAssignableFrom(t) && !t.IsAbstract && t.GetConstructor(Type.EmptyTypes) != null)
            .Select(t => (ICommand)Activator.CreateInstance(t)!)
            .Select(c => c.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n)
            .ToArray();

        var cmdList = string.Join(", ", commands.Select(c => $"'" + c + "'"));

        var psCore = new StringBuilder()
            .AppendLine("param($wordToComplete, $commandAst, $cursorPosition)")
            .AppendLine($"$cmds = @({cmdList})")
            .AppendLine("# Only complete the first argument after 'terbin'\n$elements = $commandAst.CommandElements\nif ($elements.Count -gt 2) { return }")
            .AppendLine("$cmds | Where-Object { $_ -like \"$wordToComplete*\" } | ForEach-Object {")
            .AppendLine("    [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)")
            .AppendLine("}");

        var psScript = new StringBuilder()
            .AppendLine("# Terbin PowerShell completion")
            .AppendLine("# Adds command name completion for 'terbin' (first argument)")
            .AppendLine("$scriptBlock = {\n" + psCore.ToString().Replace("\\", "\\\\").Replace("\"", "\"\"") + "\n}")
            .AppendLine("Register-ArgumentCompleter -Native -CommandName terbin -ScriptBlock $scriptBlock")
            .AppendLine("Register-ArgumentCompleter -Native -CommandName terbin.exe -ScriptBlock $scriptBlock")
            .ToString();

        var install = args.Any(a => string.Equals(a, "--install", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "-i", StringComparison.OrdinalIgnoreCase));

        if (!install)
        {
            ctx.Log.Info("Copy and run the following in PowerShell to enable completion for the current session:");
            ctx.Log.Box("Script", new[]{ psScript });
            ctx.Log.Info("Or run: terbin completion --install to add it to your PowerShell profile.");
            return;
        }

        // Attempt to install into the user's Windows PowerShell profile
        try
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // Windows PowerShell 5.1 profile path
            var profileDir = Path.Combine(docs, "WindowsPowerShell");
            Directory.CreateDirectory(profileDir);
            var profilePath = Path.Combine(profileDir, "Microsoft.PowerShell_profile.ps1");

            var banner = "# --- Terbin completion (auto-generated) ---";
            var content = (File.Exists(profilePath) ? File.ReadAllText(profilePath) + Environment.NewLine : string.Empty);

            if (!content.Contains(banner, StringComparison.Ordinal))
            {
                content += banner + Environment.NewLine + psScript + Environment.NewLine + "# --- End Terbin completion ---" + Environment.NewLine;
                File.WriteAllText(profilePath, content, Encoding.UTF8);
                ctx.Log.Success($"Completion installed to: {profilePath}");
                ctx.Log.Info("Restart PowerShell (or re-dot your profile) to activate.");
            }
            else
            {
                ctx.Log.Info("Completion already present in your profile.");
                ctx.Log.Info($"Profile: {profilePath}");
            }
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"Failed to install completion: {ex.Message}");
            ctx.Log.Info("You can still copy the script above and run it in your current session.");
        }
    }
}
