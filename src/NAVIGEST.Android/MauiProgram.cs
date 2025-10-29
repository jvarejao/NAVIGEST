using CommunityToolkit.Maui; // Toolkit base
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using UraniumUI;
using NAVIGEST.Android.Services.Icons; // <-- IIconProvider (namespace)
using NAVIGEST.Android.Pages;          // <-- Páginas
using NAVIGEST.Android.PageModels;     // <-- ViewModels
using NAVIGEST.Android.ViewModels;     // <-- HoursEntry

#if ANDROID
using Microsoft.Maui.Handlers;
using Android.Webkit;
using AColor = Android.Graphics.Color;
#endif

namespace NAVIGEST.Android
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseUraniumUI()
                .UseUraniumUIMaterial()
                .ConfigureSyncfusionToolkit()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    // Android: evitar flicker e permitir GIF via ficheiro local (file://)
                    WebViewHandler.Mapper.AppendToMapping("AndroidLocalFiles", (handler, view) =>
                    {
                        var wv = handler.PlatformView;
                        if (wv is null)
                            return;

                        // Fundo preto imediato
                        wv.SetBackgroundColor(AColor.Black);

                        // Ativar acesso a ficheiros locais
                        var s = wv.Settings;
                        if (s is not null)
                        {
                            s.AllowFileAccess = true;
                            s.AllowContentAccess = true;
                            s.AllowFileAccessFromFileURLs = true;
                            s.AllowUniversalAccessFromFileURLs = true;
                        }
                    });
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                    fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
                    fonts.AddFont("fa7_solid.otf", "FA7Solid");
                    fonts.AddFont("fa7-regular.otf", "FA7Regular");
                    fonts.AddFont("fa7_brands.otf", "FA7Brands");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // DI Services
            builder.Services.AddSingleton<ProjectRepository>();
            builder.Services.AddSingleton<TaskRepository>();
            builder.Services.AddSingleton<CategoryRepository>();
            builder.Services.AddSingleton<TagRepository>();
            builder.Services.AddSingleton<SeedDataService>();
            builder.Services.AddSingleton<ModalErrorHandler>();
            builder.Services.AddSingleton<MainYahPageViewModel>();
            builder.Services.AddSingleton<MainYahPage>();
            builder.Services.AddSingleton<ProjectListPageModel>();
            builder.Services.AddSingleton<ManageMetaPageModel>();
            builder.Services.AddTransientWithShellRoute<ProjectDetailPage, ProjectDetailPageModel>("project");
            builder.Services.AddTransientWithShellRoute<TaskDetailPage, TaskDetailPageModel>("task");
            builder.Services.AddTransient<ProductsPageModel>();
            builder.Services.AddTransient<ProductsPage>();
            builder.Services.AddSingleton<NAVIGEST.Android.Services.Auth.IBiometricAuthService, NAVIGEST.Android.Services.Auth.BiometricAuthService>();
            builder.Services.AddTransient<NAVIGEST.Android.PageModels.LoginPageModel>();

            // Horas
            builder.Services.AddTransient<HoursEntryViewModel>();
            builder.Services.AddTransient<HoursEntryPage>();

            // Splash e Welcome
            builder.Services.AddTransient<NAVIGEST.Android.Pages.SplashIntroPage>();
            builder.Services.AddTransient<NAVIGEST.Android.Pages.WelcomePage>();

            // Provider de Ícones por plataforma
#if ANDROID
            builder.Services.AddSingleton<NAVIGEST.Android.Services.Icons.IIconProvider, NAVIGEST.Android.Services.Icons.IconProvider>();
#endif

            return builder.Build();
        }
    }
}
