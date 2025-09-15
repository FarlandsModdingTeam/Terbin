using System.Diagnostics;

namespace Terbin.Commands;

//? Debería poder reconstruir el .csproj? 
class GenerateProject : AbstractCommand
{

    public override string Name => "gen";

    public string Description => "This command is used to generate a new project";
    public override bool HasErrors()
    {
        if (Checkers.IsManifestNull()) return true;

        var projectPath = Path.Combine(Environment.CurrentDirectory, $"{Ctx.manifest.Name}.csproj");
        if (Checkers.ExistFile(projectPath)) return true;

        return false;
    }
    public override void Execution()
    {
        // lightweight log; section header not needed
        Ctx.Log.Info("Generating project...");

        var projectPath = Path.Combine(Environment.CurrentDirectory, $"{Ctx.manifest.Name}.csproj");

        var modVersion = (Ctx.manifest.Versions != null && Ctx.manifest.Versions.Count > 0)
            ? Ctx.manifest.Versions[^1]
            : "0.0.0";

        var csproj = $"""
        <Project Sdk="Microsoft.NET.Sdk">
            <PropertyGroup>
                <TargetFramework>net45</TargetFramework>
                <AssemblyName>{Ctx.manifest.Name}</AssemblyName>
                <Product>Mod created using Terbin</Product>
                <Version>{modVersion}</Version>
                <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
                <LangVersion>latest</LangVersion>
                <RestoreAdditionalProjectSources>
                https://api.nuget.org/v3/index.json;
                https://nuget.bepinex.dev/v3/index.json;
                https://nuget.samboy.dev/v3/index.json
                </RestoreAdditionalProjectSources>
                <RootNamespace>{Ctx.manifest.Name}</RootNamespace>
            </PropertyGroup>

            <ItemGroup>
                <PackageReference Include="BepInEx.Analyzers" Version="1.*" PrivateAssets="all" />
                <PackageReference Include="BepInEx.Core" Version="5.*" />
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
            Arguments = $"restore \"{Ctx.manifest.Name}.csproj\"",
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
            if (!string.IsNullOrWhiteSpace(output)) Ctx.Log.Info(output.Trim());
            if (!string.IsNullOrWhiteSpace(error)) Ctx.Log.Warn(error.Trim());
        }
        Ctx.Log.Success($"Project generated: {projectPath}");
    }
}