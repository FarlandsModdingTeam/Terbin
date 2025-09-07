using System.Collections.Generic;

namespace Terbin.Data;

public class ProjectManifest
{
    public enum ManifestType
    {
        NORMAL,
        EMPTY
    }
    public required string Name;
    public required ManifestType Type;
    public required string GUID;
    public required List<string> Versions;
    public required string url;
    public required List<string> Dependencies;
}