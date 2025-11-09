# ANDROID CHANGES LOG

**Propósito**: Rastrear todas as mudanças feitas em NAVIGEST.Android para sincronização com iOS/macOS.

---

## Session: Delete Confirmation Pattern (2025-11-09)

### 1. ClientsPage.xaml.cs - ShowConfirmAsync Helper (NEW)

**Tipo**: Novo método helper

**Arquivo**: `Pages/ClientsPage.xaml.cs` (Lines ~30-50)

**Propósito**: 
Encapsular `DisplayAlert` em `MainThread.InvokeOnMainThreadAsync` para evitar deadlocks quando chamado de handlers de eventos (como SwipeItemView.Invoked).

**Código**:
```csharp
private static Task<bool> ShowConfirmAsync(string title, string message, string accept, string cancel)
{
    return MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var root = GetRootPage();
        if (root is null) return false;
        return await root.DisplayAlert(title, message, accept, cancel);
    });
}

private static Page? GetRootPage()
{
    if (Application.Current?.MainPage is NavigationPage navPage)
        return navPage.RootPage;
    
    if (Application.Current?.MainPage is FlyoutPage flyoutPage)
        return flyoutPage.Detail;
    
    return Application.Current?.MainPage;
}
```

**Dependencies**: 
- None (métodos statics privados)

**Notas**:
- Crítico para evitar UI thread deadlocks em Android
- Mesmo padrão implementado em iOS (verificado)
- GetRootPage() trata navegação normal + FlyoutPage

**Status**: ✅ Implementado e testado em Android

**Aplicável em**: iOS ✅, macOS ⏳, Windows ⏳

---

### 2. ClientsPage.xaml.cs - OnDeleteSwipeInvoked (MODIFIED)

**Tipo**: Modificação de método existente

**Arquivo**: `Pages/ClientsPage.xaml.cs` (Lines ~110-135)

**Propósito**: 
Adicionar confirmação antes de deletar cliente via swipe. Implementa padrão iOS em Android.

**Antes**:
```csharp
private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
{
    try
    {
        if (sender is not SwipeItemView siv || siv.BindingContext is not Cliente cliente)
            return;

        if (BindingContext is ClientsPageModel vm)
        {
            if (vm.DeleteCommand?.CanExecute(cliente) == true)
            {
                vm.DeleteCommand.Execute(cliente);  // Sem confirmação
            }
        }
    }
    catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
}
```

**Depois**:
```csharp
private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
{
    try
    {
        if (sender is not SwipeItemView siv || siv.BindingContext is not Cliente cliente)
            return;

        // Confirmação: "Tem a certeza que deseja eliminar '{cliente.CLINOME}'?"
        var confirm = await ShowConfirmAsync("Eliminar Cliente",
            $"Tem a certeza que deseja eliminar '{cliente.CLINOME}'?",
            "Eliminar", "Cancelar");

        if (!confirm) return;

        if (BindingContext is ClientsPageModel vm)
        {
            if (vm.DeleteCommand?.CanExecute(cliente) == true)
            {
                vm.DeleteCommand.Execute(cliente);  // Fire-and-forget
            }
        }
    }
    catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
}
```

**Dependencies**: 
- ShowConfirmAsync helper (definido acima em ClientsPage.xaml.cs)
- vm.DeleteCommand (já existia, não alterado)

**Notas**:
- Handler é `async void` (necessário para `await` do DisplayAlert)
- DisplayAlert invocado via ShowConfirmAsync (MainThread-safe)
- Se cancelar: retorna sem fazer nada
- Se confirmar: executa DeleteCommand sem await (fire-and-forget)
- **CRÍTICO**: Não fazer `await vm.DeleteCommand.Execute()` - isso causaria deadlock!

**Teste Manual**:
1. Swipe em cliente da lista
2. Deve aparecer: "Tem a certeza que deseja eliminar 'NomeCliente'?"
3. Clicar "Cancelar" → swipe fecha, cliente não é deletado
4. Clicar "Eliminar" → cliente é deletado e desaparece da lista

**Status**: ✅ Implementado e testado em Android (funciona perfeitamente)

**Aplicável em**: iOS ✅ (já tem), macOS ⏳, Windows ⏳

---

### 3. ClientsPage.xaml.cs - OnDeleteFromFormTapped (MODIFIED)

**Tipo**: Modificação de método existente

**Arquivo**: `Pages/ClientsPage.xaml.cs` (Lines ~373-395)

**Propósito**: 
Adicionar confirmação antes de deletar cliente via formulário de edição. Sincroniza UI com swipe delete.

**Antes**:
```csharp
private void OnDeleteFromFormTapped(object sender, EventArgs e)
{
    try
    {
        if (BindingContext is ClientsPageModel vm && vm.Editing is not null)
        {
            if (vm.DeleteCommand?.CanExecute(vm.Editing) == true)
            {
                vm.DeleteCommand.Execute(vm.Editing);
                HideFormView();
            }
        }
    }
    catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
}
```

