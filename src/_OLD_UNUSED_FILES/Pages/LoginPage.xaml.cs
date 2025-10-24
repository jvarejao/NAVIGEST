﻿using AppLoginMaui.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Animations;
using Microsoft.Extensions.DependencyInjection; // no topo

namespace AppLoginMaui.Pages
{
    public partial class LoginPage : ContentPage
    {
        private const string PrefRemember = "remember_username";
        private const string PrefUsername = "last_username";
        private const string AdminResetKey = "YAH-RESET-2024";

        private string? _currentUserEmail;
        private byte[]? _currentUserPhoto;

        private FontImageSource? _eyeIcon;
        private FontImageSource? _eyeSlashIcon;
        private bool _showingPwd = false;

        private int _usernameLookupSeq = 0;

        public LoginPage()
        {
            InitializeComponent();
            PrepareEyeIcons();
            AnimateCardEntry();

            BindingContext = Application.Current?
            .Handler?.MauiContext?.Services
            .GetRequiredService<AppLoginMaui.PageModels.LoginPageModel>();

            chkRemember.IsChecked = true;
            // Avatar padrão por plataforma
#if WINDOWS
            LoginAvatarImage.Source = "user_placeholder_windows.png";
#elif ANDROID
            LoginAvatarImage.Source = "user_placeholder_android.png";
#elif IOS
            LoginAvatarImage.Source = "user_placeholder_ios.png";
#else
            LoginAvatarImage.Source = "user_placeholder_windows.png"; // fallback
#endif
        }

        private void AnimateCardEntry()
        {
            // Animação suave do card ao entrar
            LoginCard.Opacity = 0;
            LoginCard.Scale = 0.92;
            LoginCard.FadeTo(1, 420, Easing.CubicOut);
            LoginCard.ScaleTo(1, 420, Easing.CubicOut);
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ✅ Inicializa tudo (biometria, auto-login, etc)
            // O InitCommand vai fazer TODO o trabalho: verificar bio_enabled, chamar Face ID se necessário
            var vm = BindingContext as AppLoginMaui.PageModels.LoginPageModel;
            vm?.InitCommand.Execute(null);

            // lembrar utilizador
            var remember = Preferences.Default.Get<bool>(PrefRemember, true); // default true
            chkRemember.IsChecked = remember;
            if (remember)
                entryUsername.Text = Preferences.Default.Get<string>(PrefUsername, string.Empty);

            // avatar por plataforma quando não há utilizador lembrado
            if (!remember || string.IsNullOrWhiteSpace(entryUsername.Text))
            {
#if WINDOWS
        LoginAvatarImage.Source = "user_placeholder_windows.png";
#elif ANDROID
                LoginAvatarImage.Source = "user_placeholder_android.png";
#elif IOS
        LoginAvatarImage.Source = "user_placeholder_ios.png";
#else
        LoginAvatarImage.Source = "user_placeholder_windows.png"; // fallback
#endif
            }
        }


        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            double w = this.Width;
            if (w <= 0) return;
            double max = 520;
            double min = 320;
            LoginContainer.WidthRequest = Math.Max(min, Math.Min(w - 40, max));
        }

        private void OnAvatarSizeChanged(object sender, EventArgs e)
        {
            double size = Math.Min(AvatarContainer.Width, AvatarContainer.Height);
            if (size <= 0) return;

            LoginAvatarImage.WidthRequest = size;
            LoginAvatarImage.HeightRequest = size;
            LoginAvatarImage.Clip = new EllipseGeometry
            {
                Center = new Point(size / 2, size / 2),
                RadiusX = size / 2,
                RadiusY = size / 2
            };
        }

        private void OnClearUserClicked(object sender, EventArgs e)
        {
            entryUsername.Text = string.Empty;
        }

