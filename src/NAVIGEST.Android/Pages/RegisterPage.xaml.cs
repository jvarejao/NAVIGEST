using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;
using NAVIGEST.Shared.Resources.Strings;

namespace NAVIGEST.Android.Pages
{
    public partial class RegisterPage : ContentPage
    {
        private static readonly string[] FieldNames =
        {
            "entryUsername",
            "entryPassword",
            "entryName",
            "entryContact",
            "entryEmail",
            "entryCategoria1",
            "entryCategoria2",
            "entryTipo"
        };

        private readonly ObservableCollection<Registration> _users = new();
        private byte[]? _imagemBytes;

        public RegisterPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CarregarListaAsync();
            AtualizarAvatar();
        }

        public async Task InitializeForHostAsync()
        {
            try
            {
                await CarregarListaAsync();
            }
            catch (Exception ex)
            {
                GlobalErro.TratarErro(ex);
            }
        }

        private async Task CarregarListaAsync()
        {
            try
            {
                var lista = await DatabaseService.GetUsersAsync();
                _users.Clear();
                foreach (var user in lista)
                {
                    _users.Add(user);
                }

                if (UserPickerOverlay?.IsVisible == true && UsersPickerView != null)
                {
                    UsersPickerView.ItemsSource = _users;
                }
            }
            catch (Exception ex)
            {
                await GlobalToast.ShowAsync("Erro ao carregar lista: " + ex.Message, ToastTipo.Erro, 2000);
            }
        }

