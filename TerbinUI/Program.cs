using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.DynamicDependency;

namespace TerbinUI;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Initialize COM wrappers (required for WinUI 3)
        WinRT.ComWrappersSupport.InitializeComWrappers();

        // Initialize Windows App SDK for unpackaged scenarios via Dynamic Dependency Bootstrapper
        var bootstrapped = BootstrapTryInitialize();
        try
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        finally
        {
            if (bootstrapped)
            {
                try { Bootstrap.Shutdown(); } catch { /* ignore */ }
            }
        }
    }

    private static bool BootstrapTryInitialize()
    {

        try
        {
            // 0x00010007 => 1.7 major.minor
            Bootstrap.Initialize(0x00010007);
            return true;
        }
        catch
        {

            // If packaged or already initialized, ignore
            return false;
        }
    }


}
