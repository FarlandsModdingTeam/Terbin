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
using System.Collections.Generic;

namespace TerbinUI.Pages;

public sealed partial class ModInfoPage : Page
{
    private class UiReference
    {
        public string? name { get; set; }
        public string? guid { get; set; }
        public string? url { get; set; }
    }

    private class UiManifest
    {
        public string? Name { get; set; }
        public string? GUID { get; set; }
        public List<string>? Versions { get; set; }
        public string? url { get; set; }
        public List<string>? Dependencies { get; set; }
    }

    private static string UserDir => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string WebIndexPath => Path.Combine(UserDir, ".terbin", "web.index");
    private static string LocalIndexPath => Path.Combine(UserDir, ".terbin", "local.index");

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
            await EnsureIndexAsync();

            string? manifestUrl = await FindManifestUrlAsync(ModId);
            if (string.IsNullOrWhiteSpace(manifestUrl))
            {
                TxtUrl.Text = "(URL de manifest no encontrada en los índices)";
                TxtNoDeps.Visibility = Visibility.Visible;
                return;
            }

            TxtUrl.Text = manifestUrl;

            // Download manifest
            string json = await DownloadStringAsync(manifestUrl);
            var man = JsonConvert.DeserializeObject<UiManifest>(json);
            if (man != null)
            {
                var latest = man.Versions != null && man.Versions.Count > 0 ? man.Versions[^1] : "-";
                TxtLatest.Text = latest;
                DepsList.ItemsSource = man.Dependencies ?? new List<string>();
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

    private static async Task<string?> FindManifestUrlAsync(string id)
    {
        try
        {
            // Search in local first, then web
            var localUrl = await FindInIndexAsync(LocalIndexPath, id);
            if (!string.IsNullOrWhiteSpace(localUrl)) return localUrl;
            return await FindInIndexAsync(WebIndexPath, id);
        }
        catch { return null; }
    }

    private static async Task<string?> FindInIndexAsync(string path, string id)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
            var list = JsonConvert.DeserializeObject<List<UiReference>>(json) ?? new();
            var hit = list.FirstOrDefault(r => string.Equals(r.guid, id, StringComparison.OrdinalIgnoreCase) || string.Equals(r.name, id, StringComparison.OrdinalIgnoreCase));
            return hit?.url;
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
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
            return;
        }

        var frame = this.Frame;
        while (frame?.Parent is Frame parent)
        {
            frame = parent;
        }
        frame?.Navigate(typeof(ModsPage), null, new DrillInNavigationTransitionInfo());
    }
}
