# NAVIGEST - Windows Specifics

## 🪟 Considerações Específicas para Windows

Este documento cataloga características, limitações e padrões específicos da plataforma Windows que afetam desenvolvimento MAUI.

---

## 1. Versões Suportadas

- **Mínima:** Windows 10 (Build 18362+)
- **Target:** Windows 10 21H2 ou Windows 11
- **Recomendada:** Windows 11

**Nota:** MAUI em Windows usa WinUI 3.

---

## 2. Project Setup

### Visual Studio Project

```csproj
<!-- NET 8 MAUI Windows Project -->
<Project Sdk="Microsoft.Maui.Controls.SingleProject">
    <PropertyGroup>
        <TargetFrameworks>net8.0-windows10.0.19041.0</TargetFrameworks>
        <OutputType>WinExe</OutputType>
        <UseMaui>true</UseMaui>
        <SingleProject>true</SingleProject>
    </PropertyGroup>
</Project>
```

### Compilação

```bash
dotnet build -f net8.0-windows10.0.19041.0 -c Release
```

---

## 3. Window Management

### Tamanho & Posição da Janela

```csharp
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }
}

// No AppShell.xaml.cs ou MainPage.xaml.cs
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        // Windows-specific: Tamanho inicial
        #if WINDOWS
            MainThread.BeginInvokeOnMainThread(() => 
            {
                var window = Application.Current?.MainPage?.Window;
                if (window != null)
                {
                    window.Width = 1200;
                    window.Height = 800;
                }
            });
        #endif
    }
}
```

### Fullscreen & Maximized

```csharp
#if WINDOWS
    var window = Application.Current?.MainPage?.Window;
    if (window != null)
    {
        // window.State = WindowState.Maximized;
        // Não existe API simples em MAUI, pode precisar platform invoke
    }
#endif
```

---

## 4. DPI Scaling

### 100% DPI (Normal)
```xaml
<!-- Layout padrão -->
<Grid ColumnDefinitions="*" RowDefinitions="Auto,*">
</Grid>
```

### 125%, 150%, 200% DPI

MAUI adapta automaticamente. Testar em:
1. Configurações Windows → Display → Scale
2. Ou em monitor de alta resolução (4K)

### Responsive Layout

```xaml
<!-- Adapta com base em disponível space -->
<Grid ColumnDefinitions="Auto,*"
      ColumnSpacing="10"
      Padding="20">
    <Label Text="Label" WidthRequest="100" />
    <Entry Placeholder="Input" />
</Grid>
```

---

## 5. Keyboard Shortcuts

### Command Key (CTRL, ALT, SHIFT)

```csharp
// MAUI não suporta natively keyboard shortcuts globais
// Workaround: Usar MenuBar ou button shortcuts

// Em code-behind, pode interceptar teclado
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        
        #if WINDOWS
            // Platform-specific keyboard handling
            // Requer: using Microsoft.Maui.Controls.Hosting;
        #endif
    }
}
```

### Menu Bar (Windows)

```xaml
<!-- MenuBar é Windows-only -->
<Grid RowDefinitions="Auto,*">
    <!-- Menu aqui em futuro release MAUI -->
    
    <ContentPage Grid.Row="1">
        <!-- App content -->
    </ContentPage>
</Grid>
```

---

## 6. File Picker & Storage

### Save/Open Dialog

```csharp
// MAUI suporta FilePicker cross-platform
var result = await FilePicker.PickAsync(new PickOptions
{
    PickerTitle = "Selecione um ficheiro",
    FileTypes = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Windows, new[] { ".json", ".txt" } }
        })
});

if (result != null)
{
    using var stream = await result.OpenReadAsync();
    // Processar ficheiro
}
```

### Save Dialog

```csharp
// FileSaver (experimental em MAUI)
var result = await FilePicker.SaveAsync(new PickOptions
{
    PickerTitle = "Guardar ficheiro",
    FileTypes = new FilePickerFileType(
        new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            { DevicePlatform.Windows, new[] { ".json" } }
        })
});
```

---

## 7. Impressão (Printing)

### Print Dialog

```csharp
if (share.SupportsShareUri)
{
    await Share.RequestAsync(new ShareFileRequest
    {
        Title = "Imprimir",
        File = new ShareFile(filePath)
    });
}

// Ou via printer específica
#if WINDOWS
    // Pode usar PrintDocument do WinRT
#endif
```

