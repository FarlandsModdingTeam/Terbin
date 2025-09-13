using System;
using Terbin.Data;

namespace Terbin.Commands;

public static class Checkers
{
    public static bool IsConfigUnloaded(string msg = "Config not loaded.")
    {
        var res = Ctx.config == null;
        if (res)
        {
            Ctx.Log.Error(msg);
            Ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);
        }

        return res;
    }

    public static bool IsArgumentsEmpty(string[] args, string msg = "No arguments")
    {
        return HasNotEnoughArgs(args, 1, msg);
    }

    public static bool HasNotEnoughArgs(string[] args, int number, string msg = "Not enough arguments")
    {
        var res = args.Length < number;
        if (res)
        {
            Ctx.Log.Error(msg);
            Ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);

        }

        return res;
    }

    public static bool IsNullOrWhiteSpace(string str, string msg = "Empty string")
    {
        var res = string.IsNullOrWhiteSpace(str);

        if (res)
        {
            Ctx.Log.Error(msg);
            Ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);
        }

        return res;
    }

    public static bool NotExistingDirectory( string path, string msg = "Not existing directory")
    {
        var res = Directory.Exists(path);

        if (res)
        {
            Ctx.Log.Error(msg);
            Ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);
        }

        return res;
    }
}
