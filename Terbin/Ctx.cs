using System;
using System.Runtime.CompilerServices;

namespace Terbin;

public class Ctx
{
    public bool existManifest;
    public Manifest? manifest;
    public string? manifestPath;

    public Config? config;
    

    public Logger Log { get; } = new Logger();
}
