# 🎯 PLANO DE AÇÃO DETALHADO - NAVIGEST

## 📊 STATUS ATUAL

### ✅ CONCLUÍDO (AppLoginMaui)
- DatabaseService.cs (47 KB) - **ROBUSTO**
  - Conexão MariaDB com pools
  - CRUD completo (Insert, Update, Delete, Select)
  - Suporte a procedures
  - Login com autenticação BCrypt
  - Gestão de clientes/utilizadores
  
- GlobalErro.cs (227 linhas) - **SOFISTICADO**
  - Sistema de logging com ficheiros
  - DisplayActionSheet com opções (Logs, Copiar, Partilhar)
  - Suppression de mensagens duplicadas (3s)
  - Caller info (ficheiro, método, linha)
  - Android exception handling específico
  
- GlobalToast.cs (266 linhas) - **BEM ESTRUTURADO**
  - Fila de toasts (max 4)
  - 4 tipos: Info, Sucesso, Aviso, Erro
  - 3 posições: Top, Center, Bottom
  - Responsive (360px phone, 480px tablet)
  - Timing customizável
  - Icons Font Awesome 7 (Solid)
  
- BiometricAuthService.cs - **FUNCIONA MAS COM ISSUES**
  - Interface clean (IBiometricAuthService)
  - Métodos: IsAvailableAsync, AuthenticateAsync, EnableBiometricLoginAsync, TryAutoLoginAsync, DisableBiometricLoginAsync
  - Plugin.Fingerprint integration
  - **PROBLEMAS**:
    1. ✅ Sintaxe `AuthenticationRequestConfiguration()` está OK
    2. ⚠️  `DisableBiometricLoginAsync()` - falta await em `SecureStorage.Remove()`
    3. ⚠️  Não suporta Face ID iOS específico (só genérico Fingerprint)

- LoginViewModel.cs (123 linhas) - **FUNCIONAL**
  - MVVM pattern completo
  - Binding a Username/Password
  - Biometric login integrado
  - Commands para Login, BiometricLogin, ToggleBiometric
  - **PROBLEMAS**: TODO comments indicam validação mock

### ❌ PROBLEMAS NO APPLOGMAUI
1. Mono-projeto (iOS + Android juntos)
2. Conflitos de provisioning iOS
3. BiometricAuthService tem pequenos bugs
4. LoginViewModel com validação fake
5. Sem separação de plataforma

---

## 🏗️ FASE 1: CRIAR ESTRUTURA NAVIGEST

### 1.1 Criar Pasta NAVIGEST

```bash
mkdir -p /Users/joaovarejao/Dev/NAVIGEST
cd /Users/joaovarejao/Dev/NAVIGEST
```

### 1.2 Criar NAVIGEST.Shared (Class Library)

```bash
dotnet new classlib -n NAVIGEST.Shared -f net9.0
cd NAVIGEST.Shared
rm Class1.cs
```

