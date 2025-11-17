# NAVIGEST - Android Specifics

## 📱 Considerações Específicas para Android

Este documento cataloga características, limitações e padrões específicos da plataforma Android que afetam desenvolvimento MAUI.

---

## 1. Versões Suportadas

- **Mínima:** Android API 21 (Android 5.0 Lollipop)
- **Target:** Android API 33+ (Android 13+)
- **Recomendada:** API 34+ (Android 14+)

**Impacto na Documentação:** Quando portar, verificar se iOS/macOS têm requisitos diferentes.

---

## 2. Rotação de Ecrã

### Comportamento Padrão
Android permite rotação portrait ↔ landscape automaticamente. MAUI recria views ao rotacionar.

### Padrão Recomendado

**No AndroidManifest.xml:**
```xml
<activity android:screenOrientation="portrait"
          android:configChanges="orientation|screenSize">
</activity>
```

Ou no code-behind:
```csharp
public partial class [PageName] : ContentPage
{
    public [PageName]()
    {
        InitializeComponent();
        
        // Force portrait
        DeviceDisplay.KeepScreenOn = true;
    }
}
```

### Componentes Afetados
- CollectionView: Scroll pode perder-se em rotação (testar)
- Custom layouts: Podem não redimensionar corretamente
- Modals/Popups: Podem não reposicionar bem

**Ação ao portar:** Testar cada página em portrait E landscape.

---

## 3. Teclado Virtual

### Comportamento Padrão
O teclado virtual pode cobrir Input fields. MAUI trata parcialmente automaticamente.

### Padrão Recomendado

```xaml
<!-- Usar ScrollView para garantir que inputs não são cobertos -->
<ScrollView>
    <VerticalStackLayout Padding="20">
        <Entry x:Name="InputField" 
               Placeholder="Digite aqui"
               ClearButtonVisibility="WhileEditing" />
    </VerticalStackLayout>
</ScrollView>
```

### Callbacks no Code-behind
```csharp
public partial class [PageName] : ContentPage
{
    public [PageName]()
    {
        InitializeComponent();
        
        // Quando Entry recebe foco, scroll para visibilidade
        InputField.Focused += (s, e) => 
        {
            MainScroll.ScrollToAsync(InputField, ScrollToPosition.Center, true);
        };
    }
}
```

---

## 4. Back Button (Botão Voltar)

### Comportamento Padrão
Android tem hardware/software back button. MAUI redireciona automaticamente para `Shell.Back()` ou `Navigation.PopAsync()`.

### Padrão Recomendado

No NavigationPage ou no code-behind:
```csharp
public partial class [PageName] : ContentPage
{
    public [PageName]()
    {
        InitializeComponent();
    }
    
    protected override bool OnBackButtonPressed()
    {
        // Lógica customizada antes de voltar
        if (HasUnsavedChanges)
        {
            DisplayAlert("Atenção", "Tem mudanças não guardadas", "OK");
            return true; // Impede voltar
        }
        
        return base.OnBackButtonPressed(); // Permite voltar normalmente
    }
}
```

**Ação ao portar:** Testar que back button funciona como esperado.

---

## 5. Permissions (Permissões)

### Permissões Comuns
- Câmera: `android.permission.CAMERA`
- Galeria: `android.permission.READ_EXTERNAL_STORAGE`
- Contactos: `android.permission.READ_CONTACTS`
- Localização: `android.permission.ACCESS_FINE_LOCATION`

### Padrão MAUI (CommunityToolkit)

```csharp
var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

if (status != PermissionStatus.Granted)
{
    status = await Permissions.RequestAsync<Permissions.Camera>();
}

if (status == PermissionStatus.Granted)
{
    // Usar câmera
}
```

**Ação ao portar:** iOS tem diferentes prompts de permissão (Info.plist).

---

## 6. Storage & File System

### Diretórios Recomendados

```csharp
// App documents (app-specific, é apagado se desinstalar)
string appDataPath = FileSystem.AppDataDirectory;

// Cache directory (pode ser apagado pelo SO)
string cachePath = FileSystem.CacheDirectory;

// External storage (SD card, se permitido)
var mainFolder = new FolderPickerOptions
{
    Title = "Selecione pasta"
};
```

### Padrão para Bases de Dados
```csharp
public static string GetConnectionString()
{
    var dbPath = Path.Combine(FileSystem.AppDataDirectory, "navigest.db");
    
    // Para MySQL remoto (como NAVIGEST usa)
    var connString = $"Server=api.example.com;Database=navigest_prod;User=user;Password=pass;";
    return connString;
}
```

---

## 7. Performance & Optimizações

### CollectionView Performance
```xaml
<!-- Use VirtualizingLayout para listas grandes -->
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemsLayout>
        <LinearItemsLayout 
            Orientation="Vertical" 
            ItemSpacing="5"
            HorizontalItemSpacing="10" />
    </CollectionView.ItemsLayout>
</CollectionView>
```

