using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace TerbinUI.Pages;

public sealed partial class InstanciasPage : Page
{
    public class InstallationItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }

    public ObservableCollection<InstallationItem> Items { get; } = new();
    public ObservableCollection<InstallationItem> FilteredItems { get; } = new();
    private string _searchQuery = string.Empty;

    private static string TerbinConfigPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbin");
    private static string UiConfigPath => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terbinui.json");

    public InstanciasPage()
    {
        InitializeComponent();
        _ = LoadAsync();
    }

    private class TerbinConfigLite
    {
        public Dictionary<string, string>? Instances { get; set; }
    }

    private class UiConfig
    {
        public string? InstallationsPath { get; set; }
    }

    private void ApplyFilter()
    {
        try
        {
            var q = _searchQuery?.Trim() ?? string.Empty;
            var filtered = string.IsNullOrEmpty(q)
                ? Items
                : new ObservableCollection<InstallationItem>(Items.Where(i =>
                    (i.Name?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                    (i.Path?.IndexOf(q, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0));

            FilteredItems.Clear();
            foreach (var it in filtered)
                FilteredItems.Add(it);
        }
        catch { /* ignore */ }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchQuery = (sender as TextBox)?.Text ?? string.Empty;
        ApplyFilter();
    }

    private async Task<string?> EnsureInstallationsBasePathAsync()
    {
        string? basePath = null;
        try
        {
            if (File.Exists(UiConfigPath))
            {
                var json = await File.ReadAllTextAsync(UiConfigPath);
                var ui = JsonConvert.DeserializeObject<UiConfig>(json) ?? new UiConfig();
                if (!string.IsNullOrWhiteSpace(ui.InstallationsPath) && Directory.Exists(ui.InstallationsPath))
                {
                    basePath = ui.InstallationsPath;
                }
            }
        }
        catch { /* ignore */ }

        if (string.IsNullOrWhiteSpace(basePath))
        {
            // Ask the user to pick the base installations folder
            var picked = await PickFolderAsync();
            if (string.IsNullOrWhiteSpace(picked)) return null;
            basePath = picked;
            try
            {
                var ui = new UiConfig { InstallationsPath = basePath };
                var jsonOut = JsonConvert.SerializeObject(ui, Formatting.Indented);
                await File.WriteAllTextAsync(UiConfigPath, jsonOut);
            }
            catch { /* ignore */ }
        }

        return basePath;
    }

    private async Task LoadAsync()
    {
        try
        {
            Items.Clear();
            if (File.Exists(TerbinConfigPath))
            {
                var json = await File.ReadAllTextAsync(TerbinConfigPath);
                var cfg = JsonConvert.DeserializeObject<TerbinConfigLite>(json);
                var instances = cfg?.Instances;
                if (instances != null)
                {
                    foreach (var kv in instances.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Value))
                        {
                            Items.Add(new InstallationItem { Name = kv.Key, Path = kv.Value });
                        }
                    }
                }
            }
        }
        catch { /* ignore */ }
        finally
        {
            ApplyFilter();
        }
    }

    private static string SanitizeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var ch in invalid)
        {
            name = name.Replace(ch, '_');
        }
        return name.Trim();
    }

    private async void OnAddInstanceClick(object sender, RoutedEventArgs e)
    {
        var basePath = await EnsureInstallationsBasePathAsync();
        if (string.IsNullOrWhiteSpace(basePath)) return;

        var nameBox = new TextBox { PlaceholderText = "Nombre de la instalaci\u00F3n" };
        var pathBox = new TextBox();

        bool userEditedPath = false;
        bool isUpdatingPath = false;
        pathBox.TextChanging += (tb, args) => { if (!isUpdatingPath) userEditedPath = true; };
        void UpdatePathFromName()
        {
            if (userEditedPath) return;
            var sanitized = SanitizeName(nameBox.Text ?? string.Empty);
            isUpdatingPath = true;
            pathBox.Text = System.IO.Path.Combine(basePath!, sanitized);
            isUpdatingPath = false;
        }
        nameBox.TextChanged += (_, __) => UpdatePathFromName();
        UpdatePathFromName();

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Nombre" });
        panel.Children.Add(nameBox);
        panel.Children.Add(new TextBlock { Text = "Localizaci\u00F3n" });
        panel.Children.Add(pathBox);

        var dialog = new ContentDialog
        {
            Title = "Nueva instalaci\u00F3n",
            Content = panel,
            PrimaryButtonText = "Crear",
            CloseButtonText = "Cancelar",
            XamlRoot = this.XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        var rawName = nameBox.Text?.Trim();
        var dest = pathBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(rawName) || string.IsNullOrWhiteSpace(dest)) return;
        var name = SanitizeName(rawName);

        try
        {
            BusyOverlay.Visibility = Visibility.Visible;
            BusyProgress.Value = 0;
            BusyProgressText.Text = "0 %";

            var psi = new ProcessStartInfo
            {
                FileName = "terbin",
                Arguments = $"instances create \"{name}\" \"{dest}\"",
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
                Utilities.ChildProcessManager.Register(proc);

                // Leer stdout en streaming para capturar "(NN%)" incluso sin saltos de línea
                var reader = proc.StandardOutput;
                var buffer = new char[256];
                var window = new StringBuilder(64);
                var readTask = Task.Run(async () =>
                {
                    int read;
                    while ((read = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            char c = buffer[i];
                            if (window.Length > 60) window.Remove(0, window.Length - 60);
                            window.Append(c);

                            // parse porcentaje "(NN%)"
                            if (c == '%')
                            {
                                int idx = window.Length - 1;
                                int start = idx - 1;
                                while (start >= 0 && char.IsDigit(window[start])) start--;
                                start++;
                                if (start < idx && int.TryParse(window.ToString(start, idx - start), out int percent))
                                {
                                    _ = DispatcherQueue.TryEnqueue(() =>
                                    {
                                        BusyProgress.Value = Math.Clamp(percent, 0, 100);
                                        BusyProgressText.Text = $"{percent} %";
                                    });
                                }
                            }
                        }
                    }
                });

                // También leer stderr para evitar bloqueos (aunque no lo usamos)
                var errReader = proc.StandardError;
                var readErrTask = Task.Run(async () => { try { await errReader.ReadToEndAsync(); } catch { } });

                await Task.WhenAll(Task.Run(() => proc.WaitForExit()), readTask, readErrTask);
            }
        }
        catch (Exception ex)
        {
            _ = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "Cerrar", XamlRoot = this.XamlRoot }.ShowAsync();
        }
        finally
        {
            BusyOverlay.Visibility = Visibility.Collapsed;
            BusyProgress.Value = 0;
            BusyProgressText.Text = "";
        }

        await LoadAsync();
    }

    private static async Task<string?> PickFolderAsync()
    {
        var hwnd = App.MainAppWindow != null ? WinRT.Interop.WindowNative.GetWindowHandle(App.MainAppWindow) : IntPtr.Zero;
        var picker = new Windows.Storage.Pickers.FolderPicker
        {
            SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
        };
        picker.FileTypeFilter.Add("*");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    private void OnItemPointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Child is FrameworkElement child)
        {
            if (child.FindName("BottomOverlay") is FrameworkElement overlay)
            {
                PlayOverlay(overlay, show: true);
            }
        }
    }

    private void OnItemPointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Border border && border.Child is FrameworkElement child)
        {
            if (child.FindName("BottomOverlay") is FrameworkElement overlay)
            {
                PlayOverlay(overlay, show: false);
            }
        }
    }

    private static void PlayOverlay(FrameworkElement overlay, bool show)
    {
        overlay.IsHitTestVisible = show;

        var sb = new Storyboard();
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        var opacityAnim = new DoubleAnimation
        {
            To = show ? 1.0 : 0.0,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(opacityAnim, overlay);
        Storyboard.SetTargetProperty(opacityAnim, "Opacity");

        if (overlay.RenderTransform is not TranslateTransform tt)
        {
            tt = new TranslateTransform { Y = show ? 20 : 0 };
            overlay.RenderTransform = tt;
        }
        var yAnim = new DoubleAnimation
        {
            To = show ? 0.0 : 20.0,
            Duration = duration,
            EasingFunction = ease
        };
        Storyboard.SetTarget(yAnim, overlay);
        Storyboard.SetTargetProperty(yAnim, "(UIElement.RenderTransform).(TranslateTransform.Y)");

        sb.Children.Add(opacityAnim);
        sb.Children.Add(yAnim);
        sb.Begin();
    }

    private void OnExecuteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is InstallationItem item)
        {
            try
            {
                var exe = System.IO.Path.Combine(item.Path, "Farlands.exe");
                if (File.Exists(exe))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        WorkingDirectory = item.Path,
                        UseShellExecute = true
                    });
                }
                else
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", item.Path) { UseShellExecute = true });
                }
            }
            catch { /* ignore */ }
        }
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is InstallationItem item)
        {
            // Navigate to info page, pass (name, path)
            Frame?.Navigate(typeof(InstanciaInfoPage), (item.Name, item.Path), new DrillInNavigationTransitionInfo());
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is InstallationItem item)
        {
            var cd = new ContentDialog
            {
                Title = "Eliminar instancia",
                Content = $"\u00BFSeguro que quieres eliminar la instancia '{item.Name}' de la configuraci\u00F3n? (No borra archivos)",
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
                    Arguments = $"instances delete \"{item.Name}\" -y",
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

            await LoadAsync();
        }
    }
}
