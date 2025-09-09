using System;
using Newtonsoft.Json;

namespace Terbin.Data;

/// <summary>
/// Es la referencia del mod en el Json (¿que json?, en proceso de descubirlo).
/// </summary>
public class Reference
{
    [JsonProperty("name")]
    public string? Name;
    [JsonProperty("guid")]
    public string? GUID;
    [JsonProperty("url")]
    public string? manifestUrl;
}