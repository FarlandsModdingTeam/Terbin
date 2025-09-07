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
    private class UiTerbinIndexRef
    {
        public string? name { get; set; }
        public string? guid { get; set; }
        public string? url { get; set; }
    }
    private class UiIndex { public List<UiTerbinIndexRef>? references { get; set; } }
    private class UiConfig { public UiIndex? index { get; set; } }

    private static string TerbinConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");

    public class ModItem
    {
        public string Name { get; set; } = string.Empty;
        public string Guid { get; set; } = string.Empty;
    }

    public List<ModItem> Mods { get; } = new();

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

    private async Task LoadAsync()
    {
        await EnsureIndexAsync();
        Mods.Clear();

        try
        {
            if (!File.Exists(TerbinConfigPath)) return;
            var json = await File.ReadAllTextAsync(TerbinConfigPath, Encoding.UTF8);
            var cfg = JsonConvert.DeserializeObject<UiConfig>(json);
            var refs = cfg?.index?.references ?? new List<UiTerbinIndexRef>();
            Mods.AddRange(refs
                .OrderBy(r => r.name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(r => new ModItem { Name = r.name ?? r.guid ?? "(sin nombre)", Guid = r.guid ?? string.Empty }));
            ModsGrid.ItemsSource = Mods;
        }
        catch
        {
            ModsGrid.ItemsSource = Array.Empty<ModItem>();
        }
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
