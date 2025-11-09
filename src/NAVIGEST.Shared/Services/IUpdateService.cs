using NAVIGEST.Shared.Models;

namespace NAVIGEST.Shared.Services;

/// <summary>
/// Serviço para verificar atualizações da aplicação.
/// 
/// Responsabilidades:
/// - Obter informações de atualização do servidor (ficheiro JSON no GitHub)
/// - Expor API simples: GetLatestAsync()
/// 
/// Implementação: UpdateService
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Obtém as informações de atualização mais recentes do servidor.
    /// 
    /// Returns:
    /// - AppUpdateInfo com dados válidos se conseguir fazer parse do JSON
    /// - null se houver erro de rede, parsing, ou o servidor retornar erro
    /// 
    /// Nota: Erros são logados mas não lançam exceção (usar try...catch no caller)
    /// </summary>
    Task<AppUpdateInfo?> GetLatestAsync();
}
