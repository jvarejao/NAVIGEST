# 📱 Update Service - Complete Implementation Guide

**MAUI .NET 9 - Multi-Platform Update Checker (Android, iOS, macOS, Windows)**

---

## 📋 Summary

Sistema completo de verificação de atualizações que funciona em todas as plataformas MAUI:

✅ **Core Service** em `NAVIGEST.Shared` (código comum a todas as plataformas)  
✅ **Modelo** de informações de atualização  
✅ **Comparador** de versões semânticas  
✅ **HttpClient** para obter JSON do GitHub  
✅ **Multi-plataforma** - mesmo código funciona em Android, iOS, macOS, Windows  
✅ **Padrão DI** - registado no MauiProgram de cada plataforma  

---

## 📁 Ficheiros Criados

| Ficheiro | Localização | Propósito |
|---|---|---|
| `AppUpdateInfo.cs` | `NAVIGEST.Shared/Models/` | Modelo de dados |
| `IUpdateService.cs` | `NAVIGEST.Shared/Services/` | Interface |
| `UpdateService.cs` | `NAVIGEST.Shared/Services/` | Implementação |
| `VersionComparer.cs` | `NAVIGEST.Shared/Helpers/` | Comparação de versões |

---

## 🔧 Configuração

### 1️⃣ Registar no MauiProgram.cs (cada plataforma)

```csharp
// Em NAVIGEST.Android/MauiProgram.cs
// Em NAVIGEST.iOS/MauiProgram.cs
// Em NAVIGEST.macOS/MauiProgram.cs
// Em NAVIGEST.Windows/MauiProgram.cs

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    builder.UseMauiApp<App>()
        // ... outras configurações ...
        ;

    // DI Services
    // ... outros serviços ...
    
    // ✅ ADD THIS:
    builder.Services.AddSingleton<HttpClient>();
    builder.Services.AddSingleton<NAVIGEST.Shared.Services.IUpdateService, 
                                   NAVIGEST.Shared.Services.UpdateService>();
    
    return builder.Build();
}
```

### 2️⃣ Configurar URL do GitHub

Em `NAVIGEST.Shared/Services/UpdateService.cs`:

```csharp
// ⚠️ MUDA ISTO:
private const string GitHubJsonUrl = "https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/updates/version.json";
```

Para:
```csharp
// Use o teu repositório e caminho real
private const string GitHubJsonUrl = "https://raw.githubusercontent.com/{owner}/{repo}/main/{path-to-json}";
```

### 3️⃣ Criar ficheiro JSON no GitHub

Cria um ficheiro no teu repositório:

```
/updates/version.json
```

Conteúdo:
```json
{
  "version": "1.0.5",
  "minSupportedVersion": "1.0.0",
  "downloadUrl": "https://play.google.com/store/apps/details?id=com.tuaempresa.navigest",
  "notes": "Correções de bugs e melhorias de performance."
}
```

**URLs recomendadas por plataforma:**
- **Android**: `https://play.google.com/store/apps/details?id=com.tuaempresa.navigest`
- **iOS**: `https://apps.apple.com/app/navigest/id123456789`
- **macOS**: `https://apps.apple.com/app/navigest/id123456789`
- **Windows**: Link direto para MSIX/EXE ou site de downloads

---

## 🎯 Fluxo de Verificação

```
┌─ App abre (MainPage.OnAppearing) ─────────────────┐
│                                                   │
│ 1. Obter versão atual: AppInfo.Current.Version  │
│ 2. Chamar UpdateService.GetLatestAsync()         │
│ 3. Comparar versões com VersionComparer          │
│                                                   │
├─→ current < minSupportedVersion?                 │
│   ├─ SIM: Atualização OBRIGATÓRIA                │
│   │   └─ Alert com 1 botão "Atualizar"          │
│   │   └─ Abre DownloadUrl                        │
│   └─ NÃO: Continua...                            │
│                                                   │
├─→ current < version?                             │
│   ├─ SIM: Atualização OPCIONAL                   │
│   │   └─ Alert com 2 botões "Atualizar"/"Depois"│
│   │   └─ Se aceitar: abre DownloadUrl            │
│   └─ NÃO: App atualizado, continua normal       │
│                                                   │
└───────────────────────────────────────────────────┘
```

---

## 💡 Exemplo de Integração

### Na tua página de entrada (ex: MainPage, SplashIntroPage, etc):

