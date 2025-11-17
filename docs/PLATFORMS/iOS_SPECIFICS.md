# NAVIGEST - iOS Specifics

## 📱 Considerações Específicas para iOS

Este documento cataloga características, limitações e padrões específicos da plataforma iOS que afetam desenvolvimento MAUI.

---

## 1. Versões Suportadas

- **Mínima:** iOS 14
- **Target:** iOS 16+
- **Recomendada:** iOS 17+

**Impacto:** Features mais recentes podem não funcionar em iOS 14.

---

## 2. Safe Area (Notch, Home Indicator)

### Comportamento Padrão
iOS respeita safe areas (espaço sem conteúdo sob notch ou home indicator).

### Padrão Recomendado

MAUI trata automaticamente, mas verificar em XAML:
```xaml
<Grid RowDefinitions="Auto,*,Auto" 
      Padding="{OnPlatform Default=0, iOS='0,0,0,20'}">
    <!-- Header -->
    <Label Text="Título" Grid.Row="0" />
    
    <!-- Content (cresce no meio) -->
    <ScrollView Grid.Row="1">
        <VerticalStackLayout>
            <!-- Content aqui -->
        </VerticalStackLayout>
    </ScrollView>
    
    <!-- Footer (acima home indicator) -->
    <Button Text="Guardar" Grid.Row="2" />
</Grid>
```

### Detectar Safe Area em Code
```csharp
var safeAreaInsets = Application.Current!.MainPage!.SafeAreaInsets;
var topInset = safeAreaInsets.Top;
var bottomInset = safeAreaInsets.Bottom;
```

---

## 3. Gestos (Swipe, Pinch, etc)

### Swipe Gesture
```xaml
<Grid>
    <Grid.GestureRecognizers>
        <SwipeGestureRecognizer Direction="Left" 
                                Command="{Binding SwipeLeftCommand}" />
        <SwipeGestureRecognizer Direction="Right" 
                                Command="{Binding SwipeRightCommand}" />
    </Grid.GestureRecognizers>
</Grid>
```

### Pull-to-Refresh
```xaml
<RefreshView Command="{Binding RefreshCommand}"
             IsRefreshing="{Binding IsRefreshing}">
    <CollectionView ItemsSource="{Binding Items}">
        <!-- Items -->
    </CollectionView>
</RefreshView>
```

### Long Press
```xaml
<Label Text="Long press me">
    <Label.GestureRecognizers>
        <TapGestureRecognizer 
            NumberOfTapsRequired="1"
            Command="{Binding LongPressCommand}"
            CommandParameter="{Binding .}" />
    </Label.GestureRecognizers>
</Label>
```

---

## 4. Permissões (Info.plist)

### Permissões Comuns Exigem Descrição

**iOS** exige descrição em `Info.plist` para **cada** permissão pedida.

### Template Info.plist

```xml
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Privacy descriptions (OBRIGATÓRIO) -->
    <key>NSCameraUsageDescription</key>
    <string>Preciso de acesso à câmera para tirar fotos</string>
    
    <key>NSPhotoLibraryUsageDescription</key>
    <string>Preciso de acesso à galeria para selecionar imagens</string>
    
    <key>NSContactsUsageDescription</key>
    <string>Preciso de acesso aos contactos</string>
    
    <key>NSLocationWhenInUseUsageDescription</key>
    <string>Preciso da localização para esta função</string>
    
    <!-- Outros settings -->
    <key>NSBonjourServices</key>
    <array/>
</dict>
</plist>
```

**Sem estas descrições, app pode ser rejeitado na App Store.**

### Request de Permission

```csharp
var status = await Permissions.CheckStatusAsync<Permissions.Camera>();

if (status != PermissionStatus.Granted)
{
    status = await Permissions.RequestAsync<Permissions.Camera>();
}
```

---

## 5. Tamanhos de Fonte & UI Scale

### Preferência de Tamanho de Fonte do User

iOS permite user selecionar "Large Text", "Extra Large", etc nas settings.

### Adaptar Fonte

```xaml
<!-- XAML responde automaticamente se usar named sizes -->
<Label Text="Título" 
       FontSize="Large"
       FontAttributes="Bold" />

<!-- Ou dynamic -->
<Label Text="Descrição"
       FontSize="{AppThemeBinding Light=12, Dark=14}" />
```

### Em Code-behind

```csharp
var fontSize = DeviceDisplay.MainDisplayInfo.Density * 12; // 12pt base
```

---

## 6. Scrolling & Momentum

### Comportamento Padrão
iOS tem "momentum scrolling" (continua scrollar após libertar). MAUI herda este comportamento.

