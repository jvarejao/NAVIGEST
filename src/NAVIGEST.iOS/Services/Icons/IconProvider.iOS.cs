#if IOS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;   // FileSystem.OpenAppPackageFileAsync
using Microsoft.Maui.Graphics;  // Colors

namespace NAVIGEST.iOS.Services.Icons
{
    public sealed class IconProvider : IIconProvider
    {
        public ImageSource Get(string logicalName, double size = 24)
        {
            try
            {
                foreach (var path in GetiOSCandidates(logicalName))
                {
                    if (PackageFileExists(path))
                        return ImageSource.FromFile(path);
                }

                // Fallback FA7
                return FaFallback(logicalName, size);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconProvider/iOS] {ex}");
                return FaFallback(logicalName, size);
            }
        }

        private static IEnumerable<string> GetiOSCandidates(string name)
        {
            // Prioridade: específicos iOS → subpasta iOS → asset comum
            yield return $"{name}.ios.png";
            yield return $"Resources/Images/iOS/{name}.png";
            yield return $"iOS/{name}.png";
            yield return $"Resources/Images/{name}.png"; // asset partilhado (opcional)
        }

        private static bool PackageFileExists(string relativePath)
        {
            try
            {
                using var _ = FileSystem.OpenAppPackageFileAsync(relativePath)
                                         .GetAwaiter().GetResult();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static ImageSource FaFallback(string name, double size) => new FontImageSource
        {
            Glyph = GetFaGlyph(name),
            FontFamily = "FA7Solid",
            Size = size,
            Color = Colors.White
        };

        private static string GetFaGlyph(string name) => name switch
        {
            "settings" => "\uf013",
            "edit"     => "\uf044",
            "delete"   => "\uf1f8",
            "add"      => "\uf067",
            "save"     => "\uf0c7",
            "search"   => "\uf002",
            "back"     => "\uf060",
            _          => "\uf128"
        };
    }
}
#endif