        private async void OnUsernameChanged(object sender, TextChangedEventArgs e)
        {
            var query = entryUsername.Text?.Trim() ?? string.Empty;
            btnClearUser.IsVisible = query.Length > 0;

            int mySeq = ++_usernameLookupSeq;

            lblSub.Text = string.IsNullOrWhiteSpace(query)
                ? "Inicia sessão para continuar"
                : "A procurar utilizador…";

            _currentUserEmail = null;
            _currentUserPhoto = null;
            LoginAvatarImage.Source = "user_placeholder.png";
            HideUserFoundLabels();

            if (string.IsNullOrWhiteSpace(query))
                return;

            await Task.Delay(200);
            if (mySeq != _usernameLookupSeq) return;

            try
            {
                var info = await DatabaseService.TryGetUserPhotoAndEmailAsync(query);

                if (mySeq != _usernameLookupSeq || (entryUsername.Text?.Trim() ?? "") != query)
                    return;

                if (info is null)
                {
                    lblSub.Text = "Utilizador não encontrado";
                    LoginAvatarImage.Source = "user_placeholder.png";
                    HideUserFoundLabels();
                    return;
                }

                _currentUserEmail = info.Email;
                _currentUserPhoto = info.ProfilePicture;

                LoginAvatarImage.Source = (_currentUserPhoto is { Length: > 0 })
                    ? ImageSource.FromStream(() => new MemoryStream(_currentUserPhoto))
                    : "user_placeholder.png";

                lblSub.Text = "Inicia sessão para continuar";

                if (string.IsNullOrWhiteSpace(info.Email))
                    ShowUserFoundLabels(null);
                else
                    ShowUserFoundLabels(info.Email);
            }
            catch
            {
                if (mySeq != _usernameLookupSeq) return;
                lblSub.Text = "Inicia sessão para continuar";
                LoginAvatarImage.Source = "user_placeholder.png";
                HideUserFoundLabels();
            }
            finally
            {
                OnAvatarSizeChanged(null!, EventArgs.Empty);
            }
        }

        private void ShowUserFoundLabels(string? email)
        {
            lblUserFound.Text = "Utilizador encontrado";
            lblUserFound.IsVisible = true;

            if (string.IsNullOrWhiteSpace(email))
            {
                lblEmailInfo.IsVisible = false;
                lblEmailInfo.Text = "";
            }
            else
            {
                lblEmailInfo.Text = email;
                lblEmailInfo.IsVisible = true;
            }
        }

        private void HideUserFoundLabels()
        {
            lblUserFound.IsVisible = false;
            lblEmailInfo.IsVisible = false;
            lblUserFound.Text = "";
            lblEmailInfo.Text = "";
        }

        private void PrepareEyeIcons()
        {
            _eyeIcon = new FontImageSource
            {
                Glyph = "\uf06e",
                FontFamily = "FA7Solid",
                Color = Color.FromArgb("#6B7280"),
                Size = 18
            };
            _eyeSlashIcon = new FontImageSource
            {
                Glyph = "\uf070",
                FontFamily = "FA7Solid",
                Color = Color.FromArgb("#6B7280"),
                Size = 18
            };

            entryPassword.IsPassword = true;
            btnToggle.ImageSource = _eyeIcon;
        }

        private void OnTogglePasswordClicked(object sender, EventArgs e)
        {
            _showingPwd = !_showingPwd;
            entryPassword.IsPassword = !_showingPwd;
            btnToggle.ImageSource = _showingPwd ? _eyeSlashIcon : _eyeIcon;
        }

        private void OnEntryCompleted(object sender, EventArgs e)
            => OnLoginClicked(btnLogin, EventArgs.Empty);

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            HideMsg();

