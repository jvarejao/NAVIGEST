# Solução: Label de Versão Não Atualiza Após Instalação

## Problema
Quando o utilizador descarregava e instalava uma versão nova (ex: v1.0.4), a app abria mas ainda mostrava a versão anterior (ex: v1.0.1) no label do SplashIntroPage e LoginPage. Só após fechar e reabrir é que a versão era atualizada corretamente.

## Raiz do Problema
A versão da app vinha de `AppInfo.Current.VersionString`, que é lida do manifest Android/iOS. Este valor é definido no arquivo `.csproj` na propriedade `ApplicationDisplayVersion`:

```xml
<!-- NAVEGEST.Android/NAVEGEST.Android.csproj -->
<ApplicationDisplayVersion>1.0.1</ApplicationDisplayVersion>
```

**O fluxo de detecção de atualização era:**
1. App abre → SplashIntroPage compara:
   - `AppInfo.Current.VersionString` (manifest) = `1.0.1`
   - `Preferences.Get(INSTALLED_VERSION_KEY)` (guardada) = `1.0.1`
2. Como são iguais, **não detecta mudança**, não guarda versão nova
3. Label mostra versão antiga (`1.0.1`)
4. Só na próxima reabertura é que `Preferences` tinha a versão nova guardada de antes

**Exemplo prático:**
- User instala v1.0.4 APK
- Manifest diz v1.0.1 (porque `.csproj` não foi atualizado)
- Preferences tem v1.0.1 guardado
- Comparação: `1.0.1 == 1.0.1` → Sem mudança detectada
- Label fica preso em 1.0.1

## Solução

### Passo 1: Atualizar versão nos `.csproj`

Antes de compilar cada release, atualizar a versão em AMBOS os arquivos:

```xml
<!-- NAVEGEST.Android/NAVEGEST.Android.csproj (linha ~19) -->
<ApplicationDisplayVersion>1.0.4</ApplicationDisplayVersion>

<!-- NAVEGEST.iOS/NAVEGEST.iOS.csproj (linha ~21) -->
<ApplicationDisplayVersion>1.0.4</ApplicationDisplayVersion>
```

### Passo 2: Recompilar

```bash
cd /Users/joaovarejao/Dev/NAVIGEST

# Android
dotnet publish -c Release -f net9.0-android -p:AndroidPackageFormat=apk \
  src/NAVIGEST.Android/NAVIGEST.Android.csproj

# iOS
dotnet publish -c Release -f net9.0-ios \
  src/NAVIGEST.iOS/NAVIGEST.iOS.csproj
```

### Passo 3: Resultado

Agora o fluxo funciona **instantaneamente**:

1. User instala v1.0.4 APK
2. App abre → SplashIntroPage:
   - Manifest = `1.0.4` ✅
   - Preferences = `1.0.1` (anterior)
   - **Detecta mudança!** `1.0.4 != 1.0.1`
   - Chama `SaveInstalledVersion(1.0.4)` → Preferences guardada
   - Label mostra "Versão 1.0.4" ✅
3. Navega para LoginPage
   - Lê Preferences no `OnAppearing()` → `1.0.4`
   - Label mostra "Versão 1.0.4" ✅ **Imediatamente!**

## Código Relevante

### SplashIntroPage.xaml.cs (Android/iOS)
```csharp
protected override async void OnAppearing()
{
    base.OnAppearing();
    if (_started) return;
    _started = true;

    try
    {
        // ✅ Verificar se a app foi atualizada
        string manifestVersion = AppInfo.Current.VersionString ?? "1.0.0";
        string savedVersion = Preferences.Get(INSTALLED_VERSION_KEY, null) ?? manifestVersion;
        
        if (manifestVersion != savedVersion)
        {
            // Se versão mudou, guardar a nova versão em Preferences
            SaveInstalledVersion(manifestVersion);
        }

        // ✅ Mostrar versão no SplashScreen
        string installedVersion = GetInstalledVersion();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            VersionLabel.Text = $"Versão {installedVersion}";
        });
        
        // ... resto do código
    }
}
```

### LoginPage.xaml.cs (Android/iOS)
```csharp
protected override void OnAppearing()
{
    base.OnAppearing();

    // ✅ Mostrar versão da app (usar versão guardada, não o manifest)
    string installedVersion = Preferences.Get(INSTALLED_VERSION_KEY, AppInfo.Current.VersionString ?? "1.0.0");
    VersionLabel.Text = $"Versão {installedVersion}";
    
    // ... resto do código
}
```

## Checklist para Próximas Releases

Antes de fazer **cada** release novo, verificar:

- [ ] Atualizar versão em `NAVIGEST.Android/NAVIGEST.Android.csproj` → `ApplicationDisplayVersion`
- [ ] Atualizar versão em `NAVIGEST.iOS/NAVIGEST.iOS.csproj` → `ApplicationDisplayVersion`
- [ ] Atualizar `version.json` no GitHub com a nova versão
- [ ] Compilar ambos os projetos (Android + iOS)
- [ ] Criar release no GitHub com os binários
- [ ] Testar instalação e verificar se label atualiza instantaneamente

## Versões Testadas

- ✅ v1.0.1 → v1.0.4: Label atualiza instantaneamente
- ✅ v1.0.4 → v1.0.5: (em testes)
