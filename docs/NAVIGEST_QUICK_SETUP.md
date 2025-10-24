# 🚀 QUICK REFERENCE - NAVIGEST SETUP

## 1️⃣ FASE 1: CREATE NAVIGEST.SHARED (2 HORAS)

### Step 1.1: Create folder structure
```bash
mkdir -p ~/Dev/NAVIGEST
cd ~/Dev/NAVIGEST
dotnet new classlib -n NAVIGEST.Shared -f net9.0
cd NAVIGEST.Shared
rm Class1.cs
```

### Step 1.2: Create folder structure
```bash
mkdir -p Services/{Database,Auth,Email,Error,Settings}
mkdir -p Services/Toast
mkdir -p ViewModels
mkdir -p Models
mkdir -p Helpers/{Converters,Behaviors}
mkdir -p Resources/{Styles,Fonts,Images,Animations,Raw}
mkdir -p Constants
mkdir -p Extensions
```

### Step 1.3: Copy files from AppLoginMaui
```bash
# From: /Users/joaovarejao/Dev/GESTYAH2024/RJ Modern UI-VBWF-EN-M1/AppLoginMaui/AppLoginMaui/

# Services
cp Services/DatabaseService.cs NAVIGEST.Shared/Services/Database/
cp Services/AppSettingsService.cs NAVIGEST.Shared/Services/Settings/
cp Services/ClientsDbService.cs NAVIGEST.Shared/Services/Database/
cp Services/EmailService.cs NAVIGEST.Shared/Services/Email/
cp Services/ModalErrorHandler.cs NAVIGEST.Shared/Services/Error/
cp Services/Auth/BiometricAuthService.cs NAVIGEST.Shared/Services/Auth/

# Helpers
cp Helpers/GlobalErro.cs NAVIGEST.Shared/Helpers/
cp Helpers/GlobalToast.cs NAVIGEST.Shared/Helpers/
cp Helpers/ServiceHelper.cs NAVIGEST.Shared/Helpers/

# ViewModels
cp PageModels/LoginViewModel.cs NAVIGEST.Shared/ViewModels/

# Models
cp Models/*.cs NAVIGEST.Shared/Models/

# Converters & Behaviors
cp Converters/*.cs NAVIGEST.Shared/Helpers/Converters/
cp Behaviors/*.cs NAVIGEST.Shared/Helpers/Behaviors/

# Resources
cp -r Resources/Styles NAVIGEST.Shared/Resources/
cp -r Resources/Fonts NAVIGEST.Shared/Resources/
cp -r Resources/Images NAVIGEST.Shared/Resources/
cp -r Resources/Splash NAVIGEST.Shared/Resources/
cp -r Resources/AppIcon NAVIGEST.Shared/Resources/
```

