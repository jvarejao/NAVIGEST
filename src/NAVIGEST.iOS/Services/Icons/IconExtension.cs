using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Extensions.DependencyInjection; // GetService<T>()
using NAVIGEST.iOS.Services.Icons;             // IIconProvider

namespace NAVIGEST.iOS.Services.Icons
{
    // Uso em XAML:
    // xmlns:icons="clr-namespace:NAVIGEST.iOS.Services.Icons"
    // <Image Source="{icons:Icon Name=save, Size=22}" />
    [ContentProperty(nameof(Name))]
    public sealed class Icon : IMarkupExtension<ImageSource>
    {
        public string Name { get; set; } = "";
        public double Size { get; set; } = 24;

        public ImageSource ProvideValue(IServiceProvider serviceProvider)
        {
            // 1) Tenta via provider registado no DI (usa assets por plataforma e fallback FA7)
            var provider = Resolve<IIconProvider>();
            var src = provider?.Get(Name, Size);
            if (src != null)
                return src;

            // 2) Fallback garantido (sem DI). No Windows usar URI resm para evitar "quadrados".
#if WINDOWS
            return new FontImageSource
            {
                Glyph = "\uf128", // info
                Size = Size,
                FontFamily = "resm:NAVIGEST.iOS.Resources.Fonts.fa7_solid.otf#FA7Solid"
            };
#else
            return new FontImageSource
            {
                Glyph = "\uf128",
                FontFamily = "FA7Solid",
                Size = Size
            };
#endif
        }

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);

        private static T? Resolve<T>() where T : class
        {
            // Resolve direto do container do MAUI (evita dependência de Helpers)
            var services = Application.Current?.Handler?.MauiContext?.Services;
            return services?.GetService<T>();
        }
    }
}