### Cache de Imagens
```xaml
<!-- Caching built-in -->
<Image Source="https://example.com/image.jpg"
       Aspect="AspectFill"
       CachingEnabled="true"
       IsOpaque="true" />
```

### Lazy Loading
```csharp
// ViewModel
public void LoadNextPage()
{
    if (!_isLoading && HasMoreItems)
    {
        _isLoading = true;
        // Carregar próxima página de dados
        _isLoading = false;
    }
}
```

---

## 8. Temas & Estilos

### Suporte para Light/Dark Mode
```xaml
<Label Text="Hello"
       TextColor="{AppThemeBinding Light=Black, Dark=White}" />
```

No App.xaml:
```xaml
<Application.Resources>
    <Color x:Key="PrimaryColor">#FF6200EE</Color>
    <Color x:Key="TextColor" 
           Light="Black" 
           Dark="White" />
</Application.Resources>
```

### Material Design 3
MAUI segue Material Design 3 por padrão. Usar cores padronizadas.

---

## 9. Notificações

### Push Notifications (Firebase)
```csharp
// MauiProgram.cs
builder.Services.AddSingleton<IFirebaseService, FirebaseService>();

// No Page/ViewModel
var firebaseService = ServiceHelper.GetService<IFirebaseService>();
await firebaseService.SendNotificationAsync("Title", "Message");
```

### Local Notifications
```csharp
var notification = new NotificationRequest
{
    NotificationId = 1001,
    Title = "Título",
    Description = "Descrição",
    BadgeNumber = 1,
    Schedule = new NotificationRequestBuilder()
                .Build()
                .Schedule
};

await NotificationCenter.SendAsync(notification);
```

---

## 10. Debugging & Logcat

### Ver Logcat
```bash
adb logcat | grep -i "navigest"
```

### Output debugging no C#
```csharp
System.Diagnostics.Debug.WriteLine($"[HorasVM] Loaded {items.Count} items");
```

Filtrar em logcat:
```bash
adb logcat -s "HorasVM"
```

### Breakpoints
Visual Studio suporta breakpoints ao debugar Android. Ligar device via USB.

---

## 11. Compatibilidade de Dispositivos

### Tamanhos Comuns
- **Mobile:** 320dp a 480dp (portrait)
- **Tablet:** 600dp+ (landscape)
- **Foldable:** 300dp a 800dp+

### Testar em
- Emulador: Android Studio (API 33+)
- Device real: Preferir para testes finais
- Multiple sizes: Testar em 4.5", 5.5", 6.7", 10"

---

## 12. Versionamento & Build

### Versão da App
No `.csproj`:
```xml
<PropertyGroup>
    <ApplicationVersion>1</ApplicationVersion>  <!-- Android build number -->
    <ApplicationDisplayVersion>1.0.30</ApplicationDisplayVersion>  <!-- User-facing version -->
</PropertyGroup>
```

### Build Variants
- Debug: Com breakpoints, sem otimizações
- Release: Otimizado, ofuscado, pronto para production

---

## 13. Integração com Android APIs

### Exemplo: Contactos
```csharp
if (DeviceInfo.Platform == DevicePlatform.Android)
{
    var contacts = await Contacts.GetAllAsync();
    // Processar contactos
}
```

### Exemplo: Câmera
```csharp
var photo = await MediaPicker.CapturePhotoAsync(
    new MediaPickerOptions { Title = "Tirar foto" });

if (photo != null)
{
    using var stream = await photo.OpenReadAsync();
    // Processar imagem
}
```

---

## 14. Conhecidos Issues & Workarounds

| Issue | Plataforma | Status | Workaround |
|-------|-----------|--------|-----------|
| CollectionView scroll não funciona em LinearLayout | Android | Comum | Usar ScrollView ou Grid |
| Teclado cobre Entry em formulários | Android | Comum | Usar ScrollView + OnFocus scroll |
| Rotação perde dados | Android | Comum | Usar ViewModel state preservation |
| Memoria leak em listas grandes | Android | Raro | Unsubscribe de events |

---

## 15. Checklist para Novo Componente Android

- [ ] Testei em retrato e paisagem
- [ ] Testei com teclado virtual
- [ ] Testei back button
- [ ] Testar em múltiplos tamanhos de dispositivo
- [ ] CollectionView funciona (se aplicável)
- [ ] Sem memory leaks (Logcat limpo)
- [ ] Performance aceitável (< 1s load)
- [ ] Documentei em COMPONENTS/

---

## 📌 Referências

- 🔗 MAUI Docs: https://docs.microsoft.com/maui
- 🔗 Android Dev: https://developer.android.com
- 🔗 CommunityToolkit: https://github.com/CommunityToolkit/Maui

