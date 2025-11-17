# NAVIGEST - macOS Specifics

## 🍎 Considerações Específicas para macOS

Este documento cataloga características, limitações e padrões específicos da plataforma macOS que afetam desenvolvimento MAUI.

---

## 1. Versões Suportadas

- **Mínima:** macOS 11 Big Sur
- **Target:** macOS 12+
- **Recomendada:** macOS 13 (Ventura) ou 14 (Sonoma)

---

## 2. Interface & Window Management

### Window Resizing
```csharp
// Tamanho mínimo da janela
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
        
        // MAUI on macOS não tem API direta, usar platform-specific
    }
}
```

### Fullscreen Mode
macOS permite fullscreen, ao contrário de iOS.

```xaml
<!-- XAML responde a screen size automaticamente -->
<Grid ColumnDefinitions="*" RowDefinitions="Auto,*">
    <!-- Content scala com janela -->
</Grid>
```

---

## 3. Trackpad Gestures

### Dois-Dedos Swipe (Back/Forward)
macOS trackpad tem gestos específicos.

```csharp
// Swipe detection (similar a iOS)
var swipeGesture = new SwipeGestureRecognizer { Direction = SwipeDirection.Left };
swipeGesture.Swiped += (s, e) => Navigation.PopAsync();

frame.GestureRecognizers.Add(swipeGesture);
```

### Trackpad Scroll Momentum
macOS trackpad tem inércia suave.

---

## 4. Mouse & Cursor

### Cursor Customizado

```xaml
<!-- Hover efeito -->
<Button Text="Clique"
        Padding="20"
        BackgroundColor="Blue">
    <Button.GestureRecognizers>
        <TapGestureRecognizer 
            Command="{Binding ClickCommand}" />
    </Button.GestureRecognizers>
</Button>
```

### Cursor Hand na Hover (ex: Links)

No code-behind ou Style:
```csharp
// macOS specific - pode não ser direto em MAUI
// Workaround: Use Button com styling apropriado
```

---

## 5. Keyboard Navigation

### Tab Navigation
macOS espera Tab para navegar entre campos.

```xaml
<VerticalStackLayout>
    <Entry x:Name="Field1" Placeholder="Campo 1" />
    <Entry x:Name="Field2" Placeholder="Campo 2" />
    <Button Text="Submeter" />
</VerticalStackLayout>
```

MAUI suporta automaticamente via Tab key.

### Command Key (⌘)
```csharp
// Em code-behind, não há API direta para ⌘ em MAUI
// Alternativa: Use AppShell keyboard shortcuts

// MauiProgram.cs
builder.Services.AddKeyboardAccelerators(); // Se suportado

// AppShell.xaml
<Shell.BindingContext>
    <local:AppShellBindingContext />
</Shell.BindingContext>
```

---

## 6. Menu Bar & Application Menu

### Application Menu (Não é standard em MAUI)

MAUI não suporta natively o macOS app menu (File, Edit, View, etc).

### Workaround: Menu Button

```xaml
<StackLayout>
    <Button Text="≡ Menu"
            Padding="10">
        <Button.GestureRecognizers>
            <TapGestureRecognizer 
                Command="{Binding ShowMenuCommand}" />
        </Button.GestureRecognizers>
    </Button>
</StackLayout>
```

---

## 7. Resolução & DPI Scaling

### Retina Display (2x pixel density)
macOS Retina tem 2x density. MAUI adapta automaticamente.

```xaml
<!-- FontSize 12 = 24px em Retina -->
<Label Text="Texto" FontSize="12" />
```

### Testar em Resoluções
- 1920x1200 (13" MacBook)
- 2560x1600 (15" MacBook Pro Retina)
- 3072x1920 (14" MacBook Pro)
- 3440x1440 (Ultrawide external monitor)

---

## 8. Dark Mode

### Suporte Automático
macOS 10.14+ suporta Dark Mode. MAUI adapta.

```xaml
<Label Text="Hello"
       TextColor="{AppThemeBinding Light=Black, Dark=White}" />
```

### Forçar Light Mode

No Info.plist:
```xml
<key>NSAppearance</key>
<string>NSAppearanceNameLight</string>
```

---

## 9. Notificações

### Notification Center

```csharp
var notification = new NotificationRequest
{
    NotificationId = 1,
    Title = "Recordar",
    Description = "Não esqueça de guardar as horas",
    Schedule = new NotificationRequestBuilder()
               .AddAppleNotification()
               .Build()
               .Schedule
};

await NotificationCenter.SendAsync(notification);
```

---

## 10. File System & Storage

### Application Support Directory

```csharp
// Dados persistentes (sincronizados com iCloud)
string supportPath = FileSystem.AppDataDirectory;

// Cache (deletável pelo SO)
string cachePath = FileSystem.CacheDirectory;
```

### Acesso a Ficheiros

macOS tem permissões mais restritivas. Pode precisar de entitlements.

```csharp
var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
```

---

## 11. Dock Integration (Avançado)

### Badge Count no Dock

Não suportado diretamente em MAUI. Alternativa: Usar menu.

---

## 12. Code Signing & Provisioning

### Certificado Necessário

Para distribuição via App Store ou assinado:

1. Certificado Developer ID
2. Provisioning profile
3. Entitlements (se necessário)

### Compilação

```bash
dotnet publish -f net8.0-maccatalyst -c Release
```

---

## 13. Performance Considerations

### Window Resizing Performance

```xaml
<!-- Use Grid ao invés de nested StackLayout -->
<Grid RowDefinitions="Auto,*,Auto" ColumnDefinitions="*">
    <!-- Mais eficiente em macOS -->
</Grid>
```

### CollectionView em Janelas Grandes

```xaml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemsLayout>
        <GridItemsLayout 
            Orientation="Vertical"
            HorizontalItemSpacing="10"
            VerticalItemSpacing="10"
            Columns="2" />  <!-- Múltiplas colunas em janela larga -->
    </CollectionView.ItemsLayout>
</CollectionView>
```

---

## 14. Compilação & Debugging

### Debug no Xcode

```bash
dotnet build -f net8.0-maccatalyst -c Debug
# Depois abrir em Xcode para debug avançado
```

### Visual Studio for Mac

Preferir a versão mais recente (baseada em Visual Studio 2022 code).

---

## 15. Conhecidos Issues & Workarounds

| Issue | Solução |
|-------|---------|
| Window não redimensiona bem | Usar Grid em vez de StackLayout |
| Trackpad gesture não funciona | Testar com trackpad real (não mouse) |
| Menu bar não aparece | Usar button menu workaround |
| Dark mode não responde | Forçar re-render com AppTheme change |
| Dock badge não aparece | Não suportado em MAUI, usar alternativa |

---

## 16. Checklist para Portar Android → macOS

- [ ] Testei com múltiplas resoluções (Retina, ultrawide)
- [ ] Window resize funciona sem UI quebra
- [ ] Trackpad gestures testados
- [ ] Dark mode funciona
- [ ] Keyboard navigation (Tab) funciona
- [ ] Performance aceitável (< 500ms render)
- [ ] Fonts display corretamente em Retina
- [ ] Code signing OK (se distribuir)

---

## 📌 Referências

- 🔗 Apple macOS Dev: https://developer.apple.com/macos
- 🔗 MAUI macOS: https://docs.microsoft.com/maui/mac-catalyst

