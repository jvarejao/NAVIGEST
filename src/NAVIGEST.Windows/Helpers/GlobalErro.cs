// Helpers/GlobalErro.cs (versão melhorada com supressão, ActionSheet e utilidades)
using System;
using System.IO;
using System.Runtime.CompilerServices; // Caller info
using System.Text;
using Microsoft.Maui.ApplicationModel; // MainThread, Device info
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage; // FileSystem
using Microsoft.Maui.ApplicationModel.DataTransfer; // Share

namespace NAVIGEST.macOS
{
    public static class GlobalErro
    {
        private static readonly object _sync = new();
        private static readonly string _logDir = Path.Combine(FileSystem.AppDataDirectory, "logs");
        private static readonly string _logFile = Path.Combine(_logDir, "app.log");
        private const long MaxLogSizeBytes = 1_000_000; // 1MB ~1MB por ficheiro
        private static string? _lastMsg;
        private static DateTime _lastMsgTime;
        private static bool _envLogged;

        /// <summary>
        /// Regista e (opcionalmente) mostra erro ao utilizador.
        /// </summary>
        public static void TratarErro(Exception ex, bool mostrarAlerta = true,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
#if ANDROID
            if (ex is Android.Content.Res.Resources.NotFoundException)
            {
                AppShell.DisplayToastAsync("Erro de recurso Android. Por favor, atualize ou reinstale a app.", ToastTipo.Erro, 2500);
                System.Diagnostics.Debug.WriteLine("Resources.NotFoundException: " + ex.Message);
                return;
            }
#endif
            try
            {
                var full = ex.ToString();
                var shortMsg = ex.Message;
                // Log completo
                var sb = new StringBuilder();
                sb.Append('[').Append(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"))
                  .Append("Z] ").Append(ex.GetType().Name).Append(": ").AppendLine(ex.Message)
                  .Append("Origem: ").Append(Path.GetFileName(file)).Append(':').Append(line).Append(" (").Append(member).AppendLine(")")
                  .AppendLine("Stack:").AppendLine(full).AppendLine("----");
                GravarLog(sb.ToString());

                if (!mostrarAlerta)
                    return;

                var now = DateTime.UtcNow;
                if (_lastMsg == shortMsg && (now - _lastMsgTime).TotalSeconds < 3)
                    return;
                _lastMsg = shortMsg; _lastMsgTime = now;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        var page = Application.Current?.Windows?.FirstOrDefault()?.Page;
                        if (page == null) return;

                        string preview = shortMsg;
                        const int maxPreview = 260;
                        if (preview.Length > maxPreview)
                            preview = preview.Substring(0, maxPreview) + "…";

                        string cancel = "OK";
                        string btnAbrir = "Abrir Logs";
                        string btnCopiar = "Copiar Detalhes";
                        string btnPartilhar = "Partilhar Log";
                        var escolha = await page.DisplayActionSheet(preview, cancel, null, btnAbrir, btnCopiar, btnPartilhar);
                        if (escolha == btnAbrir)
                        {
                            await GlobalErro.AbrirOuPartilharLogsAsync(true);
                        }
                        else if (escolha == btnCopiar)
                        {
                            try { await Clipboard.Default.SetTextAsync(full); } catch { }
                            await page.DisplayAlert("Copiado", "Detalhes copiados para a área de transferência.", "OK");
                        }
                        else if (escolha == btnPartilhar)
                        {
                            await GlobalErro.AbrirOuPartilharLogsAsync(false);
                        }
                    }
                    catch { }
                });
            }
            catch { }
        }

        /// <summary>Regista mensagem arbitrária.</summary>
        public static void LogMensagem(string mensagem,
            [CallerMemberName] string member = "",
            [CallerFilePath] string file = "",
            [CallerLineNumber] int line = 0)
        {
            try
            {
                GravarLog($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}Z] MSG: {mensagem}\nOrigem: {Path.GetFileName(file)}:{line} ({member})\n----\n");
            }
            catch { }
        }

        public static string ObterCaminhoLog() => _logFile;
        public static string LerUltimasLinhas(int maxLinhas = 200)
        {
            try
            {
                if (!File.Exists(_logFile)) return string.Empty;
                var todas = File.ReadAllLines(_logFile);
                if (todas.Length <= maxLinhas) return string.Join(Environment.NewLine, todas);
                return string.Join(Environment.NewLine, todas[^maxLinhas..]);
            }
            catch { return string.Empty; }
        }

        /// <summary>Força rotação manual (rename atual).</summary>
        public static void RotacionarLog()
        {
            try
            {
                if (!File.Exists(_logFile)) return;
                var backupName = $"app-manual-{DateTime.UtcNow:yyyyMMddHHmmss}.log";
                var backupPath = Path.Combine(_logDir, backupName);
                File.Move(_logFile, backupPath, true);
            }
            catch { }
        }

        private static void GravarLog(string texto)
        {
            try
            {
                lock (_sync)
                {
                    if (!Directory.Exists(_logDir)) Directory.CreateDirectory(_logDir);

                    // Info de ambiente apenas 1 vez
                    if (!_envLogged)
                    {
                        _envLogged = true;
                        var env = new StringBuilder();
                        env.AppendLine("==== Ambiente ====")
                           .AppendLine($"Device: {DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}")
                           .AppendLine($"OS: {DeviceInfo.Current.Platform} {DeviceInfo.Current.VersionString}")
                           .AppendLine($"App: {AppInfo.Current.Name} v{AppInfo.Current.VersionString}")
                           .AppendLine("==================");
                        File.AppendAllText(_logFile, env.ToString());
                    }

                    if (File.Exists(_logFile))
                    {
                        var info = new FileInfo(_logFile);
                        if (info.Length > MaxLogSizeBytes)
                        {
                            var backupName = $"app-{DateTime.UtcNow:yyyyMMddHHmmss}.log";
                            var backupPath = Path.Combine(_logDir, backupName);
                            File.Move(_logFile, backupPath, true);
                        }
                    }

                    File.AppendAllText(_logFile, texto);
                }
            }
            catch { }
        }

        public static async Task AbrirOuPartilharLogsAsync(bool abrirDireto)
        {
            try
            {
                if (!Directory.Exists(_logDir)) return;
#if WINDOWS || MACCATALYST
                if (abrirDireto)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = _logDir,
                            UseShellExecute = true,
                            Verb = "open"
                        });
                    }
                    catch { }
                }
                else
                {
                    if (File.Exists(_logFile))
                    {
                        await Share.Default.RequestAsync(new ShareFileRequest
                        {
                            Title = "Log da aplicação",
                            File = new ShareFile(_logFile)
                        });
                    }
                }
#elif ANDROID || IOS
                if (File.Exists(_logFile))
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Log da aplicação",
                        File = new ShareFile(_logFile)
                    });
                }
                else if (!abrirDireto)
                {
                    await Share.Default.RequestAsync(new ShareTextRequest
                    {
                        Title = "Log da aplicação",
                        Text = "Ainda não existe ficheiro de log."
                    });
                }
#endif
            }
            catch { }
        }
    }
}

