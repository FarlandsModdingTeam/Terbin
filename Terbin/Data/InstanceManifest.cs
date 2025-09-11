using System;

namespace Terbin.Data;

/// <summary>
/// ______( Manifiesto de la instancia )______<br />
/// - Contiene información sobre la instancia, como su nombre, versión y mods instalados.
/// </summary>
public class InstanceManifest
{
    public string? Name { get; set; }
    public string? Version { get; set; }
    public List<string>? Mods { get; set; }
}