---

## 8. System Information

### Detetar Sistema

```csharp
var info = DeviceInfo.Current;
if (DeviceInfo.Platform == DevicePlatform.WinUI)
{
    var model = DeviceInfo.Model; // Ex: "Dell XPS 13"
    var osVersion = DeviceInfo.VersionString; // Ex: "Windows 10 (22H2)"
}
```

---

## 9. Dark Mode

### Suporte Automático

Windows 10/11 Dark Mode é automático em MAUI.

```xaml
<Label Text="Hello"
       TextColor="{AppThemeBinding Light=Black, Dark=White}" />
```

### Forçar Light Mode

Não é standard em MAUI, pode precisar platform-specific code.

---

## 10. Tema de Cores

### Accent Color (Windows)

Windows permite user escolher accent color nas settings.

```xaml
<!-- MAUI segue o tema automático -->
<Button Text="OK" 
        BackgroundColor="{StaticResource AccentColor}" />
```

---

## 11. Notificações

### Toast Notification

```csharp
// MAUI não suporta Windows Toast natively
// Alternativa: Usar popup in-app

#if WINDOWS
    // Pode usar Windows.UI.Notifications para toast
#endif
```

### In-App Notification

```xaml
<Grid>
    <Label Text="Operação concluída"
           BackgroundColor="Green"
           TextColor="White"
           Padding="10"
           IsVisible="{Binding ShowNotification}" />
</Grid>
```

---

## 12. Performance & Optimization

### Renderização

MAUI em Windows é mais eficiente que iOS/Android em dispositivos desktop.

```xaml
<!-- Usar Grid para layouts complexos -->
<Grid RowDefinitions="*,*" ColumnDefinitions="*,*">
    <!-- 4 items em 2x2 grid -->
</Grid>
```

### Memory

Desktop tem mais RAM, mas ainda cuidado com listas grandes.

```xaml
<CollectionView ItemsSource="{Binding Items}"
                SelectionChangedCommand="{Binding SelectCommand}"
                SelectionChangedCommandParameter="{Binding SelectedItem, Source={RelativeSource Self}}">
</CollectionView>
```

---

## 13. Build & Distribution

### Visual Studio Build

```bash
# Debug
dotnet build -f net8.0-windows10.0.19041.0 -c Debug

# Release
dotnet build -f net8.0-windows10.0.19041.0 -c Release
```

### Create Installer (MSIX)

```bash
# Empacotar como MSIX (Microsoft Store format)
dotnet publish -f net8.0-windows10.0.19041.0 -c Release
```

---

## 14. Debugging

### Visual Studio Debugger

Funciona natively. Breakpoints, watch, etc tudo disponível.

```csharp
// Debug output
System.Diagnostics.Debug.WriteLine("Debug message");
```

### Console Output

```bash
dotnet run -f net8.0-windows10.0.19041.0 --no-build
```

---

## 15. Conhecidos Issues & Workarounds

| Issue | Solução |
|-------|---------|
| Window não fecha corretamente | Usar `Application.Current.Quit()` |
| DPI scaling distorce fonts | Testar em múltiplas resoluções |
| Keyboard shortcuts não funcionam | Usar button alternative |
| Print dialog não aparece | Usar Share workaround |
| Memory leak em loops | Profile com Windows Performance Analyzer |

---

## 16. WinUI 3 Integration

### Usar WinUI Components

Se necessário components específicos de Windows:

```csharp
#if WINDOWS
    using Microsoft.UI.Xaml.Controls;
    
    // Acesso a WinUI controls
#endif
```

### Hybrid Approach

Pode misturar MAUI XAML com WinUI XAML em casos especiais (avançado).

---

## 17. Checklist para Portar Android → Windows

- [ ] Testei em Windows 10 e Windows 11
- [ ] Testei em 100%, 125%, 150%, 200% DPI
- [ ] Window resize funciona
- [ ] Keyboard navigation (Tab) funciona
- [ ] Dark mode testado
- [ ] File picker funciona
- [ ] Performance aceitável (< 500ms render)
- [ ] Distribuição MSIX verificada

---

## 📌 Referências

- 🔗 Microsoft Docs MAUI: https://docs.microsoft.com/maui/windows
- 🔗 WinUI 3: https://docs.microsoft.com/windows/apps/winui/
- 🔗 Visual Studio: https://visualstudio.microsoft.com