        private async void OnEscolherImagemClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Escolher imagem",
                    FileTypes = FilePickerFileType.Images
                });

                if (result == null)
                    return;

                await using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                _imagemBytes = ms.ToArray();

                if (_imagemBytes.Length > 6_000_000)
                {
                    _imagemBytes = null;
                    await ShowToastAsync("A imagem tem mais de 6 MB. Escolhe uma imagem mais pequena.", false);
                }

                AtualizarAvatar();
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro ao escolher imagem: " + ex.Message, false);
            }
        }

        private void OnNovoClicked(object sender, EventArgs e)
        {
            LimparFormulario();
            HideUserPicker();
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            await GuardarOuAtualizarAsync(inserir: true);
        }

        private async void OnAtualizarClicked(object sender, EventArgs e)
        {
            await GuardarOuAtualizarAsync(inserir: false);
            AtualizarAvatar();
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var username = GetEntryText("entryUsername");
            if (string.IsNullOrWhiteSpace(username))
            {
                await ShowToastAsync("Indique o username para eliminar.", false);
                return;
            }

            bool confirmar = await DisplayAlert(AppResources.RegisterPage_DeleteConfirmTitle, string.Format(AppResources.RegisterPage_DeleteConfirmMessage, username), AppResources.Common_Yes, AppResources.Common_No);
            if (!confirmar)
                return;

            await EliminarUtilizadorAsync(username);
        }

        private async Task EliminarUtilizadorAsync(string username)
        {
            try
            {
                var ok = await DatabaseService.DeleteUserAsync(username);
                if (ok)
                {
                    LimparFormulario();
                    await CarregarListaAsync();
                }

                await ShowToastAsync(ok ? "Utilizador eliminado." : "Nenhuma linha eliminada (username inexistente).", ok);
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro: " + ex.Message, false);
            }
        }

        private async Task GuardarOuAtualizarAsync(bool inserir)
        {
            string username = GetEntryText("entryUsername");
            if (string.IsNullOrWhiteSpace(username))
            {
                await ShowToastAsync("Username é obrigatório.", false);
                return;
            }

            string password = GetEntryText("entryPassword");
            if (string.IsNullOrWhiteSpace(password))
            {
                await ShowToastAsync("Password é obrigatória.", false);
                return;
            }

            string name = GetEntryText("entryName");
            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowToastAsync("Nome é obrigatório.", false);
                return;
            }

            string contact = GetEntryText("entryContact");
            if (string.IsNullOrWhiteSpace(contact))
            {
                await ShowToastAsync("Contacto é obrigatório.", false);
                return;
            }

            string email = GetEntryText("entryEmail");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                await ShowToastAsync("Email inválido.", false);
                return;
            }

            string categoria1 = GetEntryText("entryCategoria1");
            if (string.IsNullOrWhiteSpace(categoria1))
            {
                await ShowToastAsync("Categoria 1 é obrigatória.", false);
                return;
            }

            string categoria2 = GetEntryText("entryCategoria2");
            if (string.IsNullOrWhiteSpace(categoria2))
            {
                await ShowToastAsync("Categoria 2 é obrigatória.", false);
                return;
            }

            string tipo = GetEntryText("entryTipo");
            if (string.IsNullOrWhiteSpace(tipo))
            {
                await ShowToastAsync("Tipo é obrigatório.", false);
                return;
            }

            var reg = new Registration
            {
                Username = username,
                Password = password,
                Name = name,
                ContactNo = contact,
                Email = email,
                Categoria1 = categoria1,
                Categoria2 = categoria2,
                TipoUtilizador = tipo,
                ProfilePicture = _imagemBytes
            };

            try
            {
                bool ok;
                if (inserir)
                {
                    if (await DatabaseService.UserExistsAsync(reg.Username))
                    {
                        await ShowToastAsync("Já existe um utilizador com esse username.", false);
                        return;
                    }

                    ok = await DatabaseService.GravarUtilizadorAsync(reg);
                }
                else
                {
                    ok = await DatabaseService.UpdateUserAsync(reg);
                }

                if (ok)
                {
                    await CarregarListaAsync();
                    LimparFormulario();
                }

                string mensagem = ok
                    ? inserir ? "Utilizador guardado." : "Utilizador atualizado."
                    : "Nenhuma alteração.";

                await ShowToastAsync(mensagem, ok);
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro: " + ex.Message, false);
            }
        }

        private void OnOpenUserPicker(object sender, EventArgs e)
        {
            ShowUserPicker();
        }

        private void OnCloseUserPicker(object sender, EventArgs e)
        {
            HideUserPicker();
        }

        private async void OnUserPickerSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not Registration user)
                return;

            await OnUserSelectedAsync(user);

            if (UsersPickerView != null)
            {
                UsersPickerView.SelectedItem = null;
            }

            HideUserPicker();

            if (MainScrollView != null)
            {
                await MainScrollView.ScrollToAsync(0, 0, true);
            }
        }

        private Task OnUserSelectedAsync(Registration user)
        {
            SetEntryText("entryUsername", user.Username ?? string.Empty);
            SetEntryText("entryPassword", string.Empty);
            SetEntryText("entryName", user.Name ?? string.Empty);
            SetEntryText("entryContact", user.ContactNo ?? string.Empty);
            SetEntryText("entryEmail", user.Email ?? string.Empty);
            SetEntryText("entryCategoria1", user.Categoria1 ?? string.Empty);
            SetEntryText("entryCategoria2", user.Categoria2 ?? string.Empty);
            SetEntryText("entryTipo", user.TipoUtilizador ?? string.Empty);

            _imagemBytes = user.ProfilePicture;
            AtualizarAvatar();
            return Task.CompletedTask;
        }

        private void ShowUserPicker()
        {
            if (UserPickerOverlay == null)
                return;

            if (UsersPickerView != null)
            {
                UsersPickerView.ItemsSource = _users;
            }

            UserPickerOverlay.IsVisible = true;
        }

        private void HideUserPicker()
        {
            if (UserPickerOverlay == null)
                return;

            UserPickerOverlay.IsVisible = false;
        }

        private void LimparFormulario()
        {
            foreach (var name in FieldNames)
            {
                SetEntryText(name, string.Empty);
            }

            _imagemBytes = null;
            AtualizarAvatar();

            if (UsersPickerView != null)
            {
                UsersPickerView.SelectedItem = null;
            }
        }

        private void AtualizarAvatar()
        {
            if (UserImage == null || UserIcon == null)
                return;

            bool temImagem = _imagemBytes is { Length: > 0 };
            UserImage.IsVisible = temImagem;
            UserIcon.IsVisible = !temImagem;

            if (temImagem)
            {
                var bytes = _imagemBytes!;
                UserImage.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
            }
            else
            {
                UserImage.Source = null;
            }
        }

        private string GetEntryText(string name)
        {
            return (FindByName(name) as Entry)?.Text?.Trim() ?? string.Empty;
        }

        private void SetEntryText(string name, string value)
        {
            if (FindByName(name) is Entry entry)
            {
                entry.Text = value;
            }
        }

        private Task ShowToastAsync(string message, bool success)
            => GlobalToast.ShowAsync(message, success ? ToastTipo.Sucesso : ToastTipo.Erro, success ? 1600 : 2200);
    }
}
