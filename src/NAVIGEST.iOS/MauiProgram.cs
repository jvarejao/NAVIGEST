using CommunityToolkit.Maui; // Toolkit base
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using UraniumUI;
using NAVIGEST.iOS.Services.Icons; // <-- IIconProvider (namespace)
using NAVIGEST.iOS.Pages;          // <-- ADICIONADO: HoursEntryPage
using NAVIGEST.iOS.ViewModels;     // <-- ADICIONADO: HoursEntryViewModel

#if WINDOWS
using System.Reflection;
using Microsoft.Maui.Handlers;
using Microsoft.UI; // ColorHelper
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls; // WebView2
using Microsoft.UI.Xaml.Input;
#endif

#if ANDROID
using Microsoft.Maui.Handlers;
using Android.Webkit;
using AColor = Android.Graphics.Color;
#endif

namespace NAVIGEST.iOS
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
#if WINDOWS
                    // WinUI: evitar flash branco antes do primeiro frame do WebView
                    WebViewHandler.Mapper.AppendToMapping("WinNoWhiteFlash", (handler, view) =>
                    {
                        if (handler.PlatformView is WebView2 wv2)
                        {
                            // preto opaco desde o 1º frame
                            wv2.DefaultBackgroundColor = ColorHelper.FromArgb(255, 0, 0, 0);
                        }
                    });

                    // Cursor de mão em botões (se já usavas)
                    ButtonHandler.Mapper.AppendToMapping("HandCursorWin", (handler, v) =>
                    {
                        if (handler.PlatformView is FrameworkElement fe)
                        {
                            var prop = typeof(FrameworkElement).GetProperty(
                                "ProtectedCursor",
                                BindingFlags.Instance | BindingFlags.NonPublic);

                            void SetHand()  => prop?.SetValue(fe, InputSystemCursor.Create(InputSystemCursorShape.Hand));
                            void SetArrow() => prop?.SetValue(fe, null);

                            fe.PointerEntered += (_, __) => SetHand();
                            fe.PointerMoved   += (_, __) => SetHand();
                            fe.PointerExited  += (_, __) => SetArrow();
                        }
                    });
#endif

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
                            // s.MixedContentMode = MixedContentHandling.AlwaysAllow; // só se precisares
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
                    // fonts.AddFont("Inter-Light.ttf", "InterLight");        // Fonte não existe no iOS
                    // fonts.AddFont("Inter-Regular.ttf", "Inter");           // Fonte não existe no iOS
                    // fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");  // Fonte não existe no iOS
                    fonts.AddFont("fa7_solid.otf", "FA7Solid");
                    fonts.AddFont("fa7-regular.otf", "FA7Regular");
                    fonts.AddFont("fa7_brands.otf", "FA7Brands");
                });

#if DEBUG
            builder.Logging.AddDebug();
            builder.Services.AddLogging(configure => configure.AddDebug());
#endif

            // DI existente
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
            builder.Services.AddSingleton<NAVIGEST.iOS.Services.Auth.IBiometricAuthService, NAVIGEST.iOS.Services.Auth.BiometricAuthService>();
            builder.Services.AddTransient<NAVIGEST.iOS.PageModels.LoginPageModel>();


            // ===== NOVO: Horas (layout — Passo 1) =====
            builder.Services.AddTransient<HoursEntryViewModel>();
            builder.Services.AddTransient<HoursEntryPage>();
            // (Mantive o padrão igual ao de Products: DI simples; a rota está no AppShell)

            // ===== Provider de Ícones por plataforma (mantido) =====
#if WINDOWS
            builder.Services.AddSingleton<IIconProvider, IconProvider>();
#elif ANDROID
            builder.Services.AddSingleton<IIconProvider, IconProvider>();
#elif IOS
            builder.Services.AddSingleton<IIconProvider, IconProvider>();
#endif
            // =====================================================

            return builder.Build();
        }
    }
}
