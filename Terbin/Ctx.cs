using System;
using System.Runtime.CompilerServices;
using Terbin.Data;

namespace Terbin;

public class Ctx
{
    public bool existManifest;
    public ProjectManifest? manifest;
    public string? manifestPath;

    public Config? config;
    

    public Logger Log { get; } = new Logger();
}
