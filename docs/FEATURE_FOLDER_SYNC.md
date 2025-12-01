# Funcionalidade: Sincronização de Pastas de Clientes (Cross-Platform)

Este documento descreve a lógica implementada para a criação e sincronização de pastas de clientes e como deve ser portada para outras plataformas (Windows, Android, iOS).

## 1. Lógica Geral (Implementada no macOS)

A funcionalidade reside principalmente em `FolderService.cs` e `ClientsPageModel.cs`.

### Fluxo:
1.  **Verificação**: Ao abrir a lista de clientes, a App verifica se existem clientes com `PastasSincronizadas = 0` (excluindo anulados).
2.  **Criação**:
    *   Lê a configuração da tabela `SETUP` (`CaminhoServidor`, `SERV1PASTA1`, etc.).
    *   Resolve o caminho do servidor para o sistema de ficheiros local.
    *   Verifica se a pasta do cliente já existe (Padrão: `NOME DO CLIENTE`).
    *   Se não existir, cria a pasta e as subpastas definidas.
    *   Se já existir, apenas valida a estrutura.
3.  **Persistência**: Atualiza a flag `PastasSincronizadas` na base de dados e na UI.

## 2. Adaptação por Plataforma

### macOS (Implementado)
*   **Caminho**: Converte caminhos UNC (`\\IP\Share`) para caminhos de montagem (`/Volumes/Share`).
*   **Acesso**: Depende do utilizador ter montado o volume no Finder (SMB).
*   **Código**: `NAVIGEST.macOS/Services/FolderService.cs`.

### Windows (Próximo Passo)
*   **Caminho**: Suporta nativamente caminhos UNC (`\\IP\Share`).
*   **Adaptação**:
    *   A lógica de `ResolvePath` deve ser simplificada para retornar o caminho original se começar por `\\`.
    *   `System.IO` funciona nativamente com permissões de rede do Windows.

### Android & iOS (Desafio Técnico)
*   **Problema**: Estes sistemas operativos **não** montam partilhas SMB no sistema de ficheiros global de forma transparente para a App (como o `/Volumes` do Mac ou letras de unidade do Windows). O `System.IO.Directory.Exists` vai falhar para caminhos de rede.
*   **Solução Recomendada**:
    1.  **Abordagem Nativa (Difícil)**: Tentar usar "File Providers" do OS.
    2.  **Biblioteca SMB (Recomendado)**: Usar uma biblioteca como `SMBLibrary` ou `SharpCifs.Std` para comunicar diretamente com o servidor NAS via protocolo SMB, sem depender do sistema de ficheiros do OS.
    *   *Nota*: Isto exigirá refazer o `FolderService` nestas plataformas para usar a biblioteca em vez de `System.IO`.

## 3. Ficheiros Modificados (Referência para Porting)

Ao aplicar isto nos outros projetos (`NAVIGEST.Android`, `NAVIGEST.iOS`, etc.), consultar:

1.  **`Models/Cliente.cs`**:
    *   Adicionada interface `INotifyPropertyChanged` para atualização imediata da UI (Visto Verde).

2.  **`Services/FolderService.cs`**:
    *   Lógica de `CreateClientFoldersAsync`.
    *   Lógica de `SanitizeFileName`.
    *   **Atenção**: `ResolvePath` e `TryMountServer` são específicos de macOS/Unix.

3.  **`PageModels/ClientsPageModel.cs`**:
    *   Método `CheckForUnsyncedFoldersAsync` (Sincronização em massa).
    *   Atualização do método `OnPastasAsync`.
    *   Melhoria no `SetPastasSincronizadas` para atualizar `Filtered` collection.

4.  **`Services/DatabaseService.cs`**:
    *   Método `EnsureClientePastasAsync`.
    *   Método `GetSetupAsync`.
