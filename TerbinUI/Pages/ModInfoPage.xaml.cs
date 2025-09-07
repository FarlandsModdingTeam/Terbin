using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml.Media.Animation;

namespace TerbinUI.Pages;

public sealed partial class ModInfoPage : Page
{
    private class UiTerbinIndexRef
    {
        public string? name { get; set; }
        public string? guid { get; set; }
        public string? url { get; set; }
    }
    private class UiManifest
    {
        public string? Name { get; set; }
        public string? GUID { get; set; }
        public System.Collections.Generic.List<string>? Versions { get; set; }
        public string? url { get; set; }
        public System.Collections.Generic.List<string>? Dependencies { get; set; }
    }
    private class UiIndex { public System.Collections.Generic.List<UiTerbinIndexRef>? references { get; set; } }
    private class UiConfig { public UiIndex? index { get; set; } }

    private static string TerbinConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");

    private string ModId = string.Empty;
    private string ModName = string.Empty;

    public ModInfoPage()
    {
        this.InitializeComponent();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ValueTuple<string, string> tuple)
        {
            ModId = tuple.Item1;
            ModName = tuple.Item2;
            TxtTitle.Text = ModName;
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        TxtGuid.Text = ModId;
        try
        {
            // Ensure index and get manifest URL
            await EnsureIndexAsync();
            var idx = await ReadIndexAsync();
            var match = idx?.references?.FirstOrDefault(r => string.Equals(r.guid, ModId, StringComparison.OrdinalIgnoreCase) || string.Equals(r.name, ModId, StringComparison.OrdinalIgnoreCase));
            if (match == null || string.IsNullOrWhiteSpace(match.url))
            {
                TxtUrl.Text = "(URL de manifest no encontrada en el índice)";
                TxtNoDeps.Visibility = Visibility.Visible;
                return;
            }

            TxtUrl.Text = match.url;

            // Download manifest
            string json = await DownloadStringAsync(match.url);
            var man = JsonConvert.DeserializeObject<UiManifest>(json);
            if (man != null)
            {
                var latest = man.Versions != null && man.Versions.Count > 0 ? man.Versions[^1] : "-";
                TxtLatest.Text = latest;
                DepsList.ItemsSource = man.Dependencies ?? new System.Collections.Generic.List<string>();
                TxtNoDeps.Visibility = (man.Dependencies == null || man.Dependencies.Count == 0) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                TxtLatest.Text = "-";
                TxtNoDeps.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            TxtUrl.Text = "(Error obteniendo información)";
            TxtLatest.Text = "-";
            TxtNoDeps.Visibility = Visibility.Visible;
        }
    }

    private static async Task EnsureIndexAsync()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "terbin",
                Arguments = "mods update",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                _ = await proc.StandardOutput.ReadToEndAsync();
                _ = await proc.StandardError.ReadToEndAsync();
                await Task.Run(() => proc.WaitForExit());
            }
        }
        catch { }
    }

    private static async Task<UiIndex?> ReadIndexAsync()
    {
        try
        {
            if (!File.Exists(TerbinConfigPath)) return null;
            var json = await File.ReadAllTextAsync(TerbinConfigPath, Encoding.UTF8);
            var cfg = JsonConvert.DeserializeObject<UiConfig>(json);
            return cfg?.index;
        }
        catch { return null; }
    }

    private static async Task<string> DownloadStringAsync(string url)
    {
        using var client = new System.Net.Http.HttpClient();
        return await client.GetStringAsync(url);
    }

    private void OnGoBackToListClick(object sender, RoutedEventArgs e)
    {
        // Preferir volver por la pila de navegación para respetar el origen (Instancia o Mods)
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
            return;
        }

        // Fallback: navegar a la lista de mods si no hay historial
        var frame = this.Frame;
        while (frame?.Parent is Frame parent)
        {
            frame = parent;
        }
        frame?.Navigate(typeof(ModsPage), null, new DrillInNavigationTransitionInfo());
    }
}
