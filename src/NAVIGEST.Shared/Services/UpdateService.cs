using System.Text.Json;
using NAVIGEST.Shared.Models;

namespace NAVIGEST.Shared.Services;

/// <summary>
/// Implementação de IUpdateService que obtém informações de atualização do GitHub.
/// 
/// Fluxo:
/// 1. Faz HTTP GET para obter ficheiro JSON alojado no GitHub (raw URL)
/// 2. Faz parse do JSON para AppUpdateInfo
/// 3. Retorna a informação ou null se houver erro
/// 
/// Todos os erros são tratados internamente (logging) e não lançam exceções.
/// </summary>
public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;

    // ⚠️ IMPORTANTE: Substituir isto pela URL real do teu ficheiro JSON no GitHub
    // Formato: https://raw.githubusercontent.com/{owner}/{repo}/main/{path-to-json}
    // Exemplo: https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json
    private const string GitHubJsonUrl = "https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json";

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Obtém informações de atualização do servidor.
    /// 
    /// Process:
    /// 1. HttpClient faz GET para GitHubJsonUrl com timeout
    /// 2. Lê response content
    /// 3. Faz deserialize para AppUpdateInfo usando System.Text.Json
    /// 4. Valida dados (version, minSupportedVersion não vazios)
    /// 5. Retorna objeto ou null se erro
    /// </summary>
    public async Task<AppUpdateInfo?> GetLatestAsync()
    {
        try
        {
            // Set timeout de 10 segundos
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));

            // Criar request com headers para desabilitar cache
            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubJsonUrl);
            request.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            request.Headers.Add("Pragma", "no-cache");

            // GET request com os headers de no-cache
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cts.Token);

            // Se não conseguir fazer download, retorna null
            if (!response.IsSuccessStatusCode)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateService: HTTP {response.StatusCode}");
                return null;
            }

            // Lê o conteúdo
            var content = await response.Content.ReadAsStringAsync(cts.Token);

            if (string.IsNullOrWhiteSpace(content))
            {
                System.Diagnostics.Debug.WriteLine("UpdateService: Response vazio");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"UpdateService: Raw JSON: {content}");

            // Faz deserialize do JSON
            // Mapeia camelCase do JSON para as propriedades PascalCase da classe
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var updateInfo = JsonSerializer.Deserialize<AppUpdateInfo>(content, options);

            System.Diagnostics.Debug.WriteLine($"UpdateService: Deserialized - Version={updateInfo?.Version}, MinVersion={updateInfo?.MinSupportedVersion}");

            // Valida dados críticos
            if (updateInfo == null || 
                string.IsNullOrWhiteSpace(updateInfo.Version) || 
                string.IsNullOrWhiteSpace(updateInfo.MinSupportedVersion))
            {
                System.Diagnostics.Debug.WriteLine("UpdateService: JSON inválido ou incompleto");
                return null;
            }

            return updateInfo;
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine("UpdateService: Timeout ao fazer download");
            return null;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService: Erro de rede - {ex.Message}");
            return null;
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService: Erro ao fazer parse JSON - {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateService: Erro geral - {ex.Message}");
            return null;
        }
    }
}