**Estrutura NAVIGEST.Shared:**
```
NAVIGEST.Shared/
├── NAVIGEST.Shared.csproj
├── Services/
│   ├── Database/
│   │   ├── IDatabaseService.cs (interface)
│   │   └── DatabaseService.cs (de AppLoginMaui)
│   ├── Auth/
│   │   ├── IBiometricAuthService.cs
│   │   ├── BiometricAuthService.cs (CORRIGIDO)
│   │   └── BiometricAuthServiceBase.cs (base class para adaptações)
│   ├── Email/
│   │   └── EmailService.cs (de AppLoginMaui)
│   ├── Error/
│   │   ├── IErrorHandler.cs
│   │   └── ModalErrorHandler.cs (de AppLoginMaui)
│   ├── Settings/
│   │   └── AppSettingsService.cs (de AppLoginMaui)
│   └── Toast/
│       └── GlobalToast.cs (de AppLoginMaui)
├── ViewModels/
│   ├── BaseViewModel.cs (classe abstrata)
│   ├── LoginViewModel.cs (de AppLoginMaui - ADAPTADO)
│   ├── ClientsViewModel.cs
│   └── MainYahViewModel.cs
├── Models/
│   ├── User.cs
│   ├── Client.cs
│   ├── Session.cs
│   └── AppSettings.cs
├── Helpers/
│   ├── GlobalErro.cs (de AppLoginMaui)
│   ├── ServiceHelper.cs (de AppLoginMaui)
│   ├── Converters/
│   │   ├── BytesToImageSourceConverter.cs
│   │   └── StringBoolConverter.cs
│   └── Behaviors/
│       └── HandCursorBehavior.cs
├── Resources/
│   ├── Styles/
│   │   ├── Colors.xaml (de AppLoginMaui)
│   │   ├── DesignSystem.xaml (de AppLoginMaui)
│   │   └── Dimensions.xaml
│   ├── Fonts/
│   │   ├── fa-solid-900.ttf
│   │   ├── fa-regular-400.ttf
│   │   ├── fa-brands-400.ttf
│   │   ├── OpenSans-Regular.ttf
│   │   └── Inter-Regular.ttf
│   ├── Images/
│   │   ├── navigest_logo.svg (NAVIGEST branding!)
│   │   └── app_icon.svg
│   ├── Animations/
│   │   └── splash_animation.xaml
│   └── Raw/
│       └── app_config.json
├── Constants/
│   ├── AppConstants.cs
│   ├── UiConstants.cs
│   └── ApiConstants.cs
└── Extensions/
    ├── ServiceCollectionExtensions.cs
    ├── StringExtensions.cs
    └── ColorExtensions.cs
```

### 1.3 Copiar Ficheiros do AppLoginMaui para NAVIGEST.Shared

```bash
# Services
cp AppLoginMaui/Services/DatabaseService.cs NAVIGEST.Shared/Services/Database/
cp AppLoginMaui/Services/EmailService.cs NAVIGEST.Shared/Services/Email/
cp AppLoginMaui/Services/AppSettingsService.cs NAVIGEST.Shared/Services/Settings/
cp AppLoginMaui/Services/Auth/BiometricAuthService.cs NAVIGEST.Shared/Services/Auth/
cp AppLoginMaui/Services/ModalErrorHandler.cs NAVIGEST.Shared/Services/Error/

# Helpers
cp AppLoginMaui/Helpers/GlobalErro.cs NAVIGEST.Shared/Helpers/
cp AppLoginMaui/Helpers/GlobalToast.cs NAVIGEST.Shared/Helpers/
cp AppLoginMaui/Helpers/ServiceHelper.cs NAVIGEST.Shared/Helpers/

# ViewModels
cp AppLoginMaui/PageModels/LoginViewModel.cs NAVIGEST.Shared/ViewModels/

# Models
cp AppLoginMaui/Models/*.cs NAVIGEST.Shared/Models/

# Resources
cp -r AppLoginMaui/Resources/Styles NAVIGEST.Shared/Resources/
cp -r AppLoginMaui/Resources/Fonts NAVIGEST.Shared/Resources/
cp -r AppLoginMaui/Resources/Images NAVIGEST.Shared/Resources/
cp -r AppLoginMaui/Resources/Animations NAVIGEST.Shared/Resources/ (se existir)
```

---

## 🔧 FASE 2: CORRIGIR E ADAPTAR CÓDIGO

### 2.1 Corrigir BiometricAuthService.cs

**Problema 1: DisableBiometricLoginAsync falta await**

```csharp
// ❌ ANTES
public async Task DisableBiometricLoginAsync()
{
    SecureStorage.Remove(KeyBiometricEnabled);
    SecureStorage.Remove(KeyBioToken);
    await Task.CompletedTask;
}

// ✅ DEPOIS
public async Task DisableBiometricLoginAsync()
{
    await SecureStorage.Default.Remove(KeyBiometricEnabled);
    await SecureStorage.Default.Remove(KeyBioToken);
}
```

**Problema 2: AuthenticationRequestConfiguration com construtor melhorado**

