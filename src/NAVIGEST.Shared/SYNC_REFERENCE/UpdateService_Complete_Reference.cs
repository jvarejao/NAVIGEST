// SYNC REFERENCE - UpdateService Complete Implementation
// SHARED REFERENCE - Last update: 2025-11-09
//
// This file contains reference code for the complete Update Service system.
// Includes: AppUpdateInfo, IUpdateService, UpdateService, VersionComparer
// Copy patterns for reference when implementing in other platforms or projects.
// See: /docs/PLATFORM_CHANGES/ANDROID_CHANGES.md - App Update Checker System
//
// DO NOT USE DIRECTLY - FOR REFERENCE ONLY

using System.Text.Json;

namespace NAVIGEST.Shared.Models;

/// <summary>
/// Model: AppUpdateInfo - Informações de atualização da app
/// JSON structure from GitHub:
/// {
///   "version": "1.0.5",
///   "minSupportedVersion": "1.0.0",
///   "downloadUrl": "https://...",
///   "notes": "Changelog..."
/// }
/// </summary>
public class AppUpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string MinSupportedVersion { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Interface: IUpdateService
/// Contrato para serviço de verificação de atualizações
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Obtém informações de atualização do servidor.
    /// Retorna AppUpdateInfo ou null se erro.
    /// </summary>
    Task<AppUpdateInfo?> GetLatestAsync();
}

/// <summary>
/// Implementation: UpdateService
/// Obtém JSON do GitHub e faz parse para AppUpdateInfo
/// 
/// Uso:
/// 1. Registar em MauiProgram: 
///    builder.Services.AddSingleton<HttpClient>();
///    builder.Services.AddSingleton<IUpdateService, UpdateService>();
/// 
/// 2. Injetar na página:
///    private readonly IUpdateService _updateService;
///    
/// 3. Usar no OnAppearing:
///    var info = await _updateService.GetLatestAsync();
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    // ⚠️ CHANGE THIS: Usar teu repositório GitHub
    private const string GitHubJsonUrl = "https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json";

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AppUpdateInfo?> GetLatestAsync()
    {
        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync(GitHubJsonUrl, HttpCompletionOption.ResponseContentRead, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateService: HTTP {response.StatusCode}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (string.IsNullOrWhiteSpace(content))
            {
                System.Diagnostics.Debug.WriteLine("UpdateService: Response vazio");
                return null;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var updateInfo = JsonSerializer.Deserialize<AppUpdateInfo>(content, options);

            if (updateInfo == null || 
                string.IsNullOrWhiteSpace(updateInfo.Version) || 
                string.IsNullOrWhiteSpace(updateInfo.MinSupportedVersion))
            {
                System.Diagnostics.Debug.WriteLine("UpdateService: JSON inválido");
                return null;
            }

            return updateInfo;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("UpdateService: Timeout");
            return null;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService: HTTP error - {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService: JSON error - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService: Error - {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Helper: VersionComparer
/// Comparação semântica de versões
/// 
/// Exemplos:
/// IsLessThan("1.0.9", "1.0.10")  → true
/// IsLessThan("v1.0.5", "1.0.6")  → true (ignora "v")
/// IsLessThan("1.0.5-beta", "1.0.5") → true (ignora sufixo)
/// IsLessThan("1.0.0", "1.0.0")   → false
/// </summary>
public static class VersionComparer
{
    public static bool IsLessThan(string versionA, string versionB)
    {
        try
        {
            var cleanA = ExtractVersionNumber(versionA);
            var cleanB = ExtractVersionNumber(versionB);

            if (!System.Version.TryParse(cleanA, out var parsedA))
                return false;

            if (!System.Version.TryParse(cleanB, out var parsedB))
                return false;

            return parsedA < parsedB;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsLessThanOrEqual(string versionA, string versionB)
    {
        try
        {
            var cleanA = ExtractVersionNumber(versionA);
            var cleanB = ExtractVersionNumber(versionB);

            if (!System.Version.TryParse(cleanA, out var parsedA))
                return false;

            if (!System.Version.TryParse(cleanB, out var parsedB))
                return false;

            return parsedA <= parsedB;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extrai números da versão: "1.0.5" ← "v1.0.5-beta+build"
    /// </summary>
    private static string ExtractVersionNumber(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return "0.0.0";

        version = version.Trim();

        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            version = version.Substring(1);

        var endIndex = version.Length;
        for (int i = 0; i < version.Length; i++)
        {
            if (!char.IsDigit(version[i]) && version[i] != '.')
            {
                endIndex = i;
                break;
            }
        }

        var cleaned = version.Substring(0, endIndex).Trim('.');

        var parts = cleaned.Split('.');
        if (parts.Length < 3)
        {
            for (int i = parts.Length; i < 3; i++)
            {
                cleaned += ".0";
            }
        }

        return cleaned;
    }
}

/// <summary>
/// USAGE EXAMPLE: Integração numa página
/// </summary>
public class UpdateCheckExample
{
    public static async Task Example_CheckForUpdatesAsync()
    {
        // Simulated
        
        // var _updateService = ServiceHelper.GetService<IUpdateService>();
        // 
        // try
        // {
        //     var currentVersion = AppInfo.Current.VersionString;
        //     var updateInfo = await _updateService.GetLatestAsync();
        //
        //     if (updateInfo == null) return;
        //
        //     // Obrigatória?
        //     if (VersionComparer.IsLessThan(currentVersion, updateInfo.MinSupportedVersion))
        //     {
        //         await DisplayAlert("Atualização Obrigatória", "...", "Atualizar");
        //         await Launcher.Default.OpenAsync(new Uri(updateInfo.DownloadUrl));
        //         return;
        //     }
        //
        //     // Opcional?
        //     if (VersionComparer.IsLessThan(currentVersion, updateInfo.Version))
        //     {
        //         var result = await DisplayAlert("Nova Versão", "...", "Atualizar", "Depois");
        //         if (result)
        //         {
        //             await Launcher.Default.OpenAsync(new Uri(updateInfo.DownloadUrl));
        //         }
        //     }
        // }
        // catch (Exception ex)
        // {
        //     GlobalErro.TratarErro(ex, mostrarAlerta: false);
        // }
    }
}
