using System;
using Terbin.Data;

namespace Terbin.Commands;

public static class Checkers
{
    public static bool IsConfigUnloaded(Ctx ctx, string msg = "Config not loaded.")
    {
        var res = ctx.config == null;
        if (res)
        {
            ctx.Log.Error(msg);
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);
        }

        return res;
    }

    public static bool IsArgumentsEmpty(Ctx ctx, string[] args, string msg = "No arguments")
    {
        return HasNotEnoughArgs(ctx, args, 1, msg);
    }

    public static bool HasNotEnoughArgs(Ctx ctx, string[] args, int number, string msg = "Not enough arguments")
    {
        var res = args.Length < number;
        if (res)
        {
            ctx.Log.Error(msg);
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);

        }

        return res;
    }

    public static bool IsNullOrWhiteSpace(Ctx ctx, string str, string msg = "Empty string")
    {
        var res = string.IsNullOrWhiteSpace(str);

        if (res)
        {
            ctx.Log.Error(msg);
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);
        }

        return res;
    }

    public static bool NotExistingDirectory(Ctx ctx, string path, string msg = "Not existing directory")
    {
        var res = Directory.Exists(path);

        if (res)
        {
            ctx.Log.Error(msg);
            ctx.PipeWrite(null, StatusCode.BAD_REQUEST, msg);
        }

        return res;
    }
}