```csharp
// ✅ MELHORADO
public async Task<bool> AuthenticateAsync(string reason = "Autenticar com biometria")
{
    try
    {
        var request = new AuthenticationRequestConfiguration
        {
            Title = "Segurança NAVIGEST",
            Reason = reason,
            FallbackTitle = "Usar código",
            CancelTitle = "Cancelar",
            AllowAlternativeAuthentication = false,
            DisableAlternativeButton = false
        };

        var result = await CrossFingerprint.Current.AuthenticateAsync(request);
        return result?.Authenticated ?? false;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Biometric auth error: {ex.Message}");
        return false;
    }
}
```

**Problema 3: Adaptar para Face ID (iOS) vs Fingerprint (Android)**

```csharp
// ✅ ADICIONAR
#if __IOS__
    private string GetBiometricType()
    {
        var biometricType = CrossFingerprint.Current.GetFingerprintAvailabilityAsync().Result;
        // iOS: Face ID or Touch ID
        return biometricType.ToString();
    }
#endif

#if __ANDROID__
    private string GetBiometricType()
    {
        // Android: Fingerprint, Face recognition, Iris
        return "Fingerprint";
    }
#endif
```

### 2.2 Corrigir LoginViewModel.cs

**Problema 1: Validação real (não mock)**

```csharp
// ❌ ANTES
private async Task LoginAsync()
{
    var ok = CanLogin();
    if (!ok)
    {
        await Application.Current!.MainPage!.DisplayAlert("Login", "Credenciais inválidas.", "OK");
        return;
    }
    // ...
}

// ✅ DEPOIS
private async Task LoginAsync()
{
    if (!CanLogin())
    {
        await GlobalErro.MostrarErroAsync(new InvalidOperationException("Username e password são obrigatórios"));
        return;
    }

    try
    {
        // Chamar autenticação real via DatabaseService
        var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(Username, Password);
        
        if (!ok)
        {
            await GlobalToast.ShowAsync("Credenciais inválidas", ToastTipo.Erro);
            return;
        }

        // Guardar sessão
        await SecureStorage.SetAsync("user_id", nome);
        await SecureStorage.SetAsync("user_type", tipo);

        if (BiometricAvailable && BiometricEnabled)
            await _bio.EnableBiometricLoginAsync($"{nome}::{tipo}");

        await GlobalToast.ShowAsync("Bem-vindo!", ToastTipo.Sucesso);
        
        // Navegar
        if (Shell.Current is not null)
            await Shell.Current.GoToAsync("///clients");
    }
    catch (Exception ex)
    {
        await GlobalErro.MostrarErroAsync(ex);
    }
}
```

### 2.3 Criar BaseViewModel (Reutilizável)

```csharp
// NAVIGEST.Shared/ViewModels/BaseViewModel.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace NAVIGEST.Shared.ViewModels
{
    public abstract class BaseViewModel : BindableObject, INotifyPropertyChanged
    {
        private bool _isBusy;
        private string _title = string.Empty;

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        protected void SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
        }

        protected async Task<T?> ExecuteAsync<T>(Func<Task<T>> operation, string? errorMessage = null)
        {
            try
            {
                IsBusy = true;
                return await operation();
            }
            catch (Exception ex)
            {
                await GlobalErro.MostrarErroAsync(ex, errorMessage);
                return default;
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task ExecuteAsync(Func<Task> operation, string? errorMessage = null)
        {
            try
            {
                IsBusy = true;
                await operation();
            }
            catch (Exception ex)
            {
                await GlobalErro.MostrarErroAsync(ex, errorMessage);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
```

---

## 📱 FASE 3: CRIAR NAVIGEST.iOS

### 3.1 Criar projeto MAUI iOS

```bash
dotnet new maui -n NAVIGEST.iOS
cd NAVIGEST.iOS
```

### 3.2 Estrutura NAVIGEST.iOS

```
NAVIGEST.iOS/
├── NAVIGEST.iOS.csproj (referência NAVIGEST.Shared)
├── App.xaml/cs
├── AppShell.xaml/cs
├── MauiProgram.cs
├── Pages/
│   ├── LoginPage.xaml (iOS-specific com Face ID)
│   ├── LoginPage.xaml.cs
│   ├── ClientsPage.xaml (iOS-specific ListPage)
│   ├── ClientsPage.xaml.cs
│   └── MainYahPage.xaml
├── Platforms/
│   ├── iOS/
│   │   ├── Info.plist (Face ID permissions)
│   │   └── LocalNotificationHandler.cs
│   └── ...
├── Resources/
│   ├── Splash/
│   │   ├── splash.svg
│   │   └── splash_animation.xaml
│   └── AppIcon/
└── Services/
    ├── PlatformBiometricService.cs (Face ID específico)
    └── Handlers/
```

