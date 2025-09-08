using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Newtonsoft.Json;
using System.Diagnostics;

namespace TerbinUI.Pages;

public sealed partial class ModsPage : Page
{
    private class UiReference
    {
        public string? name { get; set; }
        public string? guid { get; set; }
        public string? url { get; set; }
    }

    private static string UserDir => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private static string WebIndexPath => Path.Combine(UserDir, ".terbin", "web.index");
    private static string LocalIndexPath => Path.Combine(UserDir, ".terbin", "local.index");

    public class ModItem
    {
        public string Name { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
    }

    public List<ModItem> WebMods { get; } = new();
    public List<ModItem> LocalMods { get; } = new();

    public ModsPage()
    {
        InitializeComponent();
        _ = LoadAsync();
    }

    private async Task EnsureIndexAsync()
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

    private static List<ModItem> MapRefs(IEnumerable<UiReference> refsEnum)
        => refsEnum
            .OrderBy(r => r.name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(r => new ModItem { Name = r.name ?? r.guid ?? "(sin nombre)", Guid = r.guid ?? string.Empty })
            .ToList();

    private async Task LoadAsync()
    {
        await EnsureIndexAsync();
        WebMods.Clear();
        LocalMods.Clear();

        try
        {
            if (File.Exists(WebIndexPath))
            {
                var wjson = await File.ReadAllTextAsync(WebIndexPath, Encoding.UTF8);
                var wrefs = JsonConvert.DeserializeObject<List<UiReference>>(wjson) ?? new();
                WebMods.AddRange(MapRefs(wrefs));
            }
        }
        catch { }

        try
        {
            if (File.Exists(LocalIndexPath))
            {
                var ljson = await File.ReadAllTextAsync(LocalIndexPath, Encoding.UTF8);
                var lrefs = JsonConvert.DeserializeObject<List<UiReference>>(ljson) ?? new();
                LocalMods.AddRange(MapRefs(lrefs));
            }
        }
        catch { }

        WebModsGrid.ItemsSource = WebMods;
        LocalModsGrid.ItemsSource = LocalMods;
    }

    private void OnModClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ModItem item)
        {
            var id = string.IsNullOrWhiteSpace(item.Guid) ? item.Name : item.Guid;
            Frame?.Navigate(typeof(ModInfoPage), (id, item.Name), new DrillInNavigationTransitionInfo());
        }
    }
}
