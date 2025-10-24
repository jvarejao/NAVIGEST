# 🔧 CORREÇÕES DE CÓDIGO - APPLOGMAUI → NAVIGEST

## 1. BiometricAuthService.cs - CORREÇÕES NECESSÁRIAS

### ❌ PROBLEMA 1: `DisableBiometricLoginAsync` sem await

**Ficheiro**: `Services/Auth/BiometricAuthService.cs`

**Problema**:
```csharp
public async Task DisableBiometricLoginAsync()
{
    SecureStorage.Remove(KeyBiometricEnabled);  // ❌ Sem await
    SecureStorage.Remove(KeyBioToken);          // ❌ Sem await
    await Task.CompletedTask;
}
```

**Causa**: `SecureStorage.Remove()` em versões antigas de MAUI não retorna `Task`, mas em versões mais novas retorna `Task`.

**Solução (Compatible com MAUI 9.0)**:
```csharp
public async Task DisableBiometricLoginAsync()
{
    // ✅ Método correto para MAUI 9.0+
    await SecureStorage.Default.Remove(KeyBiometricEnabled);
    await SecureStorage.Default.Remove(KeyBioToken);
}
```

---

### ❌ PROBLEMA 2: `AuthenticationRequestConfiguration` construtor

**Código Original**:
```csharp
var request = new AuthenticationRequestConfiguration("Segurança", reason)
{
    FallbackTitle = "Usar código",
    CancelTitle = "Cancelar",
    AllowAlternativeAuthentication = false
};
```

**Problema**: 
- Construtor pode não aceitar 2 parâmetros posicionais
- Plugin.Fingerprint usa properties, não construtor

**Solução**:
```csharp
var request = new AuthenticationRequestConfiguration
{
    Title = "Segurança NAVIGEST",
    Reason = reason,
    FallbackTitle = "Usar código",
    CancelTitle = "Cancelar",
    AllowAlternativeAuthentication = false
};
```

---

### ❌ PROBLEMA 3: Falta null-checking em `TryAutoLoginAsync`

**Código Original**:
```csharp
var ok = await AuthenticateAsync("Entrar com Face ID/biometria");
if (!ok) return null;

return await SecureStorage.GetAsync(KeyBioToken);
```

**Problema**: Se o token não existe, retorna null em vez de falhar gracefully.

**Solução**:
```csharp
public async Task<string?> TryAutoLoginAsync()
{
    var enabled = await SecureStorage.Default.GetAsync(KeyBiometricEnabled);
    if (enabled != "1")
        return null;

    var ok = await AuthenticateAsync("Entrar com Face ID/biometria");
    if (!ok)
    {
        await GlobalToast.ShowAsync("Autenticação biométrica falhou", ToastTipo.Erro);
        return null;
    }

    var token = await SecureStorage.Default.GetAsync(KeyBioToken);
    if (string.IsNullOrEmpty(token))
    {
        await GlobalToast.ShowAsync("Token não encontrado", ToastTipo.Aviso);
        return null;
    }

    return token;
}
```

---

### ✅ MELHORAMENTO 1: Adicionar error handling completo

**Versão Melhorada**:
```csharp
using System.Threading.Tasks;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Microsoft.Maui.Storage;
using System;

namespace NAVIGEST.Shared.Services.Auth
{
    public interface IBiometricAuthService
    {
        Task<bool> IsAvailableAsync();
        Task<bool> AuthenticateAsync(string reason = "Autenticar com biometria");
        Task EnableBiometricLoginAsync(string userIdOrToken);
        Task<string?> TryAutoLoginAsync();
        Task DisableBiometricLoginAsync();
        Task<string> GetBiometricTypeAsync();
    }

    public class BiometricAuthService : IBiometricAuthService
    {
        private const string KeyBiometricEnabled = "bio_enabled";
        private const string KeyBioToken = "bio_token";

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var isAvailable = await CrossFingerprint.Current.IsAvailableAsync(true);
                return isAvailable;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"IsAvailableAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AuthenticateAsync(string reason = "Autenticar com biometria")
        {
            try
            {
                // ✅ CORRIGIDO: Usando property initialization
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
                System.Diagnostics.Debug.WriteLine($"AuthenticateAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task EnableBiometricLoginAsync(string userIdOrToken)
        {
            try
            {
                await SecureStorage.Default.SetAsync(KeyBiometricEnabled, "1");
                await SecureStorage.Default.SetAsync(KeyBioToken, userIdOrToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"EnableBiometricLoginAsync error: {ex.Message}");
                throw;
            }
        }

        public async Task<string?> TryAutoLoginAsync()
        {
            try
            {
                var enabled = await SecureStorage.Default.GetAsync(KeyBiometricEnabled);
                if (enabled != "1")
                    return null;

                var ok = await AuthenticateAsync("Entrar com NAVIGEST via biometria");
                if (!ok)
                    return null;

                var token = await SecureStorage.Default.GetAsync(KeyBioToken);
                return string.IsNullOrEmpty(token) ? null : token;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TryAutoLoginAsync error: {ex.Message}");
                return null;
            }
        }

        public async Task DisableBiometricLoginAsync()
        {
            try
            {
                // ✅ CORRIGIDO: Usando SecureStorage.Default com await
                await SecureStorage.Default.Remove(KeyBiometricEnabled);
                await SecureStorage.Default.Remove(KeyBioToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DisableBiometricLoginAsync error: {ex.Message}");
            }
        }

        public async Task<string> GetBiometricTypeAsync()
        {
            try
            {
                var available = await IsAvailableAsync();
                if (!available)
                    return "Not Available";

#if __IOS__
                // iOS: Face ID ou Touch ID
                var request = new AuthenticationRequestConfiguration { Title = "Check" };
                var result = await CrossFingerprint.Current.AuthenticateAsync(request);
                return result?.BiometricType.ToString() ?? "Unknown";
#elif __ANDROID__
                // Android: Fingerprint, Face, Iris
                return "Fingerprint";
#else
                return "Unknown";
#endif
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetBiometricTypeAsync error: {ex.Message}");
                return "Error";
            }
        }
    }
}
```

