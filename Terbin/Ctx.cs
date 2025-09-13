using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Terbin.Data;
using Index = Terbin.Data.Index;

namespace Terbin;

public static class Ctx
{
    public static bool existManifest;
    public static ProjectManifest? manifest;
    public static string? manifestPath;

    public static Config? config;
    public static Index? index;

    public static Logger Log { get; } = new Logger();

    public static StreamWriter? Writter;

    #region  PipeWritters
    public static void PipeWrite(object data, StatusCode code, string message = "")
    {
        if (Writter == null) return;

        PipeWrite(data, (int)code, message);
    }
    public static void PipeWrite(object data, int code, string message = "")
    {
        if (Writter == null) return;

        PipeWrite(data, new StatusResponse()
        {
            Code = code,
            Message = message,
        });
    }

    public static void PipeWrite(object data, StatusResponse status)
    {
        if (Writter == null) return;
        PipeWrite(new Response()
        {
            Status = status,
            Content = data,
        });
    }

    public static void PipeWrite(object data)
    {
        if (Writter == null) return;
        PipeWrite(JsonConvert.SerializeObject(data));
    }

    public static void PipeWrite(string data)
    {
        if (Writter == null) return;

        Writter.WriteLine(data);
    }
    #endregion
}