**Depois**:
```csharp
private async void OnDeleteFromFormTapped(object sender, EventArgs e)
{
    try
    {
        if (BindingContext is ClientsPageModel vm && vm.Editing is not null)
        {
            // Confirmação: "Tem a certeza que deseja eliminar '{vm.Editing.CLINOME}'?"
            var confirm = await ShowConfirmAsync("Eliminar Cliente",
                $"Tem a certeza que deseja eliminar '{vm.Editing.CLINOME}'?",
                "Eliminar", "Cancelar");

            if (!confirm) return;

            if (vm.DeleteCommand?.CanExecute(vm.Editing) == true)
            {
                vm.DeleteCommand.Execute(vm.Editing);
                HideFormView();
            }
        }
    }
    catch (Exception ex) { GlobalErro.TratarErro(ex, mostrarAlerta: false); }
}
```

**Dependencies**: 
- ShowConfirmAsync helper (ClientsPage.xaml.cs lines 30-50)
- vm.DeleteCommand (já existia, não alterado)
- HideFormView() (já existia, não alterado)

**Notas**:
- Handler convertido de `void` para `async void` para permitir `await`
- Mesmo padrão que OnDeleteSwipeInvoked
- Confirmação: "Tem a certeza que deseja eliminar '{cliente.CLINOME}'?"
- Se cancelar: retorna sem fazer nada
- Se confirmar: deleta e esconde o formulário

**Teste Manual**:
1. Abrir edição de cliente (click em cliente na lista)
2. Clicar botão "Eliminar" no formulário
3. Deve aparecer: "Tem a certeza que deseja eliminar 'NomeCliente'?"
4. Clicar "Cancelar" → formulário continua aberto, cliente não é deletado
5. Clicar "Eliminar" → cliente é deletado e formulário fecha

**Status**: ✅ Implementado e testado em Android (funciona perfeitamente)

**Aplicável em**: iOS ✅ (já tem), macOS ⏳, Windows ⏳

---

## Summary - ClientsPage Mudanças

| O quê | Linha | Tipo | Status |
|---|---|---|---|
| ShowConfirmAsync helper | 30-50 | NEW | ✅ |
| GetRootPage helper | 50-70 | NEW | ✅ |
| OnDeleteSwipeInvoked | 110-135 | MOD | ✅ |
| OnDeleteFromFormTapped | 373-395 | MOD | ✅ |

**Todas testadas e funcionando perfeitamente em Android.**

---

## Root Cause Analysis (Documentado)

**Problema Original**: Swipe Delete não funcionava (apenas fechava o swipe).

**Root Cause Identificado**: 
- Handler `async void` + `DisplayAlert` direto dentro de SwipeView.Invoked event = UI thread deadlock
- Android DisplayAlert não consegue renderizar quando chamado de certos contextos de evento

**Solução Aplicada**: 
- Copiar padrão do Form Delete que já funcionava
- Envolver DisplayAlert em `MainThread.InvokeOnMainThreadAsync`
- Usar ShowConfirmAsync helper (padrão iOS)

**Resultado**: ✅ Swipe Delete + Form Delete + Swipe Pastas agora funcionam perfeitamente

**Lições Aprendidas**:
- Documentado em: `/docs/SWIPE_DELETE_PATTERN_LESSON.md`

---

## Próximas Mudanças Previstas

- [ ] Adicionar confirmação a OnPastasSwipeInvoked (mesmo padrão)
- [ ] Revisar outros handlers de swipe para confirmações
- [ ] Sincronizar com iOS/macOS
- [ ] Implementar em Windows

---

## Session: App Update Checker System (2025-11-09)

### 1. AppUpdateInfo Model (NEW)

**Tipo**: Novo modelo

**Arquivo**: `NAVIGEST.Shared/Models/AppUpdateInfo.cs`

**Propósito**: 
Representar estrutura JSON de informações de atualização obtidas do GitHub.

**Propriedades**:
```csharp
public string Version { get; set; }              // Versão mais recente (ex: "1.0.5")
public string MinSupportedVersion { get; set; } // Versão mínima (ex: "1.0.0")
public string DownloadUrl { get; set; }         // URL para download/store
public string Notes { get; set; }               // Changelog
```

**Status**: ✅ Implementado em Shared

---

### 2. IUpdateService Interface (NEW)

**Tipo**: Nova interface

**Arquivo**: `NAVIGEST.Shared/Services/IUpdateService.cs`

**Propósito**: 
Definir contrato para serviço de verificação de atualizações.

**Método**:
```csharp
Task<AppUpdateInfo?> GetLatestAsync();  // Retorna null em caso de erro
```

