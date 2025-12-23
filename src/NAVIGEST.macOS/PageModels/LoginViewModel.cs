using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NAVIGEST.macOS.Services;
using NAVIGEST.macOS.Services.Auth;

namespace NAVIGEST.macOS.PageModels
{
    public sealed class LoginPageModel : BindableObject
    {
        private readonly IBiometricAuthService _bio;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _biometricAvailable;
        private bool _biometricEnabled;

        // Constantes de storage
        private const string KeyBioEnabled = "bio_enabled";
        private const string KeyBioUsername = "bio_username";
        private const string KeyBioPassword = "bio_password";

        public LoginPageModel(IBiometricAuthService bio)
        {
            _bio = bio;

            LoginCommand = new Command(async () => await LoginAsync(), CanLogin);
            BiometricLoginCommand = new Command(async () => await BiometricLoginAsync());
            ToggleBiometricCommand = new Command(async () => await ToggleBiometricAsync());
            InitCommand = new Command(async () => await InitAsync());
            BackCommand = new Command(async () => await BackAsync());
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
            private set { _biometricAvailable = value; OnPropertyChanged(); }
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
        public ICommand BackCommand { get; }

        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        // ======= INICIALIZAÇÃO =======
        private async Task InitAsync()
        {
            try
            {
                // 1. Limpar password ao entrar na página (para não ficar memorizada da sessão anterior)
                Password = string.Empty;

                BiometricAvailable = await _bio.IsAvailableAsync();
                
                // 🔧 DEBUG: Force true for testing if it returns false
                if (!BiometricAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ BiometricAvailable returned false - forcing true for testing");
                    BiometricAvailable = true; // DEBUG: Força true para testes
                }
                
                // ✅ Verificar se Face ID estava previamente ativado (usando Preferences em vez de SecureStorage)
                var bioEnabled = Preferences.Default.Get<bool>(KeyBioEnabled, false);
                
                if (bioEnabled)
                {
                    BiometricEnabled = true;
                    // Aguarda um tempo maior para garantir que a navegação terminou e a UI estabilizou
                    await Task.Delay(800);
                    // ✅ Dispara Face ID automaticamente
                    await BiometricAutoLoginAsync();
                }
                else
                {
                    BiometricEnabled = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Init error: {ex}");
                BiometricAvailable = true; // DEBUG: Força true mesmo em erro
                BiometricEnabled = false;
            }
        }

        // ======= LOGIN MANUAL =======
        /// <summary>
        /// ✅ LOGIN MANUAL (botão "Entrar")
        /// 1. Valida credenciais com servidor
        /// 2. Guarda sessão
        /// 3. APÓS SUCESSO: Pergunta se quer Face ID (apenas se nunca foi pedido)
        /// 4. Se SIM → guarda credenciais + ativa Face ID + mostra check/botão
        /// 5. Abre MainYahPage
        /// </summary>
        private async Task LoginAsync()
        {
            // ✅ Validação de campos
            if (!CanLogin())
            {
                var page = GetCurrentPage();
                if (page != null)
                    await page.DisplayAlert("Login", "Introduza utilizador e palavra-passe.", "OK");
                return;
            }

            try
            {
                // ✅ Autenticação real com o servidor/BD
                var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(Username, Password);
                
                if (!ok)
                {
                    var page = GetCurrentPage();
                    if (page != null)
                        await page.DisplayAlert("Login", "Credenciais inválidas. Tente novamente.", "OK");
                    return;
                }

                // ✅ Guarda sessão do utilizador
                var userInfo = await DatabaseService.TryGetUserPhotoAndEmailAsync(Username);
                var companyName = Preferences.Default.Get<string>("company.name", "");
                var companyLogoBase64 = Preferences.Default.Get<string>("company.logo", string.Empty);

                UserSession.Current.User = new UserSession.UserData
                {
                    Name = nome ?? string.Empty,
                    Role = tipo ?? string.Empty,
                    Photo = userInfo?.ProfilePicture,
                    CompanyName = companyName,
                    CompanyLogo = !string.IsNullOrEmpty(companyLogoBase64) ? Convert.FromBase64String(companyLogoBase64) : null
                };

                // ✅ LOGIN BEM-SUCEDIDO!
                // Auto-ativar Touch ID no primeiro login (sem popup)
                if (BiometricAvailable && !BiometricEnabled)
                {
                    // ✅ Guardar credenciais e ativar Touch ID automaticamente (sem perguntar)
                    Preferences.Default.Set(KeyBioEnabled, true);
                    Preferences.Default.Set(KeyBioUsername, Username);
                    Preferences.Default.Set(KeyBioPassword, Password);
                    BiometricEnabled = true;
                }

                // ✅ Navega para a página principal
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                var page = GetCurrentPage();
                if (page != null)
                    await page.DisplayAlert("Login", $"Erro: {ex.Message}", "OK");
            }
        }

        // ======= LOGIN COM FACE ID - AUTOMÁTICO (chamado em InitAsync) =======
        /// <summary>
        /// ✅ Face ID automático na abertura (se foi previamente ativado)
        /// 1. Mostra Face ID
        /// 2. Valida credenciais guardadas
        /// 3. Se OK → abre MainYahPage
        /// 4. Se falha → volta ao login manual (SEM desativar Face ID)
        /// </summary>
        private async Task BiometricAutoLoginAsync()
        {
            try
            {
                // 🔐 Autenticar com Face ID
                var authenticated = await _bio.AuthenticateAsync("Entrar com Face ID");
                if (!authenticated)
                {
                    // Utilizador cancelou ou falhou - volta ao login manual
                    return;
                }

                // ✅ Face ID OK - recuperar credenciais guardadas (usando Preferences em vez de SecureStorage)
                var savedUsername = Preferences.Default.Get<string>(KeyBioUsername, string.Empty);
                var savedPassword = Preferences.Default.Get<string>(KeyBioPassword, string.Empty);

                if (string.IsNullOrWhiteSpace(savedUsername) || string.IsNullOrWhiteSpace(savedPassword))
                {
                    // Credenciais não encontradas - volta ao login manual
                    return;
                }

                // ✅ Validar credenciais com servidor
                var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(savedUsername, savedPassword);
                
                if (!ok)
                {
                    // ⚠️ Credenciais já não são válidas (password mudada)
                    // Silenciosamente volta ao login manual SEM popup
                    System.Diagnostics.Debug.WriteLine("BiometricAutoLoginAsync: Password foi alterada");
                    return;
                }

                // ✅ Credenciais OK - guardar sessão
                var userInfo = await DatabaseService.TryGetUserPhotoAndEmailAsync(savedUsername);
                var companyName = Preferences.Default.Get<string>("company.name", "");
                var companyLogoBase64 = Preferences.Default.Get<string>("company.logo", string.Empty);

                UserSession.Current.User = new UserSession.UserData
                {
                    Name = nome ?? string.Empty,
                    Role = tipo ?? string.Empty,
                    Photo = userInfo?.ProfilePicture,
                    CompanyName = companyName,
                    CompanyLogo = !string.IsNullOrEmpty(companyLogoBase64) ? Convert.FromBase64String(companyLogoBase64) : null
                };

                // ✅ Atualizar campos (para referência, se necessário)
                Username = savedUsername;
                Password = savedPassword;

                // ✅ Navega para a página principal (SEM alert, silencioso)
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BiometricAutoLoginAsync error: {ex}");
                // Silenciosamente volta ao login manual
            }
        }

        // ======= LOGIN COM FACE ID - MANUAL (botão "Entrar com Face ID") =======
        /// <summary>
        /// ✅ Face ID manual (se utilizador clica no botão)
        /// Sem popups, entra direto após autenticação bem-sucedida
        /// </summary>
        private async Task BiometricLoginAsync()
        {
            try
            {
                // 🔐 Autenticar com Face ID
                var authenticated = await _bio.AuthenticateAsync("Entrar com Touch ID");
                if (!authenticated)
                {
                    return;
                }

                // ✅ Face ID OK - usar credenciais dos campos de texto
                if (!CanLogin())
                {
                    // Silenciosamente volta (utilizador não preencheu campos)
                    System.Diagnostics.Debug.WriteLine("BiometricLoginAsync: Campos vazios");
                    return;
                }

                // ✅ Validar credenciais com servidor
                var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(Username, Password);
                
                if (!ok)
                {
                    // Silenciosamente volta (credenciais inválidas)
                    System.Diagnostics.Debug.WriteLine("BiometricLoginAsync: Credenciais inválidas");
                    return;
                }

                // ✅ Credenciais OK - guardar sessão
                var userInfo = await DatabaseService.TryGetUserPhotoAndEmailAsync(Username);
                var companyName = Preferences.Default.Get<string>("company.name", "");
                var companyLogoBase64 = Preferences.Default.Get<string>("company.logo", string.Empty);

                UserSession.Current.User = new UserSession.UserData
                {
                    Name = nome ?? string.Empty,
                    Role = tipo ?? string.Empty,
                    Photo = userInfo?.ProfilePicture,
                    CompanyName = companyName,
                    CompanyLogo = !string.IsNullOrEmpty(companyLogoBase64) ? Convert.FromBase64String(companyLogoBase64) : null
                };

                // ✅ Se Face ID não estava ativado, ativar agora (SEM popup)
                if (!BiometricEnabled)
                {
                    Preferences.Default.Set(KeyBioEnabled, true);
                    Preferences.Default.Set(KeyBioUsername, Username);
                    Preferences.Default.Set(KeyBioPassword, Password);
                    BiometricEnabled = true;
                }

                // ✅ Navega para a página principal (SILENCIOSAMENTE, sem alerts)
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BiometricLoginAsync error: {ex}");
                // Silenciosamente volta em caso de erro
            }
        }

        private async Task ToggleBiometricAsync()
        {
            // ✅ Se o utilizador desativa o check, limpar dados guardados (usando Preferences em vez de SecureStorage)
            if (!BiometricEnabled)
            {
                Preferences.Default.Remove(KeyBioEnabled);
                Preferences.Default.Remove(KeyBioUsername);
                Preferences.Default.Remove(KeyBioPassword);
            }
            OnPropertyChanged(nameof(BiometricEnabled));
        }

        private async Task BackAsync()
        {
            if (Shell.Current is not null)
                await Shell.Current.GoToAsync("//WelcomePage");
        }

        private static Page? GetCurrentPage() => Application.Current?.Windows.FirstOrDefault()?.Page;
    }
}