---

## 2. LoginViewModel.cs - CORREÇÕES NECESSÁRIAS

### ❌ PROBLEMA 1: Validação mock (fake)

**Código Original**:
```csharp
private async Task LoginAsync()
{
    var ok = CanLogin();
    if (!ok)
    {
        await Application.Current!.MainPage!.DisplayAlert("Login", "Credenciais inválidas.", "OK");
        return;
    }

    var token = $"{Username}::session";

    if (BiometricAvailable && BiometricEnabled)
        await _bio.EnableBiometricLoginAsync(token);

    await Application.Current!.MainPage!.DisplayAlert("Login", "Sessão iniciada.", "OK");
    if (Shell.Current is not null)
        await Shell.Current.GoToAsync("//MainPage");
}
```

**Problema**: 
- Valida localmente (CanLogin só verifica se está vazio)
- Não contacta base de dados
- Não chama DatabaseService.CheckLoginAsync

**Solução (COM VALIDAÇÃO REAL)**:
```csharp
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NAVIGEST.Shared.Services.Auth;
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.ViewModels;

namespace NAVIGEST.Shared.ViewModels
{
    public sealed class LoginViewModel : BaseViewModel
    {
        private readonly IBiometricAuthService _bio;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _biometricAvailable;
        private bool _biometricEnabled;

        public LoginViewModel(IBiometricAuthService bio)
        {
            _bio = bio;

            LoginCommand = new Command(async () => await LoginAsync(), CanLogin);
            BiometricLoginCommand = new Command(async () => await BiometricLoginAsync(), () => BiometricAvailable);
            ToggleBiometricCommand = new Command(async () => await ToggleBiometricAsync());
            InitCommand = new Command(async () => await InitAsync());
        }

        // ======= PROPRIEDADES =======
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); ((Command)LoginCommand).ChangeCanExecute(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); ((Command)LoginCommand).ChangeCanExecute(); }
        }

        public bool BiometricAvailable
        {
            get => _biometricAvailable;
            private set { _biometricAvailable = value; OnPropertyChanged(); ((Command)BiometricLoginCommand).ChangeCanExecute(); }
        }

        public bool BiometricEnabled
        {
            get => _biometricEnabled;
            set { _biometricEnabled = value; OnPropertyChanged(); }
        }

        // ======= COMANDOS =======
        public ICommand LoginCommand { get; }
        public ICommand BiometricLoginCommand { get; }
        public ICommand ToggleBiometricCommand { get; }
        public ICommand InitCommand { get; }

        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        // ======= FLUXOS =======
        private async Task InitAsync()
        {
            try
            {
                BiometricAvailable = await _bio.IsAvailableAsync();
                BiometricEnabled = (await SecureStorage.Default.GetAsync("bio_enabled")) == "1";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitAsync error: {ex}");
                await GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }

        // ✅ CORRIGIDO: COM VALIDAÇÃO REAL NA BD
        private async Task LoginAsync()
        {
            if (!CanLogin())
            {
                await GlobalToast.ShowAsync("Preencha utilizador e senha", ToastTipo.Aviso);
                return;
            }

            await ExecuteAsync(async () =>
            {
                try
                {
                    // ✅ Contactar DatabaseService real
                    var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(Username, Password);
                    
                    if (!ok)
                    {
                        await GlobalToast.ShowAsync("Credenciais inválidas", ToastTipo.Erro);
                        return;
                    }

                    // ✅ Guardar sessão de forma segura
                    await SecureStorage.Default.SetAsync("user_name", nome ?? Username);
                    await SecureStorage.Default.SetAsync("user_type", tipo ?? "User");
                    await SecureStorage.Default.SetAsync("login_time", DateTime.UtcNow.ToString("O"));

                    // ✅ Se ativada, guardar token biométrico
                    if (BiometricAvailable && BiometricEnabled)
                    {
                        var token = $"{nome}::{tipo}::{Guid.NewGuid()}";
                        await _bio.EnableBiometricLoginAsync(token);
                    }

                    await GlobalToast.ShowAsync($"Bem-vindo, {nome}!", ToastTipo.Sucesso);
                    
                    // ✅ Navegação segura
                    if (Shell.Current != null)
                        await Shell.Current.GoToAsync("///clients");
                    else
                        Application.Current?.MainPage?.Navigation.PushAsync(new MainYahPage());
                }
                catch (Exception ex)
                {
                    await GlobalErro.TratarErro(ex, mostrarAlerta: true);
                }
            });
        }

        // ✅ MELHORADO: Biometric com mensagens claras
        private async Task BiometricLoginAsync()
        {
            var token = await _bio.TryAutoLoginAsync();
            if (token is null)
            {
                await GlobalToast.ShowAsync("Biometria não disponível", ToastTipo.Aviso);
                return;
            }

            try
            {
                // ✅ TODO: Validar token se necessário
                // var user = await DatabaseService.ValidateTokenAsync(token);
                
                await GlobalToast.ShowAsync("Autenticado!", ToastTipo.Sucesso);
                
                if (Shell.Current != null)
                    await Shell.Current.GoToAsync("///clients");
            }
            catch (Exception ex)
            {
                await GlobalErro.TratarErro(ex, mostrarAlerta: true);
                await _bio.DisableBiometricLoginAsync();
            }
        }

        private async Task ToggleBiometricAsync()
        {
            try
            {
                if (BiometricEnabled)
                {
                    await _bio.DisableBiometricLoginAsync();
                    await SecureStorage.Default.Remove("bio_enabled");
                    BiometricEnabled = false;
                    await GlobalToast.ShowAsync("Biometria desativada", ToastTipo.Info);
                }
                else
                {
                    if (BiometricAvailable)
                    {
                        BiometricEnabled = true;
                        await SecureStorage.Default.SetAsync("bio_enabled", "1");
                        await GlobalToast.ShowAsync("Biometria ativada", ToastTipo.Sucesso);
                    }
                    else
                    {
                        await GlobalToast.ShowAsync("Biometria não disponível", ToastTipo.Aviso);
                        BiometricEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                await GlobalErro.TratarErro(ex, mostrarAlerta: true);
            }
        }
    }
}
```

