namespace NAVIGEST.Shared.Models;

/// <summary>
/// Informações de atualização da aplicação obtidas do servidor.
/// 
/// Corresponde à estrutura JSON:
/// {
///   "version": "1.0.5",
///   "minSupportedVersion": "1.0.0",
///   "downloadUrl": "https://link-para-download-ou-store",
///   "notes": "Correções e melhorias."
/// }
/// </summary>
public class AppUpdateInfo
{
    /// <summary>
    /// Versão mais recente disponível (ex: "1.0.5", "2.1.0", etc).
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Versão mínima suportada (ex: "1.0.0").
    /// Se a versão atual for menor que esta, a atualização é OBRIGATÓRIA.
    /// </summary>
    public string MinSupportedVersion { get; set; } = string.Empty;

    /// <summary>
    /// URL para download/instalação:
    /// - Android: link direto para APK ou Google Play Store
    /// - iOS: link App Store
    /// - macOS: link App Store ou link direto para DMG
    /// - Windows: link direto para MSIX ou site de downloads
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Notas de atualização (changelog resumido).
    /// Será mostrado ao utilizador.
    /// </summary>
    public string Notes { get; set; } = string.Empty;
}
