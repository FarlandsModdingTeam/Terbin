using System;
using Newtonsoft.Json;

namespace Terbin.Data;


public class Reference
{
    [JsonProperty("name")]
    public string? Name;
    [JsonProperty("guid")]
    public string? GUID;
    [JsonProperty("url")]
    public string? manifestUrl;
}