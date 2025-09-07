using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Newtonsoft.Json;

namespace TerbinUI.Pages;

public sealed partial class InstanciaInfoPage : Page
{
    public class InstanceManifest
    {
        public string? Name { get; set; }
        public string? Version { get; set; }
        public List<string>? Mods { get; set; }
    }

    public class ModDisplay
    {
        public string Id { get; set; } = string.Empty; // GUID or name key stored in manifest
        public string Name { get; set; } = string.Empty; // Human readable name
    }

    private class SelectableMod
    {
        public UiTerbinIndexRef Ref { get; set; } = new UiTerbinIndexRef();
        public string Display { get; set; } = string.Empty;
        public override string ToString() => Display;
    }

    // Mirror types for reading terbin config index
    private class UiTerbinIndexRef
    {
        public string? name { get; set; }
        public string? guid { get; set; }
        public string? url { get; set; }
        public override string ToString() => string.IsNullOrWhiteSpace(name) ? (guid ?? string.Empty) : ($"{name} [{guid}]");
    }
    private class UiTerbinIndex
    {
        public List<UiTerbinIndexRef>? references { get; set; }
    }
    private class UiTerbinConfig
    {
        public UiTerbinIndex? index { get; set; }
    }

    public string InstanceName { get; private set; } = string.Empty;
    public string InstancePath { get; private set; } = string.Empty;

    public InstanciaInfoPage()
    {
        InitializeComponent();
    }

