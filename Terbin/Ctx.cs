using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
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

    public StreamWriter? Writter;

    #region  PipeWritters
    public void PipeWrite(object data, int code, string message)
    {
        if (Writter == null) return;

        PipeWrite(data, new StatusResponse()
        {
            Code = code,
            Message = message,
        });
    }

    public void PipeWrite(object data, StatusResponse status)
    {
        if (Writter == null) return;
        PipeWrite(new Response()
        {
            Status = status,
            Content = data,
        });
    }

    public void PipeWrite(object data)
    {
        if (Writter == null) return;
        PipeWrite(JsonConvert.SerializeObject(data));
    }

    public void PipeWrite(string data)
    {
        if (Writter == null) return;

        Writter.WriteLine(data);
    }
    #endregion
}
