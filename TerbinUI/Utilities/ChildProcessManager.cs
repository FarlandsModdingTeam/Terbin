using System.Collections.Concurrent;
using System.Diagnostics;

namespace TerbinUI.Utilities;

public static class ChildProcessManager
{
    private static readonly ConcurrentDictionary<int, Process> _children = new();

    public static void Register(Process p)
    {
        if (p == null) return;
        _children[p.Id] = p;
        p.EnableRaisingEvents = true;
        p.Exited += (_, __) =>
        {
            _children.TryRemove(p.Id, out Process _);
        };
    }

    public static void KillAll()
    {
        foreach (var kv in _children.ToArray())
        {
            try
            {
                if (!kv.Value.HasExited)
                {
                    kv.Value.Kill(entireProcessTree: true);
                }
            }
            catch { }
            finally
            {
                _children.TryRemove(kv.Key, out Process _);
            }
        }
    }
}
