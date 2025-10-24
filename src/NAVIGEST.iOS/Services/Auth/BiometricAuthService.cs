using System.Threading.Tasks;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;
using Microsoft.Maui.Storage;

namespace NAVIGEST.iOS.Services.Auth
{
    public interface IBiometricAuthService
    {
        Task<bool> IsAvailableAsync();
        Task<bool> AuthenticateAsync(string reason = "Autenticar com biometria");
        Task EnableBiometricLoginAsync(string userIdOrToken);
        Task<string?> TryAutoLoginAsync();
        Task DisableBiometricLoginAsync();
    }

    public class BiometricAuthService : IBiometricAuthService
    {
        private const string KeyBiometricEnabled = "bio_enabled";
        private const string KeyBioToken = "bio_token";

        public async Task<bool> IsAvailableAsync()
        {
            var isAvailable = await CrossFingerprint.Current.IsAvailableAsync(true);
            return isAvailable;
        }

        public async Task<bool> AuthenticateAsync(string reason = "Autenticar com biometria")
        {
            var request = new AuthenticationRequestConfiguration("Segurança", reason)
            {
                // iOS: usa Face ID / Touch ID conforme device
                FallbackTitle = "Usar código",
                CancelTitle = "Cancelar",
                // Android: este label aparece no prompt
                AllowAlternativeAuthentication = false
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(request);
            return result.Authenticated;
        }

        // Guarda o “token” que te permita reabrir sessão (JWT, refresh token ou userId)
        public async Task EnableBiometricLoginAsync(string userIdOrToken)
        {
            await SecureStorage.SetAsync(KeyBiometricEnabled, "1");
            await SecureStorage.SetAsync(KeyBioToken, userIdOrToken);
        }

        // Se biometria OK, devolve o token guardado para restaurar sessão
        public async Task<string?> TryAutoLoginAsync()
        {
            var enabled = await SecureStorage.GetAsync(KeyBiometricEnabled);
            if (enabled != "1")
                return null;

            var ok = await AuthenticateAsync("Entrar com Face ID/biometria");
            if (!ok) return null;

            return await SecureStorage.GetAsync(KeyBioToken);
        }

        public async Task DisableBiometricLoginAsync()
        {
            SecureStorage.Remove(KeyBiometricEnabled);
            SecureStorage.Remove(KeyBioToken);
            await Task.CompletedTask;
        }
    }
}
