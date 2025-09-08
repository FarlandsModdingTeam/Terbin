using System;
using System.Runtime.CompilerServices;
using Terbin.Data;
using Index = Terbin.Data.Index;

namespace Terbin;

public class Ctx
{
    public bool existManifest;
    public ProjectManifest? manifest;
    public string? manifestPath;

    public Config? config;
    public Index? index;
    

    public Logger Log { get; } = new Logger();
}