### 3.3 App.xaml (Root shell)

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<Shell
    x:Class="NAVIGEST.iOS.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    FlyoutBehavior="Disabled">

    <TabBar>
        <ShellContent
            Title="Login"
            ContentTemplate="{DataTemplate local:LoginPage}"
            Route="login" />
        
        <ShellContent
            Title="Clientes"
            ContentTemplate="{DataTemplate local:ClientsPage}"
            Route="clients" />
    </TabBar>
</Shell>
```

### 3.4 LoginPage.xaml (iOS com Face ID)

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="NAVIGEST.iOS.Pages.LoginPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    Title="NAVIGEST"
    BackgroundColor="{StaticResource BackgroundColor}">

    <VerticalStackLayout Padding="20" Spacing="15" VerticalOptions="Center">
        
        <!-- Logo NAVIGEST -->
        <Image
            Source="navigest_logo.svg"
            WidthRequest="120"
            HeightRequest="120"
            HorizontalOptions="Center" />
        
        <!-- Username -->
        <Entry
            x:Name="UsernameEntry"
            Placeholder="Utilizador"
            Text="{Binding Username}"
            Keyboard="Default"
            IsPassword="False" />
        
        <!-- Password -->
        <Entry
            x:Name="PasswordEntry"
            Placeholder="Senha"
            Text="{Binding Password}"
            IsPassword="True" />
        
        <!-- Login Button -->
        <Button
            Text="Entrar"
            Command="{Binding LoginCommand}"
            BackgroundColor="{StaticResource PrimaryColor}" />
        
        <!-- Ou Biometria -->
        <Label
            Text="OU"
            HorizontalOptions="Center"
            TextColor="{StaticResource TextSecondaryColor}" />
        
        <!-- Face ID Button (iOS specific) -->
        <Button
            Text="Face ID"
            Command="{Binding BiometricLoginCommand}"
            IsVisible="{Binding BiometricAvailable}"
            BackgroundColor="{StaticResource SecondaryColor}" />
    </VerticalStackLayout>
</ContentPage>
```

### 3.5 LoginPage.xaml.cs

```csharp
using NAVIGEST.Shared.ViewModels;
using NAVIGEST.iOS.Pages;

namespace NAVIGEST.iOS.Pages
{
    public partial class LoginPage : ContentPage
    {
        private LoginViewModel _viewModel;

        public LoginPage()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel(
                new BiometricAuthService()
            );
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.InitCommand.Execute(null);
        }
    }
}
```

### 3.6 MauiProgram.cs (iOS)

```csharp
using NAVIGEST.Shared.Services.Auth;
using NAVIGEST.iOS.Pages;

namespace NAVIGEST.iOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder()
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
                });

            // Registrar serviços
            builder.Services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
            builder.Services.AddSingleton<LoginPage>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<App>();

            return builder.Build();
        }
    }
}
```

### 3.7 Info.plist (Face ID Permissions)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- ... outras propriedades ... -->
    
    <!-- Face ID Permission -->
    <key>NSFaceIDUsageDescription</key>
    <string>NAVIGEST utiliza Face ID para autenticação rápida e segura.</string>
    
    <!-- Biometric Permission (iOS 13+) -->
    <key>NSLocalNetworkUsageDescription</key>
    <string>NAVIGEST necessita acesso biométrico.</string>