---

## 3. GlobalErro.cs - ADAPTAÇÃO PARA NAVIGEST.SHARED

### Mudanças Necessárias

**Problema**: Namespace precisa ser atualizado

**Antes**:
```csharp
namespace AppLoginMaui
{
    public static class GlobalErro { ... }
}
```

**Depois**:
```csharp
namespace NAVIGEST.Shared.Helpers
{
    public static class GlobalErro { ... }
}
```

**Outras adaptações** (mínimas):
- Adicionar método `MostrarErroAsync` como alias para `TratarErro`
- Verificar se `GlobalToast` está acessível

---

## 4. GlobalToast.cs - ADAPTAÇÃO PARA NAVIGEST.SHARED

### Mudanças Necessárias

**Problema**: Namespace precisa ser atualizado

**Antes**:
```csharp
namespace AppLoginMaui
{
    public static class GlobalToast { ... }
}
```

**Depois**:
```csharp
namespace NAVIGEST.Shared.Helpers
{
    public static class GlobalToast { ... }
}
```

---

## 5. DatabaseService.cs - MUDANÇA PARA INTERFACE

### Problema Original

**Código Original**:
```csharp
public static partial class DatabaseService
{
    // ... métodos estáticos ...
}
```

**Problema**: 
- Static class difícil de mockar
- Difícil de testar unitariamente
- Sem injeção de dependência

### ✅ SOLUÇÃO: Criar Interface + Implementação

**Ficheiro**: `NAVIGEST.Shared/Services/Database/IDatabaseService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAVIGEST.Shared.Models;

namespace NAVIGEST.Shared.Services
{
    public interface IDatabaseService
    {
        Task<bool> TestConnectionAsync();
        Task<(bool Ok, string? Nome, string? Tipo)> CheckLoginAsync(string username, string password);
        Task<List<Client>> GetClientsAsync(string userId);
        Task<bool> InsertClientAsync(Client client);
        Task<bool> UpdateClientAsync(Client client);
        Task<bool> DeleteClientAsync(int clientId);
        // ... outros métodos ...
    }
}
```

