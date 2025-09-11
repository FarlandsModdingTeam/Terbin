using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.UI.Xaml.Input;

namespace TerbinUI.Pages;

public sealed partial class AjustesPage : Page
{
    private class UiConfig
    {
        public string? InstallationsPath { get; set; }
    }

    // Minimal mirror of Terbin.Config for UI usage (only FarlandsPath, omite Instances e index)
    private class TerbinLiteConfig
    {
        public string? FarlandsPath { get; set; }
    }

    private static string UiConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbinui.json");
    private static string TerbinConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin", "config.json");

    public AjustesPage()
    {
        InitializeComponent();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            if (File.Exists(TerbinConfigPath))
            {
                var json = await File.ReadAllTextAsync(TerbinConfigPath);
                var cfg = JsonConvert.DeserializeObject<TerbinLiteConfig>(json);
                if (cfg != null)
                {
                    TbFarlandsPath.Text = cfg.FarlandsPath ?? string.Empty;
                }
            }

            if (File.Exists(UiConfigPath))
            {
                var jsonUi = await File.ReadAllTextAsync(UiConfigPath);
                var ui = JsonConvert.DeserializeObject<UiConfig>(jsonUi) ?? new UiConfig();
                TbInstallationsPath.Text = ui.InstallationsPath ?? string.Empty;
            }
        }
        catch { /* ignore */ }
    }

    private async Task SaveUiConfigAsync()
    {
        var ui = new UiConfig
        {
            InstallationsPath = string.IsNullOrWhiteSpace(TbInstallationsPath.Text) ? null : TbInstallationsPath.Text
        };
        var json = JsonConvert.SerializeObject(ui, Formatting.Indented);
        await File.WriteAllTextAsync(UiConfigPath, json);
    }

    private async Task SaveTerbinConfigFarlandsPathAsync()
    {
        try
        {
            var path = TbFarlandsPath.Text?.Trim();

            // Ensure directory exists
            var dir = Path.GetDirectoryName(TerbinConfigPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            JObject root;
            if (File.Exists(TerbinConfigPath))
            {
                try
                {
                    var current = await File.ReadAllTextAsync(TerbinConfigPath);
                    root = string.IsNullOrWhiteSpace(current) ? new JObject() : JObject.Parse(current);
                }
                catch
                {
                    root = new JObject();
                }
            }
            else
            {
                root = new JObject();
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                // remove FarlandsPath if empty
                root.Remove("FarlandsPath");
            }
            else
            {
                root["FarlandsPath"] = path;
            }

            var outJson = root.ToString(Formatting.Indented);
            await File.WriteAllTextAsync(TerbinConfigPath, outJson);
        }
        catch { /* ignore */ }
    }

    private async void OnBrowseFarlandsPath_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            TbFarlandsPath.Text = folder;
            await SaveTerbinConfigFarlandsPathAsync();
        }
    }

    private void OnOpenFarlandsPath_Click(object sender, RoutedEventArgs e)
    {
        var p = TbFarlandsPath.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", p) { UseShellExecute = true }); } catch { }
        }
    }

    private async void OnFarlandsPath_LostFocus(object sender, RoutedEventArgs e)
    {
        await SaveTerbinConfigFarlandsPathAsync();
    }

    private async void OnFarlandsPath_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await SaveTerbinConfigFarlandsPathAsync();
        }
    }

    private async void OnBrowseInstallationsPath_Click(object sender, RoutedEventArgs e)
    {
        var folder = await PickFolderAsync();
        if (!string.IsNullOrEmpty(folder))
        {
            TbInstallationsPath.Text = folder;
            await SaveUiConfigAsync();
        }
    }

    private void OnOpenInstallationsPath_Click(object sender, RoutedEventArgs e)
    {
        var p = TbInstallationsPath.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", p) { UseShellExecute = true }); } catch { }
        }
    }

    private static async Task<string?> PickFolderAsync()
    {
        var hwnd = App.MainAppWindow != null ? WindowNative.GetWindowHandle(App.MainAppWindow) : IntPtr.Zero;
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }


}
