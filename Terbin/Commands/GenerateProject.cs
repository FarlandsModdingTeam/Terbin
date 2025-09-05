using System.Diagnostics;

namespace Terbin.Commands;

class GenerateProject : ICommand
{
    public string Name => "gen";

    public string Description => "This command is used to generate a new project";

    public void Execution(Ctx ctx, string[] args)
    {
    ctx.Log.Section("Project generation");

    if (ctx.manifest == null)
    {
        ctx.Log.Error("No manifest loaded. Create one with 'manifest' before generating a project.");
        return;
    }

    var projectPath = Path.Combine(Environment.CurrentDirectory, $"{ctx.manifest.Name}.csproj");
    if (File.Exists(projectPath))
    {
    ctx.Log.Warn($"A project already exists: {ctx.manifest.Name}.csproj");
        return;
    }

        var modVersion = (ctx.manifest.Versions != null && ctx.manifest.Versions.Count > 0)
            ? ctx.manifest.Versions[^1]
            : "0.0.0";

        var csproj = $"""
        <Project Sdk="Microsoft.NET.Sdk">
            <PropertyGroup>
                <TargetFramework>net35</TargetFramework>
                <AssemblyName>{ctx.manifest.Name}</AssemblyName>
                <Product>Mod created using Terbin</Product>
                <Version>{modVersion}</Version>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <LangVersion>latest</LangVersion>
                <RestoreAdditionalProjectSources>
                https://api.nuget.org/v3/index.json;
                https://nuget.bepinex.dev/v3/index.json;
                https://nuget.samboy.dev/v3/index.json
                </RestoreAdditionalProjectSources>
                <RootNamespace>{ctx.manifest.Name}</RootNamespace>
            </PropertyGroup>

            <ItemGroup>
                <PackageReference Include="BepInEx.Analyzers" Version="1.*" PrivateAssets="all" />
                <PackageReference Include="BepInEx.Core" Version="5.*" />
                <PackageReference Include="UnityEngine.Modules" Version="5.6.0" IncludeAssets="compile" />
            </ItemGroup>
            
            <ItemGroup>
                <!-- Pull in all game/Unity DLLs from libs except netstandard itself to avoid conflicts -->
                <LibAssemblies Include="libs\**\*.dll" />
            </ItemGroup>

            <ItemGroup>
                <Reference Include="@(LibAssemblies)">
                    <Private>false</Private>
                </Reference>
            </ItemGroup>

            <ItemGroup Condition="'$(TargetFramework.TrimEnd(`0123456789`))' == 'net'">
                <PackageReference Include="Microsoft.NETFramework.ReferenceAssemblies" Version="1.0.2" PrivateAssets="all" />
            </ItemGroup>
        </Project>
        """;

    // Create the project file
        File.WriteAllText(projectPath, csproj);

    // Run 'dotnet restore'
        var restorePsi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"restore \"{ctx.manifest.Name}.csproj\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(restorePsi))
        {
            string output = process?.StandardOutput.ReadToEnd() ?? "";
            string error = process?.StandardError.ReadToEnd() ?? "";
            process?.WaitForExit();
            if (!string.IsNullOrWhiteSpace(output)) ctx.Log.Info(output.Trim());
            if (!string.IsNullOrWhiteSpace(error)) ctx.Log.Warn(error.Trim());
        }
    ctx.Log.Success($"Project generated: {projectPath}");
    }
}