### Step 1.4: Fix namespaces
**Find and replace in NAVIGEST.Shared/**:
- `namespace AppLoginMaui` → `namespace NAVIGEST.Shared`
- `namespace AppLoginMaui.Services` → `namespace NAVIGEST.Shared.Services`
- `namespace AppLoginMaui.Helpers` → `namespace NAVIGEST.Shared.Helpers`
- `namespace AppLoginMaui.Models` → `namespace NAVIGEST.Shared.Models`
- `namespace AppLoginMaui.PageModels` → `namespace NAVIGEST.Shared.ViewModels`

### Step 1.5: Apply 3 critical code fixes

**File: Services/Auth/BiometricAuthService.cs**

**FIX #1** - Line ~60 (DisableBiometricLoginAsync):
```csharp
// ❌ BEFORE
public async Task DisableBiometricLoginAsync()
{
    SecureStorage.Remove(KeyBiometricEnabled);
    SecureStorage.Remove(KeyBioToken);
    await Task.CompletedTask;
}

// ✅ AFTER
public async Task DisableBiometricLoginAsync()
{
    await SecureStorage.Default.Remove(KeyBiometricEnabled);
    await SecureStorage.Default.Remove(KeyBioToken);
}
```

**FIX #2** - Line ~30 (AuthenticateAsync constructor):
```csharp
// ❌ BEFORE
var request = new AuthenticationRequestConfiguration("Segurança", reason)
{
    FallbackTitle = "Usar código",
    CancelTitle = "Cancelar",
    AllowAlternativeAuthentication = false
};

// ✅ AFTER
var request = new AuthenticationRequestConfiguration
{
    Title = "Segurança NAVIGEST",
    Reason = reason,
    FallbackTitle = "Usar código",
    CancelTitle = "Cancelar",
    AllowAlternativeAuthentication = false
};
```

**File: ViewModels/LoginViewModel.cs**

**FIX #3** - Line ~80 (LoginAsync method):
```csharp
// ❌ BEFORE
private async Task LoginAsync()
{
    var ok = CanLogin();
    if (!ok)
    {
        await Application.Current!.MainPage!.DisplayAlert("Login", "Credenciais inválidas.", "OK");
        return;
    }

    var token = $"{Username}::session";
    await Application.Current!.MainPage!.DisplayAlert("Login", "Sessão iniciada.", "OK");
    if (Shell.Current is not null)
        await Shell.Current.GoToAsync("//MainPage");
}

// ✅ AFTER
private async Task LoginAsync()
{
    if (!CanLogin())
    {
        await GlobalToast.ShowAsync("Preencha utilizador e senha", ToastTipo.Aviso);
        return;
    }

    try
    {
        var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(Username, Password);
        
        if (!ok)
        {
            await GlobalToast.ShowAsync("Credenciais inválidas", ToastTipo.Erro);
            return;
        }

        await SecureStorage.Default.SetAsync("user_name", nome ?? Username);
        await SecureStorage.Default.SetAsync("user_type", tipo ?? "User");

        if (BiometricAvailable && BiometricEnabled)
        {
            var token = $"{nome}::{tipo}::{Guid.NewGuid()}";
            await _bio.EnableBiometricLoginAsync(token);
        }

        await GlobalToast.ShowAsync($"Bem-vindo, {nome}!", ToastTipo.Sucesso);
        
        if (Shell.Current != null)
            await Shell.Current.GoToAsync("///clients");
    }
    catch (Exception ex)
    {
        await GlobalErro.TratarErro(ex, mostrarAlerta: true);
    }
}
```

### Step 1.6: Create extension methods
**Create file: Extensions/ServiceCollectionExtensions.cs**
```csharp
using Microsoft.Extensions.DependencyInjection;
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Services.Auth;

namespace NAVIGEST.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNavigestServices(this IServiceCollection services)
        {
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<IBiometricAuthService, BiometricAuthService>();
            services.AddSingleton<EmailService>();
            return services;
        }

        public static IServiceCollection AddNavigestViewModels(this IServiceCollection services)
        {
            services.AddTransient<LoginViewModel>();
            return services;
        }
    }
}
```

### Step 1.7: Create .csproj
**File: NAVIGEST.Shared.csproj**
```xml
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net9.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>
    
    <ItemGroup>
        <PackageReference Include="Microsoft.Maui.Controls" Version="9.0.0" />
        <PackageReference Include="MySqlConnector" Version="2.3.0" />
        <PackageReference Include="Plugin.Fingerprint" Version="3.0.0-beta.1" />
        <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    </ItemGroup>
</Project>
```

---

## 2️⃣ FASE 2: CREATE NAVIGEST.iOS (2 HORAS)

### Step 2.1: Create MAUI iOS project
```bash
cd ~/Dev/NAVIGEST
dotnet new maui -n NAVIGEST.iOS
cd NAVIGEST.iOS
```

### Step 2.2: Create folder structure
```bash
mkdir -p Pages
mkdir -p Handlers
mkdir -p Resources/{Splash,AppIcon}
```

### Step 2.3: Add reference to NAVIGEST.Shared
**Edit NAVIGEST.iOS.csproj**:
```xml
<ItemGroup>
    <ProjectReference Include="..\NAVIGEST.Shared\NAVIGEST.Shared.csproj" />
</ItemGroup>
```

### Step 2.4: Create AppShell.xaml
```xaml
<?xml version="1.0" encoding="UTF-8" ?>
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

### Step 2.5: Create LoginPage.xaml (iOS specific)
```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="NAVIGEST.iOS.Pages.LoginPage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    Title="NAVIGEST"
    BackgroundColor="{StaticResource BackgroundColor}">

    <VerticalStackLayout Padding="20" Spacing="15" VerticalOptions="Center">
        
        <Image
            Source="navigest_logo.svg"
            WidthRequest="120"
            HeightRequest="120"
            HorizontalOptions="Center" />
        
        <Entry
            Placeholder="Utilizador"
            Text="{Binding Username}" />
        
        <Entry
            Placeholder="Senha"
            Text="{Binding Password}"
            IsPassword="True" />
        
        <Button
            Text="Entrar"
            Command="{Binding LoginCommand}"
            BackgroundColor="{StaticResource PrimaryColor}" />
        
        <Button
            Text="Face ID"
            Command="{Binding BiometricLoginCommand}"
            IsVisible="{Binding BiometricAvailable}"
            BackgroundColor="{StaticResource SecondaryColor}" />
    </VerticalStackLayout>
</ContentPage>
```

### Step 2.6: Create LoginPage.xaml.cs
```csharp
using NAVIGEST.iOS.Pages;
using NAVIGEST.Shared.ViewModels;
using NAVIGEST.Shared.Services.Auth;

namespace NAVIGEST.iOS.Pages
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();
            var viewModel = new LoginViewModel(
                ServiceHelper.GetService<IBiometricAuthService>()!
            );
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var vm = (LoginViewModel)BindingContext;
            await vm.InitCommand.Execute(null);
        }
    }
}
```

### Step 2.7: Create MauiProgram.cs
```csharp
using NAVIGEST.iOS.Pages;
using NAVIGEST.Shared.Extensions;

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
                })
                .Services
                .AddNavigestServices()
                .AddNavigestViewModels()
                .AddSingleton<LoginPage>()
                .AddSingleton<AppShell>()
                .AddSingleton<App>();

            return builder.Build();
        }
    }
}
```

### Step 2.8: Update Info.plist (Face ID permissions)
**Edit Platforms/iOS/Info.plist**:
```xml
<key>NSFaceIDUsageDescription</key>
<string>NAVIGEST uses Face ID for secure authentication.</string>
```

### Step 2.9: Test iOS build
```bash
cd ~/Dev/NAVIGEST/NAVIGEST.iOS
dotnet build -f net9.0-ios -c Debug
# Deploy to iOS simulator or device
```

---

## 3️⃣ FASE 3: CREATE NAVIGEST.ANDROID (2 HORAS)

**Same as iOS but**:
- Use Material Design colors
- Fingerprint instead of Face ID
- AndroidManifest.xml instead of Info.plist
- Add `android:permission.USE_BIOMETRIC`

---

## ✅ VALIDATION CHECKLIST

- [ ] NAVIGEST.Shared builds without errors
- [ ] All namespaces updated (AppLoginMaui → NAVIGEST.Shared)
- [ ] BiometricAuthService: FIX #1, #2 applied
- [ ] LoginViewModel: FIX #3 applied and tested
- [ ] NAVIGEST.iOS builds without errors
- [ ] NAVIGEST.iOS runs on simulator
- [ ] Face ID permission dialog appears
- [ ] Login with test credentials works
- [ ] GlobalToast messages display correctly
- [ ] GlobalErro error handling works
- [ ] NAVIGEST logo displays on LoginPage

---

## 📊 FILES CHECKLIST

### NAVIGEST.Shared/
- [ ] Services/Database/DatabaseService.cs
- [ ] Services/Database/ClientsDbService.cs
- [ ] Services/Auth/BiometricAuthService.cs (✅ FIXED)
- [ ] Services/Email/EmailService.cs
- [ ] Services/Error/ModalErrorHandler.cs
- [ ] Services/Settings/AppSettingsService.cs
- [ ] Helpers/GlobalErro.cs
- [ ] Helpers/GlobalToast.cs
- [ ] Helpers/ServiceHelper.cs
- [ ] Helpers/Converters/*.cs
- [ ] Helpers/Behaviors/*.cs
- [ ] ViewModels/LoginViewModel.cs (✅ FIXED)
- [ ] ViewModels/BaseViewModel.cs (NEW)
- [ ] Models/*.cs
- [ ] Resources/Styles/
- [ ] Resources/Fonts/
- [ ] Resources/Images/
- [ ] Resources/Splash/
- [ ] Extensions/ServiceCollectionExtensions.cs (NEW)

### NAVIGEST.iOS/
- [ ] Pages/LoginPage.xaml
- [ ] Pages/LoginPage.xaml.cs
- [ ] Pages/ClientsPage.xaml (?)
- [ ] AppShell.xaml
- [ ] MauiProgram.cs
- [ ] Platforms/iOS/Info.plist (✅ UPDATED)

---

## 🐛 TROUBLESHOOTING

### Error: "AuthenticationRequestConfiguration constructor doesn't match"
**Fix**: Use property initialization instead of positional parameters
```csharp
// ❌ WRONG
new AuthenticationRequestConfiguration("Title", "Reason")

// ✅ RIGHT
new AuthenticationRequestConfiguration { Title = "...", Reason = "..." }
```

### Error: "SecureStorage.Remove is not async"
**Fix**: Use `SecureStorage.Default` with await
```csharp
// ❌ WRONG
SecureStorage.Remove(key);

// ✅ RIGHT
await SecureStorage.Default.Remove(key);
```

### Error: "Namespace AppLoginMaui not found"
**Fix**: Update all using statements
```csharp
// ❌ WRONG
using AppLoginMaui;
using AppLoginMaui.Services;

// ✅ RIGHT
using NAVIGEST.Shared;
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Helpers;
```

### Error: "DisplayAlert not found in iOS"
**Fix**: Use GlobalToast instead
```csharp
// ❌ WRONG
await Application.Current!.MainPage!.DisplayAlert("Title", "Message", "OK");

// ✅ RIGHT
await GlobalToast.ShowAsync("Message", ToastTipo.Info);
```

---

## 🎯 NEXT STEPS

1. ✅ Execute FASE 1 (NAVIGEST.Shared) - **2 hours**
2. ✅ Execute FASE 2 (NAVIGEST.iOS) - **2 hours**
3. ⏳ Execute FASE 3 (NAVIGEST.Android) - **2 hours**
4. ⏳ Execute FASE 4 (NAVIGEST.Windows) - **2 hours** (optional)
5. ⏳ Full integration testing - **2 hours**

**Total: ~10 hours from scratch to working NAVIGEST multi-platform solution**