    protected override async void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ValueTuple<string, string> tuple)
        {
            InstanceName = tuple.Item1;
            InstancePath = tuple.Item2;
            await LoadAsync();
        }
    }

    private async Task LoadAsync()
    {
        TxtName.Text = InstanceName;
        TxtPath.Text = InstancePath;

        var manifestPath = Path.Combine(InstancePath, "manifest.json");
        if (File.Exists(manifestPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(manifestPath, Encoding.UTF8);
                var man = JsonConvert.DeserializeObject<InstanceManifest>(json) ?? new InstanceManifest();
                var modsIds = man.Mods ?? new List<string>();

                // Map to names using local index; fall back to id if not found
                var refs = await LoadModsIndexAsync();
                var byGuid = refs.ToDictionary(r => (r.guid ?? string.Empty).ToLowerInvariant(), r => r);
                var byName = refs.ToDictionary(r => (r.name ?? string.Empty).ToLowerInvariant(), r => r);
                var display = new List<ModDisplay>();
                foreach (var id in modsIds)
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    var key = id.ToLowerInvariant();
                    if (byGuid.TryGetValue(key, out var r1)) display.Add(new ModDisplay { Id = id, Name = r1.name ?? r1.guid ?? id });
                    else if (byName.TryGetValue(key, out var r2)) display.Add(new ModDisplay { Id = id, Name = r2.name ?? id });
                    else display.Add(new ModDisplay { Id = id, Name = id });
                }

                ModsList.ItemsSource = display;
                TxtNoMods.Visibility = display.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                ModsList.ItemsSource = Array.Empty<ModDisplay>();
                TxtNoMods.Text = "(Error leyendo manifest.json)";
                TxtNoMods.Visibility = Visibility.Visible;
            }
        }
        else
        {
            ModsList.ItemsSource = Array.Empty<ModDisplay>();
            TxtNoMods.Text = "(No hay manifest.json en la instancia)";
            TxtNoMods.Visibility = Visibility.Visible;
        }
    }

    private void OnGoHomeClick(object sender, RoutedEventArgs e)
    {
        var frame = this.Frame;
        while (frame?.Parent is Frame parent)
        {
            frame = parent;
        }
        frame?.Navigate(typeof(InstanciasPage), null, new DrillInNavigationTransitionInfo());
    }

    private void OnOpenPathClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(InstancePath))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", InstancePath) { UseShellExecute = true });
            }
        }
        catch { }
    }

    private void OnExecuteClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var exe = Path.Combine(InstancePath, "Farlands.exe");
            if (File.Exists(exe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = InstancePath,
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo("explorer.exe", InstancePath) { UseShellExecute = true });
            }
        }
        catch { }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        var cd = new ContentDialog
        {
            Title = "Eliminar instancia",
            Content = $"\u00BFSeguro que quieres eliminar la instancia '{InstanceName}' de la configuraci\u00F3n? (No borra archivos)",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            XamlRoot = this.XamlRoot
        };
        var res = await cd.ShowAsync();
        if (res != ContentDialogResult.Primary) return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "terbin",
                Arguments = $"instances delete \"{InstanceName}\" -y",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            var stdout = await proc!.StandardOutput.ReadToEndAsync();
            var stderr = await proc!.StandardError.ReadToEndAsync();
            await Task.Run(() => proc!.WaitForExit());
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                _ = new ContentDialog { Title = "terbin (stderr)", Content = new ScrollViewer { Content = new TextBlock { Text = stderr, TextWrapping = TextWrapping.Wrap } }, CloseButtonText = "Cerrar", XamlRoot = this.XamlRoot }.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            _ = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "Cerrar", XamlRoot = this.XamlRoot }.ShowAsync();
        }

        // Volver usando navegación estándar
        if (Frame?.CanGoBack == true)
        {
            Frame.GoBack();
        }
        else
        {
            Frame?.Navigate(typeof(InstanciasPage));
        }
    }

    private static string TerbinConfigPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");

    private async Task<List<UiTerbinIndexRef>> LoadModsIndexAsync()
    {
        // Ensure index via CLI (silent)
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
                // read asynchronously but ignore content
                _ = await proc.StandardOutput.ReadToEndAsync();
                _ = await proc.StandardError.ReadToEndAsync();
                await Task.Run(() => proc.WaitForExit());
            }
        }
        catch { /* ignore */ }

        try
        {
            if (!File.Exists(TerbinConfigPath)) return new List<UiTerbinIndexRef>();
            var json = await File.ReadAllTextAsync(TerbinConfigPath, Encoding.UTF8);
            var cfg = JsonConvert.DeserializeObject<UiTerbinConfig>(json);
            var refs = cfg?.index?.references ?? new List<UiTerbinIndexRef>();
            // Normalize display when missing guid/name
            foreach (var r in refs)
            {
                if (string.IsNullOrWhiteSpace(r.name) && !string.IsNullOrWhiteSpace(r.guid)) r.name = r.guid;
            }
            return refs;
        }
        catch
        {
            return new List<UiTerbinIndexRef>();
        }
    }

    private async void OnAddModClick(object sender, RoutedEventArgs e)
    {
        // Load index
        var refs = await LoadModsIndexAsync();
        if (refs.Count == 0)
        {
            _ = new ContentDialog { Title = "Índice vacío", Content = "No se encontraron mods en el índice. Ejecuta 'terbin mods update' e inténtalo de nuevo.", CloseButtonText = "Cerrar", XamlRoot = this.XamlRoot }.ShowAsync();
            return;
        }

        var items = refs
            .OrderBy(r => r.name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(r => new SelectableMod { Ref = r, Display = r.name ?? r.guid ?? "(sin nombre)" })
            .ToList();

        var searchBox = new TextBox { PlaceholderText = "Buscar..." };
        var listView = new ListView { SelectionMode = ListViewSelectionMode.Single, Height = 380 };
        void ApplyFilter()
        {
            var q = (searchBox.Text ?? string.Empty).Trim();
            IEnumerable<SelectableMod> src = items;
            if (!string.IsNullOrEmpty(q))
            {
                src = items.Where(i =>
                    (i.Display?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Ref.name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Ref.guid?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }
            listView.ItemsSource = src.ToList();
        }
        searchBox.TextChanged += (_, __) => ApplyFilter();
        ApplyFilter();

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(searchBox);
        panel.Children.Add(listView);

        var dialog = new ContentDialog
        {
            Title = "Selecciona un mod",
            Content = panel,
            PrimaryButtonText = "Agregar",
            CloseButtonText = "Cancelar",
            XamlRoot = this.XamlRoot
        };

        // Disable primary until selection
        dialog.PrimaryButtonClick += (_, args) =>
        {
            if (listView.SelectedItem == null)
            {
                args.Cancel = true;
            }
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;
        if (listView.SelectedItem is not SelectableMod selected) return;
        var chosen = selected.Ref;
        var modKey = !string.IsNullOrWhiteSpace(chosen.guid) ? chosen.guid : (chosen.name ?? string.Empty);
        if (string.IsNullOrWhiteSpace(modKey)) return;

        try
        {
            BusyOverlay.Visibility = Visibility.Visible;

            var psi = new ProcessStartInfo
            {
                FileName = "terbin",
                Arguments = $"instances add \"{InstanceName}\" \"{modKey}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                var stdout = await proc.StandardOutput.ReadToEndAsync();
                var stderr = await proc.StandardError.ReadToEndAsync();
                await Task.Run(() => proc.WaitForExit());
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    _ = new ContentDialog { Title = "terbin (stderr)", Content = new ScrollViewer { Content = new TextBlock { Text = stderr, TextWrapping = TextWrapping.Wrap } }, CloseButtonText = "Cerrar", XamlRoot = this.XamlRoot }.ShowAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _ = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "Cerrar", XamlRoot = this.XamlRoot }.ShowAsync();
        }
        finally
        {
            BusyOverlay.Visibility = Visibility.Collapsed;
        }

        await LoadAsync();
    }

    private async void OnModItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ModDisplay md)
        {
            Frame?.Navigate(typeof(TerbinUI.Pages.ModInfoPage), (md.Id, md.Name), new DrillInNavigationTransitionInfo());
        }
    }
}