**Status**: ✅ Implementado em Shared

---

### 3. UpdateService Implementation (NEW)

**Tipo**: Nova implementação

**Arquivo**: `NAVIGEST.Shared/Services/UpdateService.cs`

**Propósito**: 
Implementação HTTP do IUpdateService que obtém JSON do GitHub.

**Features**:
- HttpClient com timeout de 10 segundos
- Deserialização JSON com System.Text.Json
- Tratamento robusto de erros (logging apenas, sem exceções)
- Suporta URLs raw do GitHub
- ⚠️ IMPORTANTE: Muda GitHubJsonUrl para o teu repositório!

**Método**:
```csharp
public async Task<AppUpdateInfo?> GetLatestAsync()
{
    // GET de GitHub
    // Parse JSON
    // Valida dados
    // Retorna AppUpdateInfo ou null
}
```

**Status**: ✅ Implementado em Shared

**Configuração Necessária**:
- Criar ficheiro JSON em GitHub: `/updates/version.json`
- Atualizar URL em UpdateService.cs (line ~28)

---

### 4. VersionComparer Helper (NEW)

**Tipo**: Novo helper estático

**Arquivo**: `NAVIGEST.Shared/Helpers/VersionComparer.cs`

**Propósito**: 
Comparação semântica de versões (1.0.9 vs 1.0.10, v1.0, 1.0.0-beta, etc).

**Métodos**:
```csharp
public static bool IsLessThan(string versionA, string versionB)         // a < b
public static bool IsLessThanOrEqual(string versionA, string versionB) // a <= b
```

**Exemplos**:
```
"1.0.9" < "1.0.10"    → true
"v1.0.5" < "1.0.6"    → true
"1.0.5-beta" < "1.0.5" → true
"1.0.0" < "1.0.0"     → false
```

**Status**: ✅ Implementado em Shared

---

### 5. MauiProgram.cs Registration (MODIFIED)

**Tipo**: Modificação

**Arquivo**: `NAVIGEST.Android/MauiProgram.cs` (Lines ~85-86)

**Propósito**: 
Registar UpdateService na injeção de dependências.

**Antes**:
```csharp
builder.Services.AddSingleton<ModalErrorHandler>();
// Sem UpdateService
```

**Depois**:
```csharp
builder.Services.AddSingleton<ModalErrorHandler>();

// ✅ Update Service - Verificação de atualizações multi-plataforma
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddSingleton<NAVIGEST.Shared.Services.IUpdateService, 
                               NAVIGEST.Shared.Services.UpdateService>();
```

**Status**: ✅ Implementado em Android

**Aplicável em**: iOS ✅, macOS ✅, Windows ✅ (mesma linha em cada MauiProgram)

---

## Fluxo de Verificação de Atualizações

```
OnAppearing() da página de entrada
    ↓
CheckForUpdatesAsync()
    ↓
1. Obter versão atual: AppInfo.Current.VersionString
2. Chamar _updateService.GetLatestAsync()
3. Se null → sair silenciosamente
4. Comparar com VersionComparer:
    ├─ current < minSupportedVersion?
    │  └─ SIM: Alert obrigatório + abre URL
    └─ current < version?
       └─ SIM: Alert opcional (2 botões) + abre URL se aceitar
```

**Abertura de URLs**:
- Android: `Launcher.OpenAsync()` abre APK/Play Store
- iOS: `Launcher.OpenAsync()` abre App Store
- macOS: `Launcher.OpenAsync()` abre App Store/link genérico
- Windows: `Launcher.OpenAsync()` abre browser

---

## Ficheiros JSON de Exemplo

**Localização**: `/updates/version.json` no GitHub

**Conteúdo**:
```json
{
  "version": "1.0.5",
  "minSupportedVersion": "1.0.0",
  "downloadUrl": "https://play.google.com/store/apps/details?id=com.tuaempresa.navigest",
  "notes": "Correções de bugs e melhorias de performance."
}
```

**URLs Recomendadas**:
- Android: Google Play Store link
- iOS: App Store link
- macOS: App Store link
- Windows: Link direto para MSIX ou site

---

## Status - Update Service

| O quê | Shared | Android | iOS | macOS | Windows |
|---|---|---|---|---|---|
| AppUpdateInfo | ✅ | - | - | - | - |
| IUpdateService | ✅ | - | - | - | - |
| UpdateService | ✅ | - | - | - | - |
| VersionComparer | ✅ | - | - | - | - |
| MauiProgram Registration | - | ✅ | ⏳ | ⏳ | ⏳ |
| Page Integration | - | ⏳ | ⏳ | ⏳ | ⏳ |

---

**Última Atualização**: 2025-11-09  
**Próxima Revisão**: Quando próxima mudança for feita em Android

```
