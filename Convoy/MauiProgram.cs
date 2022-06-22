using Convoy.ViewModels;

namespace Convoy;

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
#if MACCATALYST
        		builder.Services.AddTransient<IFolderPicker, Platforms.MacCatalyst.FolderPicker>();
#elif WINDOWS
        		builder.Services.AddTransient<IFolderPicker, Platforms.Windows.FolderPicker>();
#endif
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<App>();
        return builder.Build();
	}
}
