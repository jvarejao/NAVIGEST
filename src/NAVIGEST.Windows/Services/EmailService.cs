using System;
using System.Linq;
    using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NAVIGEST.macOS.Services
{
    /// <summary>
    /// Serviço de envio de e-mail via SMTP (MailKit).
    /// ATENÇÃO: Remover credenciais hardcoded antes de publicar o código (usar SecureStorage / KeyChain / KeyStore).
    /// </summary>
    public static class EmailService
    {
        // ======================= CONFIG =======================
        // Ajusta conforme o teu servidor.
        // Se o servidor suportar STARTTLS fiável, preferir Porta=587 + UseSsl=false (mais compatível).
        private const string SmtpHost = "mail.yahpublicidade.com";
        private const int SmtpPort = 465;          // 465 (SSL direto) ou 587 (STARTTLS)
        private const bool UseSsl = true;          // true = SslOnConnect (465); false = StartTls (587)

        private const string SmtpUser = "comercial@yahpublicidade.com";
        private const string SmtpPass = "#JONy22442208"; // <-- mover para armazenamento seguro / não versionar

        private const string FromMail = "comercial@yahpublicidade.com";
        private const string FromName = "YAH Publicidade";
        // ======================================================

        /// <summary>
        /// Envia e-mail de reset de password com código.
        /// </summary>
        public static async Task SendResetEmailAsync(string toEmail, string username, string token)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(FromName, FromMail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Código de recuperação de password";

            message.Body = new TextPart("plain")
            {
                Text =
$@"Olá {username},

Recebemos um pedido para recuperar a tua password.

Código: {token}

Este código é válido por 15 minutos.
Abre a app, escreve o teu utilizador e clica em 'Tenho um código do e-mail' para definires a nova password.

Se não foste tu, ignora este e-mail.

— YAH Publicidade"
            };

            using var client = new SmtpClient();

            // (1) Afrouxar validação apenas em Android/iOS quando o ÚNICO problema é revogação/OCSP indisponível.
            client.ServerCertificateValidationCallback = CertificateValidationRelaxedForMobile;

#if ANDROID || IOS
            // Evita que o MailKit tente forçar verificação de revogação (que falha com muitos servidores sem OCSP/CRL acessível a partir do dispositivo).
            client.CheckCertificateRevocation = false;
#endif

            if (UseSsl)
            {
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.SslOnConnect)
                            .ConfigureAwait(false);
            }
            else
            {
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls)
                            .ConfigureAwait(false);
            }

            await client.AuthenticateAsync(SmtpUser, SmtpPass).ConfigureAwait(false);
            await client.SendAsync(message).ConfigureAwait(false);
            await client.DisconnectAsync(true).ConfigureAwait(false);
        }

        /// <summary>
        /// Exemplo de envio simples (teste).
        /// </summary>
        public static Task SendTestAsync(string toEmail)
            => SendResetEmailAsync(toEmail, "Teste", "123456");

        /// <summary>
        /// Callback de validação de certificado.
        /// Em Windows mantém validação normal.
        /// Em Android/iOS: aceita se os únicos erros forem de revogação/OCSP (cadeia senão válida).
        /// </summary>
        private static bool CertificateValidationRelaxedForMobile(object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            try
            {
                // Sem erros → OK
                if (sslPolicyErrors == SslPolicyErrors.None)
                    return true;

#if ANDROID || IOS
                // Se houver cadeia e os únicos problemas forem relativos a revogação/OCSP desconhecida, aceitar.
                if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0 && chain != null)
                {
                    // Se qualquer erro for diferente destes, rejeita.
                    // Permitidos: RevocationStatusUnknown, OfflineRevocation, NoError.
                    bool onlyRevocationIssues = chain.ChainStatus.All(st =>
                        st.Status == X509ChainStatusFlags.NoError ||
                        st.Status == X509ChainStatusFlags.RevocationStatusUnknown ||
                        st.Status == X509ChainStatusFlags.OfflineRevocation);

                    if (onlyRevocationIssues)
                        return true;
                }
#endif
                // Qualquer outra situação: mantém rejeição para não abrir brecha de segurança.
                return false;
            }
            catch
            {
                // Em caso de exceção volta a rejeitar (fail closed).
                return false;
            }
        }
    }
}

