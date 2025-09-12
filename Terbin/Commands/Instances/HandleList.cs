namespace Terbin.Commands.Instances;

internal class HandleList
{
    public static void List(Ctx ctx, string[] args)
    {
        var items = ctx.config!.GetInstances().OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"- {kv.Key}: {kv.Value}");

        ctx.Log.Box("Instances", items);
    }
}