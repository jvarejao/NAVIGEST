#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;   // FileSystem.OpenAppPackageFileAsync
using Microsoft.Maui.Graphics;  // Colors

namespace NAVIGEST.macOS.Services.Icons
{
    public sealed class IconProvider : IIconProvider
    {
        public ImageSource Get(string logicalName, double size = 24)
        {
            try
            {
                foreach (var path in GetMacCatalystCandidates(logicalName))
                {
                    if (PackageFileExists(path))
                        return ImageSource.FromFile(path);
                }

                // Fallback FA7
                return FaFallback(logicalName, size);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconProvider/MacCatalyst] {ex}");
                return FaFallback(logicalName, size);
            }
        }

        private static IEnumerable<string> GetMacCatalystCandidates(string name)
        {
            // Prioridade: específicos MacCatalyst → subpasta iOS → asset comum
            yield return $"{name}.maccatalyst.png";
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

        private static ImageSource FaFallback(string logicalName, double size)
        {
            // Fallback: tentar usar um ícone Font Awesome 7 Solid
            // (Certifica-te de que IconExtension está integrado no markup)
            try
            {
                // Se IconExtension mapeia nomes do logicalName para FA7...
                return new FontImageSource
                {
                    FontFamily = "FA7Solid",
                    Glyph = "\uf198", // Exemplo: ícone genérico
                    Size = size,
                    Color = Colors.White
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[IconProvider/FA7Fallback] {ex}");
                // Último recurso: imagem branca vazia
                return ImageSource.FromFile("placeholder.png");
            }
        }
    }
}
#endif
