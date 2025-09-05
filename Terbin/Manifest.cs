using System.Collections.Generic;

public class Manifest
{
    public required string Name;
    public required string GUID;
    public required List<string> Versions;
    public required string url;
    public required List<string> Dependencies;
}