</dict>
</plist>
```

---

## 🤖 FASE 4: CRIAR NAVIGEST.ANDROID

### 4.1 Criar projeto MAUI Android

```bash
dotnet new maui -n NAVIGEST.Android
cd NAVIGEST.Android
```

### 4.2 Estrutura Similar a iOS, mas:

- LoginPage com Fingerprint (não Face ID)
- Material Design (não iOS design)
- AndroidManifest.xml com USE_BIOMETRIC permission

### 4.3 AndroidManifest.xml

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    
    <!-- Biometric Permissions -->
    <uses-permission android:name="android.permission.USE_BIOMETRIC" />
    <uses-permission android:name="android.permission.USE_FINGERPRINT" />
    
    <!-- Database Permissions -->
    <uses-permission android:name="android.permission.INTERNET" />
    <uses-permission android:name="android.permission.ACCESS_NETWORK_STATE" />
    
    <!-- ... resto ... -->
</manifest>
```

---

## 💻 FASE 5: CRIAR NAVIGEST.WINDOWS (WinUI)

### 5.1 Criar projeto WinUI

```bash
dotnet new winui -n NAVIGEST.Windows
```

### 5.2 Views (em vez de Pages)

- LoginView.xaml (WinUI style)
- ClientsView.xaml (com DataGrid)
- MainYahView.xaml

---

## 🎉 FASE 6: TESTES E PUBLICAÇÃO

### 6.1 iOS

```bash
cd NAVIGEST.iOS
dotnet build -f net9.0-ios
# ou
dotnet publish -f net9.0-ios -c Release
```

### 6.2 Android

```bash
cd NAVIGEST.Android
dotnet build -f net9.0-android
```

### 6.3 Windows

```bash
cd NAVIGEST.Windows
dotnet build
```

---

## 📋 CHECKLIST

### Fase 1: Estrutura ✅
- [ ] Criar pasta NAVIGEST
- [ ] Criar NAVIGEST.Shared class library
- [ ] Copiar Services, Helpers, Models, ViewModels
- [ ] Copiar Resources (Styles, Fonts, Images)
- [ ] Ajustar namespaces (AppLoginMaui → NAVIGEST.Shared)

### Fase 2: Correções ✅
- [ ] Corrigir BiometricAuthService (DisableBiometricLoginAsync)
- [ ] Corrigir BiometricAuthService (AuthenticationRequestConfiguration)
- [ ] Melhorar LoginViewModel (validação real)
- [ ] Criar BaseViewModel
- [ ] Adicionar error handling completo

### Fase 3: iOS ⏳
- [ ] Criar NAVIGEST.iOS projeto
- [ ] Criar LoginPage iOS (Face ID)
- [ ] Criar ClientsPage iOS
- [ ] Configurar Info.plist (Face ID permissions)
- [ ] Testar Face ID em device

### Fase 4: Android ⏳
- [ ] Criar NAVIGEST.Android projeto
- [ ] Criar LoginPage Android (Fingerprint)
- [ ] Criar ClientsPage Android
- [ ] Configurar AndroidManifest (Biometric permissions)
- [ ] Testar Fingerprint em device

### Fase 5: Windows ⏳
- [ ] Criar NAVIGEST.Windows projeto
- [ ] Criar LoginView WinUI
- [ ] Criar ClientsView com DataGrid
- [ ] Configurar packaging

### Fase 6: Testes ⏳
- [ ] Teste iOS Face ID
- [ ] Teste Android Fingerprint
- [ ] Teste Windows login
- [ ] Teste database conexão
- [ ] Teste error handling

---

## 🚀 PRÓXIMOS PASSOS IMEDIATOS

1. **HOJE**: Copiar ficheiros do AppLoginMaui para NAVIGEST.Shared
2. **HOJE**: Corrigir BiometricAuthService
3. **HOJE**: Criar BaseViewModel
4. **HOJE**: Criar NAVIGEST.iOS com LoginPage
5. **AMANHÃ**: Testar iOS build
6. **AMANHÃ**: Criar NAVIGEST.Android
7. **DEPOIS**: Windows e MacOS

---

## 💡 DICAS IMPORTANTES

- ✅ Cada projeto isolado = sem conflitos de provisioning
- ✅ NAVIGEST.Shared = código reutilizável
- ✅ Face ID iOS ≠ Fingerprint Android (implementar ambos)
- ✅ Testa incrementalmente, não tudo de uma vez
- ✅ Guarda NAVIGEST logo em Resources (não vai perder!)
- ✅ Usa git commits frequentes (não como da primeira vez!)

