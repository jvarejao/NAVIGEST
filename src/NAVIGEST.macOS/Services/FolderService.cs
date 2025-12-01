using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using NAVIGEST.macOS.Models;
using System.Linq;

namespace NAVIGEST.macOS.Services
{
    public static class FolderService
    {
        /// <summary>
        /// Tenta criar a estrutura de pastas para o cliente.
        /// Retorna (sucesso, mensagem).
        /// </summary>
        public static async Task<(bool Success, string Message)> CreateClientFoldersAsync(Cliente cliente)
        {
            try
            {
                var setup = await DatabaseService.GetSetupAsync();
                if (setup == null)
                    return (false, "Configuração (SETUP) não encontrada na base de dados.");

                var rootPath = ResolvePath(setup.CaminhoServidor);
                if (string.IsNullOrWhiteSpace(rootPath))
                    return (false, "Caminho do servidor não configurado ou inválido.");

                if (!Directory.Exists(rootPath))
                {
                    // Tenta montar automaticamente se for caminho de rede
                    TryMountServer(setup.CaminhoServidor);
                    return (false, $"A pasta raiz não está acessível: {rootPath}.\nO sistema tentou ligar ao servidor. Por favor, autentique-se se necessário e tente novamente.");
                }

                // Nome da pasta do cliente: "CLINOME" (limpo de caracteres inválidos)
                // Alterado a pedido do utilizador para usar apenas o NOME, pois já existem muitas pastas assim.
                var folderName = SanitizeFileName(cliente.CLINOME ?? "Cliente");
                var clientPath = Path.Combine(rootPath, folderName);

                bool folderExisted = Directory.Exists(clientPath);

                // Criar pasta mãe
                if (!folderExisted)
                {
                    Directory.CreateDirectory(clientPath);
                }

                // Criar subpastas
                var subfolders = setup.GetSubfolders();
                foreach (var sub in subfolders)
                {
                    var subPath = Path.Combine(clientPath, sub.Trim());
                    if (!Directory.Exists(subPath))
                    {
                        Directory.CreateDirectory(subPath);
                    }
                }

                if (folderExisted)
                {
                    return (true, $"A pasta '{folderName}' já existia. A estrutura foi verificada.");
                }

                return (true, clientPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FolderService] Erro: {ex.Message}");
                return (false, $"Erro ao criar pastas: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre a pasta do cliente no Finder.
        /// </summary>
        public static async Task OpenClientFolderAsync(Cliente cliente)
        {
            try
            {
                var setup = await DatabaseService.GetSetupAsync();
                if (setup == null || string.IsNullOrWhiteSpace(setup.CaminhoServidor))
                    return;

                var rootPath = ResolvePath(setup.CaminhoServidor);
                if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                {
                    // Tenta abrir a raiz se a do cliente falhar, ou avisa
                    return;
                }

                // Tenta encontrar a pasta do cliente. 
                // Prioridade 1: Pelo Nome (Novo padrão solicitado)
                var folderName = SanitizeFileName(cliente.CLINOME ?? "");
                var clientFolder = Path.Combine(rootPath, folderName);

                if (!Directory.Exists(clientFolder))
                {
                    // Prioridade 2: Pelo Código (Padrão antigo ou misto)
                    // Procura qualquer pasta que comece pelo código do cliente
                    var found = Directory.GetDirectories(rootPath, $"{cliente.CLICODIGO}*").FirstOrDefault();
                    if (!string.IsNullOrEmpty(found))
                    {
                        clientFolder = found;
                    }
                }

                if (Directory.Exists(clientFolder))
                {
                    Process.Start("open", $"\"{clientFolder}\"");
                }
                else
                {
                    // Se não existe, abre a raiz para o utilizador procurar
                    Process.Start("open", $"\"{rootPath}\"");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FolderService] Erro ao abrir pasta: {ex.Message}");
            }
        }

        private static string? ResolvePath(string? dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath)) return null;

            // Lógica para converter caminhos Windows (\\IP\Share) para macOS (/Volumes/Share)
            // Ex: \\100.81.152.95\yah\TRABALHOS... -> /Volumes/yah/TRABALHOS...
            
            if (dbPath.StartsWith(@"\\"))
            {
                var parts = dbPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    // parts[0] = IP (100.81.152.95)
                    // parts[1] = Share (yah)
                    // parts[2...] = Resto do caminho
                    
                    var shareName = parts[1];
                    var volumePath = $"/Volumes/{shareName}";

                    // Mesmo que não exista, devolvemos o caminho convertido para tentar usar
                    // ou para informar o utilizador onde deveria estar.
                    var relativePath = Path.Combine(parts.Skip(2).ToArray());
                    return Path.Combine(volumePath, relativePath);
                }
            }

            return dbPath;
        }

        private static void TryMountServer(string? dbPath)
        {
            if (string.IsNullOrWhiteSpace(dbPath) || !dbPath.StartsWith(@"\\")) return;

            try
            {
                // Converte \\IP\Share\Path para smb://IP/Share/Path
                var parts = dbPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var ip = parts[0];
                    var share = parts[1];
                    var url = $"smb://{ip}/{share}";
                    Process.Start("open", url);
                }
            }
            catch { /* ignore */ }
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var newName = string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
            return newName;
        }
    }
}