```csharp
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Helpers;

public partial class MainPage : ContentPage
{
    private readonly IUpdateService _updateService;

    public MainPage()
    {
        InitializeComponent();
        _updateService = ServiceHelper.GetService<IUpdateService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Teu código existente...
        
        // ✅ ADD THIS: Verificar atualizações em background
        _ = CheckForUpdatesAsync();
    }

    private async Task CheckForUpdatesAsync()
    {
        try
        {
            var currentVersion = AppInfo.Current.VersionString;
            var updateInfo = await _updateService.GetLatestAsync();

            if (updateInfo == null) return;

            // Atualização obrigatória?
            if (VersionComparer.IsLessThan(currentVersion, updateInfo.MinSupportedVersion))
            {
                var root = GetRootPage();
                if (root != null)
                {
                    await root.DisplayAlert(
                        "Atualização Obrigatória",
                        $"Versão mínima: {updateInfo.MinSupportedVersion}\n\n{updateInfo.Notes}",
                        "Atualizar");
                    
                    await Launcher.Default.OpenAsync(new Uri(updateInfo.DownloadUrl));
                }
                return;
            }

            // Atualização opcional?
            if (VersionComparer.IsLessThan(currentVersion, updateInfo.Version))
            {
                var root = GetRootPage();
                if (root != null)
                {
                    var result = await root.DisplayAlert(
                        "Nova Versão Disponível",
                        $"Nova versão: {updateInfo.Version}\n\n{updateInfo.Notes}",
                        "Atualizar", "Depois");
                    
                    if (result)
                    {
                        await Launcher.Default.OpenAsync(new Uri(updateInfo.DownloadUrl));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private static Page? GetRootPage()
    {
        if (Application.Current?.MainPage is NavigationPage navPage)
            return navPage.RootPage;

        if (Application.Current?.MainPage is FlyoutPage flyoutPage)
            return flyoutPage.Detail;

        return Application.Current?.MainPage;
    }
}
```

---

## ✨ Características

✅ **Comparação Semântica** - Suporta 1.0.9 vs 1.0.10, v1.0, 1.0.0-beta, etc  
✅ **Multi-Plataforma** - Mesmo código funciona em Android, iOS, macOS, Windows  
✅ **Robusto** - Todos os erros tratados, não causa crashes  
✅ **Silencioso** - Não bloqueia UI, executa em background  
✅ **Configurável** - Mudar URL e política de atualização é trivial  
✅ **Padrão MAUI** - Usa DI, HttpClient factory, boas práticas  

---

## 🔒 Tratamento de Erros

Todos os erros são tratados gracefully:

- **Timeout de rede** → App continua sem atualização
- **JSON inválido** → App continua sem atualização
- **Servidor down** → App continua sem atualização
- **URLs inválidas** → App continua, Launcher trata erro

Todos os erros são logados em `Debug.WriteLine()` para troubleshooting.

---

## 📊 Comparação de Versões - Exemplos

```csharp
VersionComparer.IsLessThan("1.0.0", "1.0.1");      // true
VersionComparer.IsLessThan("1.0.9", "1.0.10");     // true
VersionComparer.IsLessThan("1.9.9", "2.0.0");      // true
VersionComparer.IsLessThan("1.0.0", "1.0.0");      // false
VersionComparer.IsLessThan("2.0.0", "1.9.9");      // false
VersionComparer.IsLessThan("1.0", "1.0.0");        // false (completa com .0)
VersionComparer.IsLessThan("v1.0.5", "1.0.6");     // true (ignora "v")
VersionComparer.IsLessThan("1.0.5-beta", "1.0.5"); // true (ignora sufixo)
```

---

## 🚀 Deployment

### GitHub Actions (opcional)

Para atualizar automaticamente o ficheiro JSON quando fizer release:

```yaml
name: Update version.json

on:
  release:
    types: [published]

jobs:
  update:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Update version.json
        run: |
          VERSION=${{ github.event.release.tag_name }}
          NOTES="${{ github.event.release.body }}"
          echo "{\"version\": \"$VERSION\", ...}" > updates/version.json
      - name: Commit
        run: git add updates/version.json && git commit -m "Update version to $VERSION"
```

---

## 📝 Próximos Passos

1. ✅ Criar `/updates/version.json` no GitHub
2. ✅ Atualizar URL em `UpdateService.cs`
3. ✅ Registar em cada MauiProgram.cs
4. ✅ Integrar na página de entrada
5. ✅ Testar em todas as plataformas
6. ✅ Deployer app com versão 1.0.0

---

## 🔗 Documentação Relacionada

- `UPDATE_SERVICE_REGISTRATION.md` - Como registar em MauiProgram
- `UPDATE_SERVICE_EXAMPLE.md` - Exemplo completo de página
- `PLATFORM_CHANGES/ANDROID_CHANGES.md` - Mudanças documentadas

---

**Versão**: 1.0  
**Última Atualização**: 2025-11-09  
**Plataformas Suportadas**: Android, iOS, macOS, Windows
