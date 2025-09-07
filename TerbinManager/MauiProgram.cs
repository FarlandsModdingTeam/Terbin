using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
#endif

namespace TerbinManager;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Evitar que el contenido se meta bajo la barra de título en Windows
		builder.ConfigureLifecycleEvents(events =>
		{
#if WINDOWS
			events.AddWindows(w =>
			{
				w.OnWindowCreated(window =>
				{
					// Desactiva el título personalizado para que MAUI no extienda el contenido bajo la barra del sistema
					window.ExtendsContentIntoTitleBar = false;

					// Opcional: asegurar que el presentador de ventana muestre barra de título del sistema
					var appWindow = window.AppWindow;
					if (appWindow?.Presenter is OverlappedPresenter presenter)
					{
						presenter.IsResizable = true;
						presenter.IsMinimizable = true;
						presenter.IsMaximizable = true;
					}
				});
			});
#endif
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