### Testar Performance
```xaml
<CollectionView ItemsSource="{Binding Items}">
    <CollectionView.ItemsLayout>
        <LinearItemsLayout Orientation="Vertical" 
                           ItemSpacing="10" />
    </CollectionView.ItemsLayout>
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <StackLayout Padding="20">
                <Label Text="{Binding Name}" FontSize="16" />
                <Label Text="{Binding Description}" FontSize="12" />
            </StackLayout>
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

---

## 7. Keyboard Appearance

### Safe Keyboard Dismiss

```xaml
<Entry x:Name="InputField"
       ReturnType="Done"
       Completed="OnEntryCompleted" />
```

```csharp
private void OnEntryCompleted(object? sender, EventArgs e)
{
    InputField.IsEnabled = false;
    InputField.IsEnabled = true; // Hide keyboard
}
```

Ou:
```csharp
InputField.Unfocus();
```

---

## 8. Temas & Aparência (Light/Dark)

### Suporte Automático
iOS 13+ suporta Dark Mode. MAUI adapta automaticamente se usar AppThemeBinding.

```xaml
<Label Text="Hello"
       TextColor="{AppThemeBinding Light=Black, Dark=White}" />

<BoxView Color="{AppThemeBinding Light={StaticResource LightBackground}, 
                                   Dark={StaticResource DarkBackground}}" />
```

### Forçar Light Mode (se necessário)

No `Info.plist`:
```xml
<key>UIUserInterfaceStyle</key>
<string>Light</string>  <!-- Ou "Dark" -->
```

---

## 9. Notificações (Push & Local)

### Push Notifications (APNS)

Requer certificado Apple Developer.

```csharp
// Pedir permissão
await NotificationCenter.RequestAuthorizationAsync();

// Registar no APNs
var deviceToken = await GetDeviceTokenAsync();
SendToServer(deviceToken); // Enviar para backend guardar
```

### Local Notifications

```csharp
var notification = new NotificationRequest
{
    NotificationId = 101,
    Title = "Recordar",
    Description = "Não esqueça de guardar as horas",
    Schedule = new NotificationRequestBuilder()
               .AddAppleNotification()
               .AddBadge(1)
               .Build()
               .Schedule
};

await NotificationCenter.SendAsync(notification);
```

---

## 10. Storage (iCloud, Local)

### App Sandbox
iOS app tem acesso apenas ao seu próprio directory sandbox.

```csharp
// App documents (sincronizados com iCloud se ativar)
string appDataPath = FileSystem.AppDataDirectory;

// Cache (não sincronizado)
string cachePath = FileSystem.CacheDirectory;
```

### Habilitar iCloud Sync (Optional)

Requer entitlement no provisioning profile:
```xml
<key>com.apple.developer.ubiquity-container-identifiers</key>
<array>
    <string>iCloud.com.yourcompany.navigest</string>
</array>
```

---

## 11. Screen Lock Prevention

### Prevenir Lock Enquanto App em Foreground

```csharp
public partial class [PageName] : ContentPage
{
    public [PageName]()
    {
        InitializeComponent();
        
        // Impede lock enquanto nesta página
        DeviceDisplay.KeepScreenOn = true;
    }
    
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DeviceDisplay.KeepScreenOn = false; // Re-permitir lock
    }
}
```

---

## 12. Provisioning & Code Signing

### Certificados Necessários
1. **Development Certificate** - Para compilar em device
2. **App ID** - Identificador único (ex: com.navigatorcode.navigest)
3. **Provisioning Profile** - Liga device + certificate + app

### Processo

1. Ir a Apple Developer Account
2. Criar App ID
3. Registar device (UDID)
4. Criar provisioning profile
5. Descarregar certificado + profile
6. Importar em Xcode/Visual Studio

**Documentação completa:** `iOS_PROVISIONING.md`

---

## 13. Compilação para Device vs Simulator

### Compilar para Simulator (desenvolvimento rápido)
```bash
dotnet build -f net8.0-ios -c Debug
```

### Compilar para Device (testes reais)
```bash
dotnet build -f net8.0-ios -c Release
```

Exige provisioning profile válido.

---

## 14. Conhecidos Issues & Workarounds

| Issue | Solução |
|-------|---------|
| ListView não scrolls em iPad | Usar CollectionView |
| Font não responde AppThemeBinding | Forçar re-render com trigger |
| Safe area padding duplo | Verificar Grid row/col definitions |
| Keyboard covers Entry | Usar ScrollView |
| GIF animation lenta | Usar ImageView + cache |

---

## 15. Checklist para Portar Android → iOS

- [ ] Testei em device + simulator
- [ ] Safe area respeitado (sem notch overlap)
- [ ] Gestos funcionam (swipe, long press)
- [ ] Permissões em Info.plist
- [ ] Dark mode testado
- [ ] Keyboard dismiss funciona
- [ ] Performance aceitável
- [ ] App Store guidelines

---

## 📌 Referências

- 🔗 Apple Dev: https://developer.apple.com
- 🔗 MAUI iOS: https://docs.microsoft.com/maui/ios
- 🔗 Provisioning: `iOS_PROVISIONING.md`

