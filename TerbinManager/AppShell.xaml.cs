using System.Globalization;

namespace TerbinManager;

public class InverseBoolConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b ? !b : value!;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b ? !b : value!;
}

public class BoolToWidthConverter : IValueConverter
{
	// true => compacto (60), false => ancho normal (280 por defecto MAUI)
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is bool b && b ? 60d : 280d;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is double d && d <= 60d;
}

public partial class AppShell : Shell
{
	public static readonly BindableProperty IsFlyoutCompactProperty =
		BindableProperty.Create(nameof(IsFlyoutCompact), typeof(bool), typeof(AppShell), false);

	public bool IsFlyoutCompact
	{
		get => (bool)GetValue(IsFlyoutCompactProperty);
		set => SetValue(IsFlyoutCompactProperty, value);
	}

	public AppShell()
	{
		InitializeComponent();
		// Registrar rutas para navegación explícita
		Routing.RegisterRoute("settings", typeof(SettingsPage));
		Routing.RegisterRoute("instancias", typeof(InstancesPage));
		Routing.RegisterRoute("mods", typeof(ModsPage));

		// Estilo Windows: menú bloqueado a la izquierda
		// Definido también en XAML de Shell, no es necesario duplicarlo aquí
	}

	private async void OnSettingsMenuItemClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("settings");
	}

	private void OnToggleCompactClicked(object? sender, EventArgs e)
	{
		IsFlyoutCompact = !IsFlyoutCompact;
	}
}
