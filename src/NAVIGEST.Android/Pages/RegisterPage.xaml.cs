using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;

namespace NAVIGEST.Android.Pages
{
    public partial class RegisterPage : ContentPage
    {
        private readonly ObservableCollection<Registration> _users = new();
        private byte[]? _imagemBytes;

        // Campos de erro para exibir mensagens inline
        private string _usernameError = string.Empty;
        private string _passwordError = string.Empty;
        private string _nameError = string.Empty;
        private string _contactError = string.Empty;
        private string _emailError = string.Empty;
        private string _categoria1Error = string.Empty;
        private string _categoria2Error = string.Empty;
        private string _tipoError = string.Empty;

        public RegisterPage()
        {
            InitializeComponent();
            SetUsersViewItemsSource();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            SetUsersViewItemsSource();
            await CarregarListaAsync();
            ApplyResponsiveLayout(Width);
            ClampPageWidth(Width);
            AtualizarAvatar();
            LimparMensagensErro();
        }

        private T? N<T>(string name) where T : class => this.FindByName<T>(name);

        private void SetEntryText(string baseName, string value)
        {
            if (N<Entry>(baseName) is Entry e) e.Text = value;
            if (N<Entry>(baseName + "Mobile") is Entry em) em.Text = value;
        }

        private void ClampPageWidth(double width)
        {
            if (width <= 0) return;
            double max = 1080;
            var pageContainer = N<Grid>("PageContainer");
            if (pageContainer != null)
                pageContainer.WidthRequest = Math.Min(width, max);
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            ClampPageWidth(Width);
            ApplyResponsiveLayout(Width);
            SetUsersViewItemsSource();
        }

        private void SetUsersViewItemsSource()
        {
            if (N<CollectionView>("UsersView") is CollectionView usersView)
                usersView.ItemsSource = _users;

            if (N<CollectionView>("UsersViewMobile") is CollectionView usersViewMobile)
                usersViewMobile.ItemsSource = _users;
        }

        private void ApplyResponsiveLayout(double width)
        {
            if (width <= 0) return;

            var detailsGrid = N<Grid>("DetailsGrid");
            var fieldsGrid = N<Grid>("FieldsGrid");
            var buttonsStack = N<VerticalStackLayout>("ButtonsStack");
            var avatarStack = N<VerticalStackLayout>("AvatarStack");
            var fieldsCol1 = N<VerticalStackLayout>("FieldsCol1");

            if (detailsGrid == null || fieldsGrid == null || buttonsStack == null || avatarStack == null || fieldsCol1 == null)
                return;

            bool isPhone = DeviceInfo.Current.Idiom == DeviceIdiom.Phone;
            bool narrow = isPhone || width < 720;

            detailsGrid.ColumnDefinitions.Clear();
            detailsGrid.RowDefinitions.Clear();

            if (narrow)
            {
                detailsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                detailsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                detailsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                detailsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                Grid.SetColumn(avatarStack, 0);
                Grid.SetRow(avatarStack, 0);

                Grid.SetColumn(fieldsGrid, 0);
                Grid.SetRow(fieldsGrid, 1);

                Grid.SetColumn(buttonsStack, 0);
                Grid.SetRow(buttonsStack, 2);

                fieldsGrid.ColumnDefinitions.Clear();
                fieldsGrid.RowDefinitions.Clear();
                fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                fieldsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                fieldsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                Grid.SetColumn(fieldsCol1, 0);
                Grid.SetRow(fieldsCol1, 0);
            }
            else
            {
                detailsGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(120)));
                detailsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                detailsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                detailsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                Grid.SetRow(avatarStack, 0);
                Grid.SetColumn(avatarStack, 0);

                Grid.SetRow(fieldsGrid, 0);
                Grid.SetColumn(fieldsGrid, 1);

                Grid.SetRow(buttonsStack, 0);
                Grid.SetColumn(buttonsStack, 2);

