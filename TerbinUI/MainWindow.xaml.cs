using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TerbinUI
{
    public sealed partial class MainWindow : Window
    {
        private readonly Dictionary<string, Type> _pages = new()
        {
            ["inicio"] = typeof(Pages.InicioPage),
            ["instancias"] = typeof(Pages.InstanciasPage),
            ["mods"] = typeof(Pages.ModsPage),
            ["ajustes"] = typeof(Pages.AjustesPage),
        };

        public MainWindow()
        {
            InitializeComponent();

            // Extend content into title bar and use custom drag region
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;
            SetTitleBar(TitleBarDragRegion);
            AppWindow.Title = string.Empty; // hide text

            // Match standard caption height: 32px logical as default, adjust via Thickness
            TitleBarDragRegion.Height = 32;

            // Sync back button and nav selection
            ContentFrame.Navigated += ContentFrame_Navigated;

            // Start at Inicio only if not already navigated
            if (ContentFrame.SourcePageType == null)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
                NavigateTo("inicio");
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            NavView.IsBackEnabled = ContentFrame.CanGoBack;

            // Update selected item in NavView based on current page
            var currentType = e.SourcePageType;
            if (currentType != null)
            {
                var match = _pages.FirstOrDefault(kv => kv.Value == currentType);
                if (!string.IsNullOrEmpty(match.Key))
                {
                    var item = NavView.MenuItems
                        .OfType<NavigationViewItem>()
                        .FirstOrDefault(i => string.Equals(i.Tag as string, match.Key, StringComparison.OrdinalIgnoreCase));
                    if (item != null && NavView.SelectedItem != item)
                    {
                        NavView.SelectedItem = item;
                    }
                }
                else if (e.SourcePageType == typeof(Pages.AjustesPage))
                {
                    NavView.SelectedItem = NavView.SettingsItem;
                }
            }
        }

        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (ContentFrame.CanGoBack)
            {
                ContentFrame.GoBack();
            }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            string? tag = null;
            if (args.IsSettingsSelected)
            {
                tag = "ajustes";
            }
            else if (args.SelectedItem is NavigationViewItem item)
            {
                tag = item.Tag as string;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                NavigateTo(tag);
            }
        }

        private void NavigateTo(string tag)
        {
            if (_pages.TryGetValue(tag, out var pageType))
            {
                // Avoid navigating to same page type to prevent resetting state/back button flicker
                if (ContentFrame.SourcePageType == pageType)
                    return;

                // TODO: try cacth.
                ContentFrame.Navigate(pageType);
            }
        }
    }
}
