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
                    TryMountServer(setup);
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
                {
                    await AppShell.DisplayToastAsync("Servidor de ficheiros não configurado.", ToastTipo.Erro);
                    return;
                }

                var rootPath = ResolvePath(setup.CaminhoServidor);
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    await AppShell.DisplayToastAsync("Caminho do servidor inválido.", ToastTipo.Erro);
                    return;
                }

                if (!Directory.Exists(rootPath))
                {
                    // Tenta montar automaticamente se for caminho de rede
                    TryMountServer(setup);
                    
                    // Dá um pequeno tempo para montar
                    await Task.Delay(2000);

                    if (!Directory.Exists(rootPath))
                    {
                        await AppShell.DisplayToastAsync("Servidor de ficheiros inacessível. Verifique a ligação.", ToastTipo.Erro);
                        return;
                    }
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
                    await AppShell.DisplayToastAsync($"Pasta do cliente não encontrada: {folderName}", ToastTipo.Aviso);
                    // Não abre a raiz para não confundir, ou pergunta se quer abrir a raiz?
                    // O utilizador disse: "se a pasta tiver um visto verde, eu clico e nbao acontece nada... tem de dar um aviso"
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FolderService] Erro ao abrir pasta: {ex.Message}");
                await AppShell.DisplayToastAsync("Erro ao abrir pasta.", ToastTipo.Erro);
            }
        }

        public static async Task<bool> TestConnectionAsync()
        {
            try
            {
                var setup = await DatabaseService.GetSetupAsync();
                if (setup == null || string.IsNullOrWhiteSpace(setup.CaminhoServidor)) return false;
                var rootPath = ResolvePath(setup.CaminhoServidor);
                
                if (!string.IsNullOrWhiteSpace(rootPath) && Directory.Exists(rootPath))
                {
                    return true;
                }

                // Se não existe, tenta montar
                TryMountServer(setup);
                
                // Espera um pouco para o sistema montar
                await Task.Delay(3000);

                return !string.IsNullOrWhiteSpace(rootPath) && Directory.Exists(rootPath);
            }
            catch { return false; }
        }

        public static string? ResolvePath(string? dbPath)
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

        private static void TryMountServer(Setup setup)
        {
            TryMountPath(setup.CaminhoServidor, setup);
        }

        private static void TryMountPath(string? path, Setup setup)
        {
             if (string.IsNullOrWhiteSpace(path) || !path.StartsWith(@"\\")) return;

            try
            {
                var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    var ip = parts[0];
                    var share = parts[1];
                    
                    string url;
                    if (!string.IsNullOrWhiteSpace(setup.ServerUser) && !string.IsNullOrWhiteSpace(setup.ServerPassword))
                    {
                        var user = Uri.EscapeDataString(setup.ServerUser);
                        var pass = Uri.EscapeDataString(setup.ServerPassword);
                        url = $"smb://{user}:{pass}@{ip}/{share}";
                    }
                    else
                    {
                        url = $"smb://{ip}/{share}";
                    }

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

        /// <summary>
        /// Tenta criar a estrutura de pastas para o produto.
        /// Retorna (sucesso, mensagem).
        /// </summary>
        public static async Task<(bool Success, string Message)> CreateProductFoldersAsync(Product product)
        {
            try
            {
                var setup = await DatabaseService.GetSetupAsync();
                if (setup == null)
                    return (false, "Configuração (SETUP) não encontrada na base de dados.");

                // Usa CaminhoServidor2 para produtos, ou CaminhoServidor se o 2 não estiver definido?
                // Assumindo CaminhoServidor2 conforme estrutura do Setup.
                var rootPathRaw = !string.IsNullOrWhiteSpace(setup.CaminhoServidor2) ? setup.CaminhoServidor2 : setup.CaminhoServidor;
                
                var rootPath = ResolvePath(rootPathRaw);
                if (string.IsNullOrWhiteSpace(rootPath))
                    return (false, "Caminho do servidor (Produtos) não configurado ou inválido.");

                if (!Directory.Exists(rootPath))
                {
                    // Tenta montar automaticamente
                    // Nota: TryMountServer usa setup.CaminhoServidor. Precisamos adaptar se for o 2.
                    // Mas como o TryMountServer recebe o Setup object, podemos ajustar lá ou criar um overload.
                    // Por agora, vamos tentar montar o path específico.
                    TryMountPath(rootPathRaw, setup);
                    return (false, $"A pasta raiz não está acessível: {rootPath}.\nO sistema tentou ligar ao servidor. Por favor, autentique-se se necessário e tente novamente.");
                }

                // Nome da pasta do produto: "PRODCODIGO - PRODNOME" ou apenas "PRODNOME"?
                // Em clientes mudou para apenas NOME. Em produtos, geralmente o código é importante.
                // Vamos usar "CODIGO - NOME" para garantir unicidade, ou seguir o padrão do cliente se for pedido.
                // Por defeito: "CODIGO - NOME"
                var folderName = SanitizeFileName($"{product.PRODCODIGO} - {product.PRODNOME}");
                var productPath = Path.Combine(rootPath, folderName);

                bool folderExisted = Directory.Exists(productPath);

                // Criar pasta mãe
                if (!folderExisted)
                {
                    Directory.CreateDirectory(productPath);
                }

                // Criar subpastas
                var subfolders = setup.GetProductSubfolders();
                foreach (var sub in subfolders)
                {
                    var subPath = Path.Combine(productPath, sub.Trim());
                    if (!Directory.Exists(subPath))
                    {
                        Directory.CreateDirectory(subPath);
                    }
                }

                if (folderExisted)
                {
                    return (true, $"A pasta '{folderName}' já existia. A estrutura foi verificada.");
                }

                return (true, productPath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FolderService] Erro: {ex.Message}");
                return (false, $"Erro ao criar pastas: {ex.Message}");
            }
        }

        /// <summary>
        /// Abre a pasta do produto no Finder.
        /// </summary>
        public static async Task OpenProductFolderAsync(Product product)
        {
            try
            {
                var setup = await DatabaseService.GetSetupAsync();
                if (setup == null)
                {
                    await AppShell.DisplayToastAsync("Configuração não encontrada.", ToastTipo.Erro);
                    return;
                }

                var rootPathRaw = !string.IsNullOrWhiteSpace(setup.CaminhoServidor2) ? setup.CaminhoServidor2 : setup.CaminhoServidor;
                var rootPath = ResolvePath(rootPathRaw);

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    await AppShell.DisplayToastAsync("Caminho do servidor (Produtos) inválido.", ToastTipo.Erro);
                    return;
                }

                if (!Directory.Exists(rootPath))
                {
                    TryMountPath(rootPathRaw, setup);
                    await Task.Delay(2000);

                    if (!Directory.Exists(rootPath))
                    {
                        await AppShell.DisplayToastAsync("Servidor de ficheiros inacessível.", ToastTipo.Erro);
                        return;
                    }
                }

                // Tenta encontrar a pasta
                var folderName = SanitizeFileName($"{product.PRODCODIGO} - {product.PRODNOME}");
                var productFolder = Path.Combine(rootPath, folderName);

                if (!Directory.Exists(productFolder))
                {
                    // Tenta encontrar apenas pelo código se o nome mudou
                    var found = Directory.GetDirectories(rootPath, $"{product.PRODCODIGO}*").FirstOrDefault();
                    if (!string.IsNullOrEmpty(found))
                    {
                        productFolder = found;
                    }
                }

                if (Directory.Exists(productFolder))
                {
                    Process.Start("open", $"\"{productFolder}\"");
                }
                else
                {
                    await AppShell.DisplayToastAsync($"Pasta do produto não encontrada: {folderName}", ToastTipo.Aviso);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FolderService] Erro ao abrir pasta: {ex.Message}");
                await AppShell.DisplayToastAsync("Erro ao abrir pasta.", ToastTipo.Erro);
            }
        }
    }
}