                fieldsGrid.ColumnDefinitions.Clear();
                fieldsGrid.RowDefinitions.Clear();
                fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                fieldsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                fieldsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                Grid.SetRow(fieldsCol1, 0);
                Grid.SetColumn(fieldsCol1, 0);
            }
        }

        public async Task InitializeForHostAsync()
        {
            try
            {
                await CarregarListaAsync();
            }
            catch (Exception ex) { GlobalErro.TratarErro(ex); }
        }

        private async Task CarregarListaAsync()
        {
            try
            {
                var lista = await DatabaseService.GetUsersAsync();
                _users.Clear();
                foreach (var u in lista) _users.Add(u);
            }
            catch (Exception ex)
            {
                await GlobalToast.ShowAsync("Erro ao carregar lista: " + ex.Message, ToastTipo.Erro, 2000);
            }
        }

        private async void OnAtualizarListaClicked(object sender, EventArgs e)
        {
            await CarregarListaAsync();
            await ShowToastAsync("Lista de utilizadores atualizada.", true);
        }

        private void OnAvatarContainerSizeChanged(object sender, EventArgs e)
        {
            // Desktop
            var avatar = N<Grid>("AvatarContainer");
            var userImage = N<Image>("UserImage");

            if (avatar != null && userImage != null)
            {
                double size = Math.Min(avatar.Width, avatar.Height);
                if (size > 0)
                {
                    userImage.WidthRequest = size;
                    userImage.HeightRequest = size;
                    userImage.Clip = new EllipseGeometry
                    {
                        Center = new Point(size / 2, size / 2),
                        RadiusX = size / 2,
                        RadiusY = size / 2
                    };
                }
            }

            // Mobile
            if (N<Grid>("AvatarContainerMobile") is Grid avatarMobile &&
                N<Image>("UserImageMobile") is Image userImageMobile)
            {
                double sizeM = Math.Min(avatarMobile.Width, avatarMobile.Height);
                if (sizeM > 0)
                {
                    userImageMobile.WidthRequest = sizeM;
                    userImageMobile.HeightRequest = sizeM;
                    userImageMobile.Clip = new EllipseGeometry
                    {
                        Center = new Point(sizeM / 2, sizeM / 2),
                        RadiusX = sizeM / 2,
                        RadiusY = sizeM / 2
                    };
                }
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
                if (result == null) return;

                using var stream = await result.OpenReadAsync();
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                _imagemBytes = ms.ToArray();

                if (_imagemBytes.Length > 6_000_000)
                {
                    _imagemBytes = null;
                    await ShowToastAsync("A imagem tem mais de 6 MB. Escolhe uma imagem mais pequena.", false);
                    AtualizarAvatar();
                    return;
                }

                AtualizarAvatar();
                OnAvatarContainerSizeChanged(null!, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro ao escolher imagem: " + ex.Message, false);
            }
        }

        private void OnNovoClicked(object sender, EventArgs e)
        {
            // Limpa campos Desktop e Mobile
            SetEntryText("entryUsername", "");
            SetEntryText("entryPassword", "");
            SetEntryText("entryName", "");
            SetEntryText("entryContact", "");
            SetEntryText("entryEmail", "");
            SetEntryText("entryCategoria1", "");
            SetEntryText("entryCategoria2", "");
            SetEntryText("entryTipo", "");

            _imagemBytes = null;

            if (N<CollectionView>("UsersView") is CollectionView uv) uv.SelectedItem = null;
            if (N<CollectionView>("UsersViewMobile") is CollectionView uvm) uvm.SelectedItem = null;

            LimparMensagensErro();
            AtualizarAvatar();
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
            try
            {
                var username = GetCurrentEntryText("entryUsername");
                if (string.IsNullOrWhiteSpace(username))
                {
                    await ShowToastAsync("Indique o username para eliminar.", false);
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    bool confirmar = await DisplayAlert("Confirmação", $"Eliminar o utilizador '{username}'?", "Sim", "Não");
                    if (!confirmar)
                        return;

                    await EliminarUtilizadorAsync(username);
                });
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro ao eliminar: " + ex.Message, false);
            }
        }

        private async Task EliminarUtilizadorAsync(string username)
        {
            try
            {
                var ok = await DatabaseService.DeleteUserAsync(username);
                if (ok)
                {
                    OnNovoClicked(null!, EventArgs.Empty); // Limpa campos e seleção
                    await CarregarListaAsync();
                }
                await ShowToastAsync(ok ? "Utilizador eliminado." : "Nenhuma linha eliminada (username inexistente).", ok);
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro: " + ex.Message, false);
            }
        }

        // LÓGICA DE VALIDAÇÃO INLINE
        private async Task GuardarOuAtualizarAsync(bool inserir)
        {
            string username = GetCurrentEntryText("entryUsername");
            string password = GetCurrentEntryText("entryPassword");
            string name = GetCurrentEntryText("entryName");
            string contact = GetCurrentEntryText("entryContact");
            string email = GetCurrentEntryText("entryEmail");
            string categoria1 = GetCurrentEntryText("entryCategoria1");
            string categoria2 = GetCurrentEntryText("entryCategoria2");
            string tipo = GetCurrentEntryText("entryTipo");

            bool temErro = false;
            _usernameError = string.Empty;
            _passwordError = string.Empty;
            _nameError = string.Empty;
            _contactError = string.Empty;
            _emailError = string.Empty;
            _categoria1Error = string.Empty;
            _categoria2Error = string.Empty;
            _tipoError = string.Empty;

            if (string.IsNullOrWhiteSpace(username)) { _usernameError = "Username é obrigatório."; temErro = true; }
            if (string.IsNullOrWhiteSpace(password)) { _passwordError = "Password é obrigatória."; temErro = true; }
            if (string.IsNullOrWhiteSpace(name)) { _nameError = "Nome é obrigatório."; temErro = true; }
            if (string.IsNullOrWhiteSpace(contact)) { _contactError = "Contacto é obrigatório."; temErro = true; }
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@")) { _emailError = "Email inválido."; temErro = true; }
            if (string.IsNullOrWhiteSpace(categoria1)) { _categoria1Error = "Categoria 1 é obrigatória."; temErro = true; }
            if (string.IsNullOrWhiteSpace(categoria2)) { _categoria2Error = "Categoria 2 é obrigatória."; temErro = true; }
            if (string.IsNullOrWhiteSpace(tipo)) { _tipoError = "Tipo é obrigatório."; temErro = true; }

            AtualizarMensagensErro();

            if (temErro)
            {
                await ShowToastAsync("Preencha os campos obrigatórios.", false);
                return;
            }

            var reg = new Registration
            {
                Username = username,
                Password = password,
                Name = name,
                ContactNo = contact,
                Categoria1 = categoria1,
                Categoria2 = categoria2,
                TipoUtilizador = tipo,
                Email = email,
                ProfilePicture = _imagemBytes
            };

            try
            {
                bool ok;
                if (inserir)
                {
                    if (await DatabaseService.UserExistsAsync(reg.Username))
                    {
                        _usernameError = "Já existe um utilizador com esse username.";
                        AtualizarMensagensErro();
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
                    OnNovoClicked(null!, EventArgs.Empty); // Limpa campos e seleção
                    await CarregarListaAsync();
                }
                await ShowToastAsync(ok ? (inserir ? "Utilizador guardado." : "Utilizador atualizado.") : "Nenhuma alteração.", ok);
            }
            catch (Exception ex)
            {
                await ShowToastAsync("Erro: " + ex.Message, false);
            }
        }

        // Atualiza mensagens de erro inline nos campos
        private void AtualizarMensagensErro()
        {
            // Desktop
            if (N<Label>("lblUsernameError") is Label lblUser) { lblUser.Text = _usernameError; lblUser.IsVisible = !string.IsNullOrEmpty(_usernameError); }
            if (N<Label>("lblPasswordError") is Label lblPass) { lblPass.Text = _passwordError; lblPass.IsVisible = !string.IsNullOrEmpty(_passwordError); }
            if (N<Label>("lblNameError") is Label lblName) { lblName.Text = _nameError; lblName.IsVisible = !string.IsNullOrEmpty(_nameError); }
            if (N<Label>("lblContactError") is Label lblContact) { lblContact.Text = _contactError; lblContact.IsVisible = !string.IsNullOrEmpty(_contactError); }
            if (N<Label>("lblEmailError") is Label lblEmail) { lblEmail.Text = _emailError; lblEmail.IsVisible = !string.IsNullOrEmpty(_emailError); }
            if (N<Label>("lblCategoria1Error") is Label lblCat1) { lblCat1.Text = _categoria1Error; lblCat1.IsVisible = !string.IsNullOrEmpty(_categoria1Error); }
            if (N<Label>("lblCategoria2Error") is Label lblCat2) { lblCat2.Text = _categoria2Error; lblCat2.IsVisible = !string.IsNullOrEmpty(_categoria2Error); }
            if (N<Label>("lblTipoError") is Label lblTipo) { lblTipo.Text = _tipoError; lblTipo.IsVisible = !string.IsNullOrEmpty(_tipoError); }
            // Mobile
            if (N<Label>("lblUsernameErrorMobile") is Label lblUserM) { lblUserM.Text = _usernameError; lblUserM.IsVisible = !string.IsNullOrEmpty(_usernameError); }
            if (N<Label>("lblPasswordErrorMobile") is Label lblPassM) { lblPassM.Text = _passwordError; lblPassM.IsVisible = !string.IsNullOrEmpty(_passwordError); }
            if (N<Label>("lblNameErrorMobile") is Label lblNameM) { lblNameM.Text = _nameError; lblNameM.IsVisible = !string.IsNullOrEmpty(_nameError); }
            if (N<Label>("lblContactErrorMobile") is Label lblContactM) { lblContactM.Text = _contactError; lblContactM.IsVisible = !string.IsNullOrEmpty(_contactError); }
            if (N<Label>("lblEmailErrorMobile") is Label lblEmailM) { lblEmailM.Text = _emailError; lblEmailM.IsVisible = !string.IsNullOrEmpty(_emailError); }
            if (N<Label>("lblCategoria1ErrorMobile") is Label lblCat1M) { lblCat1M.Text = _categoria1Error; lblCat1M.IsVisible = !string.IsNullOrEmpty(_categoria1Error); }
            if (N<Label>("lblCategoria2ErrorMobile") is Label lblCat2M) { lblCat2M.Text = _categoria2Error; lblCat2M.IsVisible = !string.IsNullOrEmpty(_categoria2Error); }
            if (N<Label>("lblTipoErrorMobile") is Label lblTipoM) { lblTipoM.Text = _tipoError; lblTipoM.IsVisible = !string.IsNullOrEmpty(_tipoError); }

            // Visual feedback: borda de erro
            if (N<Entry>("entryUsername") is Entry entryUser) entryUser.Style = !string.IsNullOrEmpty(_usernameError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryPassword") is Entry entryPass) entryPass.Style = !string.IsNullOrEmpty(_passwordError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryName") is Entry entryName) entryName.Style = !string.IsNullOrEmpty(_nameError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryContact") is Entry entryContact) entryContact.Style = !string.IsNullOrEmpty(_contactError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryEmail") is Entry entryEmail) entryEmail.Style = !string.IsNullOrEmpty(_emailError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryCategoria1") is Entry entryCat1) entryCat1.Style = !string.IsNullOrEmpty(_categoria1Error) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryCategoria2") is Entry entryCat2) entryCat2.Style = !string.IsNullOrEmpty(_categoria2Error) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryTipo") is Entry entryTipo) entryTipo.Style = !string.IsNullOrEmpty(_tipoError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryUsernameMobile") is Entry entryUserM) entryUserM.Style = !string.IsNullOrEmpty(_usernameError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryPasswordMobile") is Entry entryPassM) entryPassM.Style = !string.IsNullOrEmpty(_passwordError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryNameMobile") is Entry entryNameM) entryNameM.Style = !string.IsNullOrEmpty(_nameError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryContactMobile") is Entry entryContactM) entryContactM.Style = !string.IsNullOrEmpty(_contactError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryEmailMobile") is Entry entryEmailM) entryEmailM.Style = !string.IsNullOrEmpty(_emailError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryCategoria1Mobile") is Entry entryCat1M) entryCat1M.Style = !string.IsNullOrEmpty(_categoria1Error) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryCategoria2Mobile") is Entry entryCat2M) entryCat2M.Style = !string.IsNullOrEmpty(_categoria2Error) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
            if (N<Entry>("entryTipoMobile") is Entry entryTipoM) entryTipoM.Style = !string.IsNullOrEmpty(_tipoError) ? (Style?)Application.Current?.Resources["EntryErro"] : (Style?)Application.Current?.Resources["EntryPadrao"];
        }

        private void LimparMensagensErro()
        {
            _usernameError = string.Empty;
            _passwordError = string.Empty;
            _nameError = string.Empty;
            _contactError = string.Empty;
            _emailError = string.Empty;
            _categoria1Error = string.Empty;
            _categoria2Error = string.Empty;
            _tipoError = string.Empty;
            AtualizarMensagensErro();
        }

        private string GetCurrentEntryText(string baseName)
        {
            var idiom = DeviceInfo.Current.Idiom;
            if (idiom == DeviceIdiom.Phone || idiom == DeviceIdiom.Tablet)
            {
                if (N<Entry>(baseName + "Mobile") is Entry entryMobile)
                    return entryMobile.Text?.Trim() ?? "";
            }
            else
            {
                if (N<Entry>(baseName) is Entry entryDesktop)
                    return entryDesktop.Text?.Trim() ?? "";
            }
            return "";
        }

        private void OnUserSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection?.FirstOrDefault() is not Registration sel) return;

            SetEntryText("entryUsername", sel.Username);
            SetEntryText("entryPassword", sel.Password);
            SetEntryText("entryName", sel.Name);
            SetEntryText("entryContact", sel.ContactNo);
            SetEntryText("entryEmail", sel.Email);
            SetEntryText("entryCategoria1", sel.Categoria1);
            SetEntryText("entryCategoria2", sel.Categoria2);
            SetEntryText("entryTipo", sel.TipoUtilizador);

            _imagemBytes = sel.ProfilePicture;

            LimparMensagensErro();
            AtualizarAvatar();
            OnAvatarContainerSizeChanged(null!, EventArgs.Empty);
        }

        private void AtualizarAvatar()
        {
            // Desktop
            if (N<Image>("UserImage") is Image userImage &&
                N<Label>("UserIcon") is Label userIcon)
            {
                bool temImagem = _imagemBytes is { Length: > 0 };
                userImage.IsVisible = temImagem;
                userIcon.IsVisible = !temImagem;
                userImage.Source = temImagem
                    ? ImageSource.FromStream(() => new MemoryStream(_imagemBytes))
                    : null;
            }

            // Mobile
            if (N<Image>("UserImageMobile") is Image userImageMobile &&
                N<Label>("UserIconMobile") is Label userIconMobile)
            {
                bool temImagem = _imagemBytes is { Length: > 0 };
                userImageMobile.IsVisible = temImagem;
                userIconMobile.IsVisible = !temImagem;
                userImageMobile.Source = temImagem
                    ? ImageSource.FromStream(() => new MemoryStream(_imagemBytes))
                    : null;
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

        // Novo: delega para GlobalToast mantendo assinatura usada no código existente
        private Task ShowToastAsync(string message, bool success)
            => GlobalToast.ShowAsync(message, success ? ToastTipo.Sucesso : ToastTipo.Erro, success ? 1600 : 2200);
    }
}