            var user = entryUsername.Text?.Trim() ?? "";
            var pass = entryPassword.Text ?? "";

            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                ShowMsg("Indica utilizador e palavra-passe.", false);
                return;
            }

            try
            {
                SetBusy(true);

                var (ok, nome, tipo) = await DatabaseService.CheckLoginAsync(user, pass);

                if (!ok)
                {
                    ShowMsg("Credenciais inválidas.", false);
                    return;
                }

                var userInfo = await DatabaseService.TryGetUserPhotoAndEmailAsync(user);
                var companyName = Preferences.Default.Get<string>("company.name", "");
                var companyLogoBase64 = Preferences.Default.Get<string>("company.logo", string.Empty);

                UserSession.Current.User = new UserSession.UserData
                {
                    Name = nome,
                    Role = tipo,
                    Photo = userInfo?.ProfilePicture,
                    CompanyName = companyName,
                    CompanyLogo = !string.IsNullOrEmpty(companyLogoBase64) ? Convert.FromBase64String(companyLogoBase64) : null
                };

                if (chkRemember.IsChecked)
                {
                    Preferences.Set(PrefRemember, true);
                    Preferences.Set(PrefUsername, user);
                }
                else
                {
                    Preferences.Set(PrefRemember, false);
                    Preferences.Remove(PrefUsername);
                }

                ShowMsg($"Bem-vindo{(string.IsNullOrWhiteSpace(nome) ? "" : $", {nome}")}!", true);

                await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                ShowMsg("Erro no login: " + ex.Message, false);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async void OnHaveCodeTapped(object sender, EventArgs e)
        {
            var u = entryUsername.Text?.Trim();
            if (string.IsNullOrWhiteSpace(u))
            {
                await DisplayAlert("Recuperação", "Escreve primeiro o nome de utilizador.", "OK");
                return;
            }

            var code = await DisplayPromptAsync("Código do e-mail",
                "Introduz o código de 6 dígitos recebido:",
                accept: "OK", cancel: "Cancelar",
                placeholder: "000000", maxLength: 6, keyboard: Keyboard.Numeric);

            if (string.IsNullOrWhiteSpace(code))
                return;

            var newPass = await PromptMaskedPasswordAsync("Nova palavra-passe", "Introduz a nova password:");
            if (string.IsNullOrWhiteSpace(newPass))
                return;

            try
            {
                var ok = await DatabaseService.ValidateTokenAndResetPasswordAsync(u, code.Trim(), newPass.Trim());
                await DisplayAlert("Recuperação",
                    ok ? "Password alterada com sucesso." : "Código inválido ou expirado.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Falha no reset: " + ex.Message, "OK");
            }
        }

        private async void OnForgotByEmailTapped(object sender, EventArgs e)
        {
            var u = entryUsername.Text?.Trim();
            if (string.IsNullOrWhiteSpace(u))
            {
                await DisplayAlert("Recuperação", "Escreve primeiro o nome de utilizador.", "OK");
                return;
            }

            try
            {
                var info = await DatabaseService.TryGetUserPhotoAndEmailAsync(u);
                if (info is null || string.IsNullOrWhiteSpace(info.Email))
                {
                    await DisplayAlert("Recuperação", "Não existe e-mail associado a este utilizador.", "OK");
                    return;
                }

                var token = await DatabaseService.CreatePasswordResetTokenAsync(u, TimeSpan.FromMinutes(15));
                await EmailService.SendResetEmailAsync(info.Email!, u, token);

                await DisplayAlert("Recuperação",
                    $"Enviámos um e-mail para {info.Email} com um código de 6 dígitos.\nUsa 'Tenho um código do e-mail' para o aplicar.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Falha ao enviar e-mail: " + ex.Message, "OK");
            }
        }

        private async void OnInternalResetTapped(object sender, EventArgs e)
        {
            var u = entryUsername.Text?.Trim();
            if (string.IsNullOrWhiteSpace(u))
            {
                await DisplayAlert("Reset interno", "Escreve primeiro o nome de utilizador.", "OK");
                return;
            }

            var key = await DisplayPromptAsync("Reset interno", "Chave de reset (admin):",
                accept: "OK", cancel: "Cancelar", placeholder: "chave", keyboard: Keyboard.Text);
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!string.Equals(key, AdminResetKey, StringComparison.Ordinal))
            {
                await DisplayAlert("Reset interno", "Chave inválida.", "OK");
                return;
            }

            var newPass = await PromptMaskedPasswordAsync("Nova palavra-passe", "Introduz a nova password:");
            if (string.IsNullOrWhiteSpace(newPass))
                return;

            try
            {
                var ok = await DatabaseService.ResetPasswordAsync(u, newPass.Trim());
                await DisplayAlert("Reset interno", ok ? "Password alterada." : "Utilizador não encontrado.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erro", "Falha no reset interno: " + ex.Message, "OK");
            }
        }

        // Popup inline (corrigido: sem erros de cast, sem SetZIndex inexistente, alpha correto)
        private Task<string?> PromptMaskedPasswordAsync(string title, string message)
        {
            var tcs = new TaskCompletionSource<string?>();

            if (this.Content is not Layout root)
            {
                tcs.SetResult(null);
                return tcs.Task;
            }

            var overlay = new Grid
            {
                BackgroundColor = Color.FromRgba(0, 0, 0, 0.55f),
                Padding = 20,
                InputTransparent = false
            };

            // Obter cor de fundo segura
            Color cardColor = Colors.White;
            if (Application.Current?.Resources != null &&
                Application.Current.Resources.TryGetValue("PageBackgroundColor", out var bgObj) &&
                bgObj is Color foundColor)
            {
                cardColor = foundColor;
            }

            var card = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = cardColor,
                Padding = 20,
                WidthRequest = 340,
                MaximumWidthRequest = 360,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            var titleLabel = new Label
            {
                Text = title,
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var msgLabel = new Label
            {
                Text = message,
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var entry = new Entry
            {
                IsPassword = true,
                Placeholder = "••••••••",
                HorizontalOptions = LayoutOptions.Fill,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing
            };

            var btnCancel = new Button
            {
                Text = "Cancelar",
                BackgroundColor = Colors.Transparent
            };

            var btnOk = new Button
            {
                Text = "OK"
            };

            var buttonsLayout = new HorizontalStackLayout
            {
                Spacing = 14,
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 14, 0, 0),
                Children = { btnCancel, btnOk }
            };

            var stack = new VerticalStackLayout
            {
                Spacing = 4
            };
            stack.Children.Add(titleLabel);
            stack.Children.Add(msgLabel);
            stack.Children.Add(entry);
            stack.Children.Add(buttonsLayout);

            card.Content = stack;
            overlay.Children.Add(card);

            void CloseOverlay(string? result)
            {
                if (root.Children.Contains(overlay))
                    root.Children.Remove(overlay);
                tcs.TrySetResult(result);
            }

            btnCancel.Clicked += (_, __) => CloseOverlay(null);

            btnOk.Clicked += async (_, __) =>
            {
                var pwd = entry.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(pwd))
                {
                    await DisplayAlert("Validação", "Indica a password.", "OK");
                    return;
                }
                if (pwd.Length < 4)
                {
                    await DisplayAlert("Validação", "Mínimo 4 caracteres.", "OK");
                    return;
                }
                CloseOverlay(pwd);
            };

            entry.Completed += (_, __) => btnOk.SendClicked();

            // ZIndex (propriedade diretamente na view)
            overlay.ZIndex = 9999;

            root.Children.Add(overlay);

            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(50), () => entry.Focus());

            return tcs.Task;
        }

        private void SetBusy(bool busy)
        {
            btnLogin.IsEnabled = !busy;
            loading.IsVisible = busy;
            loading.IsRunning = busy;
        }

        private void ShowMsg(string text, bool ok)
        {
            labelMensagem.IsVisible = true;
            labelMensagem.Text = text;
            labelMensagem.TextColor = ok ? Colors.Green : Colors.Red;
        }

        private void HideMsg()
        {
            labelMensagem.IsVisible = false;
            labelMensagem.Text = "";
        }
    }
}

#if WINDOWS
// Código Windows específico (exemplo: animações, navegação, layouts)
#endif
#if ANDROID
// Código Android específico (exemplo: animações, navegação, layouts)
#endif
#if IOS
// Código iOS específico (exemplo: animações, navegação, layouts)
#endif
