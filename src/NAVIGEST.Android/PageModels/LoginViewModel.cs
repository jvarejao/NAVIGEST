using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NAVIGEST.Android.Services;
using NAVIGEST.Android.Services.Auth;
using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.Android.PageModels
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

        private bool CanLogin() =>
            !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password);

        // ======= INICIALIZAÇÃO =======
        private async Task InitAsync()
        {
            try
            {
                BiometricAvailable = await _bio.IsAvailableAsync();
                
                // 🔧 DEBUG: Force true for testing if it returns false
                if (!BiometricAvailable)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ BiometricAvailable returned false - forcing true for testing");
                    BiometricAvailable = true; // DEBUG: Força true para testes
                }
                
                // ✅ Verificar se Face ID estava previamente ativado
                var bioEnabled = (await SecureStorage.GetAsync(KeyBioEnabled)) == "1";
                
                if (bioEnabled)
                {
                    BiometricEnabled = true;
                    // Aguarda um tick para estabilizar a UI
                    await Task.Delay(200);
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
                    await page.DisplayAlert(AppResources.Login_Title, AppResources.Login_Msg_EnterUserAndPass, AppResources.Common_OK);
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
                        await page.DisplayAlert(AppResources.Login_Title, AppResources.Login_Msg_InvalidCredentials, AppResources.Common_OK);
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
                // Agora, se Face ID nunca foi ativado, perguntar
                if (BiometricAvailable && !BiometricEnabled)
                {
                    var page = GetCurrentPage();
                    var response = page != null
                        ? await page.DisplayAlert(
                            AppResources.Login_Biometric,
                            AppResources.Login_UseBiometric + "?",
                            AppResources.Common_Yes,
                            AppResources.Common_No)
                        : false;

                    if (response)
                    {
                        // ✅ Guardar credenciais e ativar Face ID
                        await SecureStorage.SetAsync(KeyBioEnabled, "1");
                        await SecureStorage.SetAsync(KeyBioUsername, Username ?? string.Empty);
                        await SecureStorage.SetAsync(KeyBioPassword, Password ?? string.Empty);
                        BiometricEnabled = true;
                    }
                    // Se responde NÃO, próxima vez volta a perguntar
                }

                // ✅ Navega para a página principal
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                var page = GetCurrentPage();
                if (page != null)
                    await page.DisplayAlert(AppResources.Login_Title, string.Format(AppResources.Login_Msg_LoginError, ex.Message), AppResources.Common_OK);
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
                var authenticated = await _bio.AuthenticateAsync(AppResources.Login_Biometric);
                if (!authenticated)
                {
                    // Utilizador cancelou ou falhou - volta ao login manual
                    return;
                }

                // ✅ Face ID OK - recuperar credenciais guardadas
                var savedUsername = await SecureStorage.GetAsync(KeyBioUsername);
                var savedPassword = await SecureStorage.GetAsync(KeyBioPassword);

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
                    var page = GetCurrentPage();
                    if (page != null)
                        await page.DisplayAlert(
                            AppResources.Common_Info,
                            AppResources.Login_Msg_InvalidCredentials,
                            AppResources.Common_OK
                        );
                    // ✅ NÃO desativa Face ID, apenas volta ao login manual
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
                Username = savedUsername ?? string.Empty;
                Password = savedPassword ?? string.Empty;

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
        /// Este é menos utilizado, pois o automático já funciona
        /// </summary>
        private async Task BiometricLoginAsync()
        {
            try
            {
                // 🔐 Autenticar com Face ID
                var authenticated = await _bio.AuthenticateAsync(AppResources.Login_Biometric);
                if (!authenticated)
                {
                    return;
                }

                // ✅ Face ID OK - usar credenciais dos campos de texto
                if (!CanLogin())
                {
                    var page = GetCurrentPage();
                    if (page != null)
                        await page.DisplayAlert(
                            AppResources.Common_Error,
                            AppResources.Login_Msg_EnterUserAndPass,
                            AppResources.Common_OK
                        );
                    return;
                }

                // ✅ Validar credenciais com servidor
                var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(Username, Password);
                
                if (!ok)
                {
                    var page = GetCurrentPage();
                    if (page != null)
                        await page.DisplayAlert(
                            AppResources.Common_Error,
                            AppResources.Login_Msg_InvalidCredentials,
                            AppResources.Common_OK
                        );
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

                // ✅ Se Face ID não estava ativado, ativar agora
                if (!BiometricEnabled)
                {
                    await SecureStorage.SetAsync(KeyBioEnabled, "1");
                    await SecureStorage.SetAsync(KeyBioUsername, Username ?? string.Empty);
                    await SecureStorage.SetAsync(KeyBioPassword, Password ?? string.Empty);
                    BiometricEnabled = true;
                }

                // ✅ Navega para a página principal
                if (Shell.Current is not null)
                    await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BiometricLoginAsync error: {ex}");
                var page = GetCurrentPage();
                if (page != null)
                    await page.DisplayAlert(AppResources.Common_Error, string.Format(AppResources.Login_Msg_LoginError, ex.Message), AppResources.Common_OK);
            }
        }

        private static Page? GetCurrentPage() => Application.Current?.Windows.FirstOrDefault()?.Page;

        private async Task ToggleBiometricAsync()
        {
            // ✅ Se o utilizador desativa o check, limpar dados guardados
            if (!BiometricEnabled)
            {
                await SecureStorage.SetAsync(KeyBioEnabled, "0");
                SecureStorage.Remove(KeyBioUsername);
                SecureStorage.Remove(KeyBioPassword);
            }
            OnPropertyChanged(nameof(BiometricEnabled));
        }
    }
}