**Ficheiro**: `NAVIGEST.Shared/Services/Database/DatabaseService.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using NAVIGEST.Shared.Models;

namespace NAVIGEST.Shared.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly string _connectionString;
        private readonly IAppSettingsService _settingsService;

        public DatabaseService(IAppSettingsService settingsService)
        {
            _settingsService = settingsService;
            _connectionString = BuildConnectionString();
        }

        private string BuildConnectionString()
        {
            var s = _settingsService.Load();
            var builder = new MySqlConnectionStringBuilder
            {
                Server = s.Server,
                Port = s.Port,
                Database = s.Database,
                UserID = s.UserId,
                Password = s.Password ?? string.Empty,
                SslMode = s.SslMode,
                AllowPublicKeyRetrieval = s.AllowPublicKeyRetrieval,
                ConnectionTimeout = (uint)s.ConnectionTimeout,
                DefaultCommandTimeout = (uint)s.DefaultCommandTimeout
            };
            return builder.ConnectionString;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Connection error: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool Ok, string? Nome, string? Tipo)> CheckLoginAsync(string username, string password)
        {
            // ... resto do código igual, mas usando _connectionString ...
        }

        // ... outros métodos ...
    }
}
```

---

## 6. MauiProgram.cs - ORGANIZAR INJEÇÃO DE DEPENDÊNCIAS

### ❌ PROBLEMA ORIGINAL

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder()
            .UseMauiApp<App>()
            // ... 20 linhas de configuração ...
            .Services.AddSingleton<DatabaseService>()
            .Services.AddSingleton<EmailService>()
            // ... etc ...
        ;
    }
}
```

**Problema**: Tudo misturado, difícil de manter.

### ✅ SOLUÇÃO: Extension Methods

**Ficheiro**: `NAVIGEST.Shared/Extensions/ServiceCollectionExtensions.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using NAVIGEST.Shared.Services;
using NAVIGEST.Shared.Services.Auth;
using NAVIGEST.Shared.Helpers;

namespace NAVIGEST.Shared.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddNavigestServices(this IServiceCollection services)
        {
            // Database
            services.AddSingleton<IAppSettingsService, AppSettingsService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();

            // Auth
            services.AddSingleton<IBiometricAuthService, BiometricAuthService>();

            // Email
            services.AddSingleton<EmailService>();

            // Error handling
            services.AddSingleton<IErrorHandler, ModalErrorHandler>();

            return services;
        }

        public static IServiceCollection AddNavigestViewModels(this IServiceCollection services)
        {
            services.AddTransient<LoginViewModel>();
            services.AddTransient<ClientsViewModel>();
            services.AddTransient<MainYahViewModel>();
            return services;
        }

        public static IServiceCollection AddNavigestPages(this IServiceCollection services)
        {
            services.AddTransient<LoginPage>();
            services.AddTransient<ClientsPage>();
            services.AddTransient<MainYahPage>();
            return services;
        }
    }
}
```

**Ficheiro**: `NAVIGEST.iOS/MauiProgram.cs`

```csharp
using NAVIGEST.Shared.Extensions;

namespace NAVIGEST.iOS
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder()
                .UseMauiApp<App>()
                .ConfigureFonts(fonts => { /* ... */ })
                .Services
                .AddNavigestServices()      // ✅ Tudo de uma vez!
                .AddNavigestViewModels()
                .AddNavigestPages();

            return builder.Build();
        }
    }
}
```

---

## 📋 RESUMO DE CORREÇÕES

| Ficheiro | Problema | Solução |
|----------|----------|---------|
| `BiometricAuthService.cs` | `DisableBiometricLoginAsync` sem await | Usar `SecureStorage.Default` com await |
| `BiometricAuthService.cs` | Construtor `AuthenticationRequestConfiguration` errado | Usar property initialization |
| `LoginViewModel.cs` | Validação mock | Integrar `DatabaseService.CheckLoginAsync` real |
| `DatabaseService.cs` | Static class | Criar interface + implementação |
| `MauiProgram.cs` | Injeção de dependências misturada | Usar extension methods |
| `GlobalErro.cs` | Namespace `AppLoginMaui` | Mudar para `NAVIGEST.Shared.Helpers` |
| `GlobalToast.cs` | Namespace `AppLoginMaui` | Mudar para `NAVIGEST.Shared.Helpers` |

---

## 🎯 PRÓXIMOS PASSOS

1. Aplicar todas estas correções em NAVIGEST.Shared
2. Testar injeção de dependências
3. Testar DatabaseService com conexão real
4. Testar BiometricAuthService em device iOS
5. Testar LoginViewModel com validação real

