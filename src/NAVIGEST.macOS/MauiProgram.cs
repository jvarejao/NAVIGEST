using CommunityToolkit.Maui; // Toolkit base
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
using LiveChartsCore; // Try adding this
using NAVIGEST.macOS.Services.Icons; // <-- IIconProvider (namespace)
using NAVIGEST.macOS.Pages;          // <-- ADICIONADO: HoursEntryPage
using NAVIGEST.macOS.ViewModels;     // <-- ADICIONADO: HoursEntryViewModel
using NAVIGEST.macOS.Data;           // Repositories
using NAVIGEST.macOS.Services;       // SeedDataService, ModalErrorHandler
using NAVIGEST.macOS.PageModels;     // PageModels

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

#if IOS || MACCATALYST
using Microsoft.Maui.Handlers;
using UIKit;
#endif

namespace NAVIGEST.macOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseSkiaSharp()
                .UseLiveCharts()
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

#if IOS || MACCATALYST
                    // iOS/macOS: Entry com border arredondado e animação
                    EntryHandler.Mapper.AppendToMapping("CustomBorder", (handler, view) =>
                    {
                        if (handler.PlatformView is UITextField textField && view is Entry entry)
                        {
                            // Remove border nativo
                            textField.BorderStyle = UITextBorderStyle.None;
                            
                            // Adicionar padding interno
                            var paddingView = new UIView(new CoreGraphics.CGRect(0, 0, 12, textField.Frame.Height));
                            textField.LeftView = paddingView;
                            textField.LeftViewMode = UITextFieldViewMode.Always;
                            textField.RightView = new UIView(new CoreGraphics.CGRect(0, 0, 12, textField.Frame.Height));
                            textField.RightViewMode = UITextFieldViewMode.Always;
                            
                            // Aplicar cor de fundo
                            if (entry.BackgroundColor != null)
                            {
                                var color = entry.BackgroundColor;
                                textField.BackgroundColor = UIColor.FromRGBA(
                                    (nfloat)color.Red, 
                                    (nfloat)color.Green, 
                                    (nfloat)color.Blue, 
                                    (nfloat)color.Alpha);
                            }
                            
                            // Border com cantos arredondados
                            textField.Layer.CornerRadius = 10;
                            textField.Layer.BorderWidth = 1.5f;
                            textField.Layer.MasksToBounds = true;
                            
                            // Cor do border baseada no tema
                            var isDark = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
                            var borderColor = isDark ? UIColor.FromRGBA(56/255f, 56/255f, 58/255f, 1f) : UIColor.FromRGBA(198/255f, 198/255f, 200/255f, 1f);
                            textField.Layer.BorderColor = borderColor.CGColor;
                            
                            // Animação no foco
                            textField.EditingDidBegin += (s, e) =>
                            {
                                UIView.Animate(0.2, () =>
                                {
                                    textField.Layer.BorderWidth = 2f;
                                    textField.Layer.BorderColor = UIColor.FromRGB(0, 122, 255).CGColor; // iOS Blue
                                });
                            };
                            
                            textField.EditingDidEnd += (s, e) =>
                            {
                                UIView.Animate(0.2, () =>
                                {
                                    textField.Layer.BorderWidth = 1.5f;
                                    var isDarkMode = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
                                    var normalBorder = isDarkMode ? UIColor.FromRGBA(56/255f, 56/255f, 58/255f, 1f) : UIColor.FromRGBA(198/255f, 198/255f, 200/255f, 1f);
                                    textField.Layer.BorderColor = normalBorder.CGColor;
                                });
                            };
                        }
                    });

                    // Aplica animação hover nativa em TODOS os botões (iOS e Mac Catalyst)
                    ButtonHandler.Mapper.PrependToMapping("MacHoverAll", (handler, view) =>
                    {
                        if (handler.PlatformView is UIButton button)
                        {
                            var hoverGesture = new UIHoverGestureRecognizer((gesture) =>
                            {
                                if (gesture.State == UIGestureRecognizerState.Began || 
                                    gesture.State == UIGestureRecognizerState.Changed)
                                {
                                    UIView.Animate(0.15, () =>
                                    {
                                        button.Alpha = 0.85f;
                                        button.Transform = CoreGraphics.CGAffineTransform.MakeScale(1.02f, 1.02f);
                                    });
                                }
                                else if (gesture.State == UIGestureRecognizerState.Ended ||
                                         gesture.State == UIGestureRecognizerState.Cancelled)
                                {
                                    UIView.Animate(0.15, () =>
                                    {
                                        button.Alpha = 1.0f;
                                        button.Transform = CoreGraphics.CGAffineTransform.MakeIdentity();
                                    });
                                }
                            });
                            button.AddGestureRecognizer(hoverGesture);
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
                    fonts.AddFont("Inter-Light.ttf", "InterLight");
                    fonts.AddFont("Inter-Regular.ttf", "Inter");
                    fonts.AddFont("Inter-SemiBold.ttf", "InterSemiBold");
                    fonts.AddFont("fa7_solid.otf", "FA7Solid");
                    fonts.AddFont("fa7-regular.otf", "FA7Regular");
                    fonts.AddFont("fa7_brands.otf", "FA7Brands");
                });

            // Configurar logging ANTES de registar serviços que precisam de ILogger
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
            builder.Services.AddSingleton<NAVIGEST.macOS.Services.Auth.IBiometricAuthService, NAVIGEST.macOS.Services.Auth.BiometricAuthService>();
            builder.Services.AddTransient<NAVIGEST.macOS.PageModels.LoginPageModel>();

            // ===== NOVO: Horas (layout — Passo 1) =====
            builder.Services.AddTransient<HorasColaboradorViewModel>();
            builder.Services.AddTransient<HoursEntryPage>();
            // (Mantive o padrão igual ao de Products: DI simples; a rota está no AppShell)

            // ===== Provider de Ícones por plataforma (mantido) =====
#if WINDOWS
            builder.Services.AddSingleton<IIconProvider, IconProvider>();
#elif ANDROID
            builder.Services.AddSingleton<IIconProvider, IconProvider>();
#elif IOS || MACCATALYST
            builder.Services.AddSingleton<IIconProvider, IconProvider>();
#endif
            // =====================================================

            return builder.Build();
        }
    }
}