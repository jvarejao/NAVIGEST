using System.Threading.Tasks;
using Microsoft.Maui.Storage;

#if MACCATALYST
using Foundation;
using LocalAuthentication;
#endif

namespace NAVIGEST.macOS.Services.Auth
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
#if MACCATALYST
            try
            {
                var context = new LAContext();
                var policy = LAPolicy.DeviceOwnerAuthenticationWithBiometrics;
                
                NSError? error = null;
                bool canEvaluate = context.CanEvaluatePolicy(policy, out error);
                
                System.Diagnostics.Debug.WriteLine($"[MACOS] LAContext.CanEvaluatePolicy: {canEvaluate}, Error: {error?.LocalizedDescription}");
                System.Diagnostics.Debug.WriteLine($"[MACOS] BiometryType: {context.BiometryType}");
                
                return canEvaluate;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MACOS] IsAvailableAsync error: {ex}");
                return false;
            }
#else
            return false;
#endif
        }

        public async Task<bool> AuthenticateAsync(string reason = "Autenticar com biometria")
        {
#if MACCATALYST
            try
            {
                var context = new LAContext();
                var policy = LAPolicy.DeviceOwnerAuthenticationWithBiometrics;
                
                NSError? error = null;
                
                if (!context.CanEvaluatePolicy(policy, out error))
                {
                    System.Diagnostics.Debug.WriteLine($"[MACOS] Cannot evaluate policy: {error?.LocalizedDescription}");
                    return false;
                }

                // Mostra o prompt de autenticação
                var tcs = new TaskCompletionSource<bool>();
                
                context.EvaluatePolicy(policy, reason, (success, evalError) =>
                {
                    if (success)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MACOS] Authentication successful!");
                        tcs.SetResult(true);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[MACOS] Authentication failed: {evalError?.LocalizedDescription}");
                        tcs.SetResult(false);
                    }
                });

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MACOS] AuthenticateAsync error: {ex}");
                return false;
            }
#else
            return false;
#endif
        }

        public async Task EnableBiometricLoginAsync(string userIdOrToken)
        {
            // No macOS/Catalyst, não usar SecureStorage (Keychain) para evitar MissingEntitlement
            // Usar Preferences.Default para armazenar credenciais
            Preferences.Default.Set(KeyBiometricEnabled, true);
            Preferences.Default.Set(KeyBioToken, userIdOrToken);
            System.Diagnostics.Debug.WriteLine($"[MACOS] Biometric login enabled");
        }

        public async Task<string?> TryAutoLoginAsync()
        {
            // No macOS/Catalyst, não usar SecureStorage (Keychain) para evitar MissingEntitlement
            var enabled = Preferences.Default.Get<bool>(KeyBiometricEnabled, false);
            if (!enabled)
            {
                System.Diagnostics.Debug.WriteLine($"[MACOS] Biometric not enabled");
                return null;
            }

            var ok = await AuthenticateAsync("Entrar com Touch ID");
            if (!ok)
            {
                System.Diagnostics.Debug.WriteLine($"[MACOS] Auto-login biometric failed");
                return null;
            }

            var token = Preferences.Default.Get<string>(KeyBioToken, string.Empty);
            System.Diagnostics.Debug.WriteLine($"[MACOS] Auto-login successful, returning token");
            return token;
        }

        public async Task DisableBiometricLoginAsync()
        {
            // No macOS/Catalyst, não usar SecureStorage (Keychain) para evitar MissingEntitlement
            Preferences.Default.Remove(KeyBiometricEnabled);
            Preferences.Default.Remove(KeyBioToken);
            System.Diagnostics.Debug.WriteLine($"[MACOS] Biometric login disabled");
            await Task.CompletedTask;
        }
    }
}
