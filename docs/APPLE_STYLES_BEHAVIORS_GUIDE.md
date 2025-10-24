# 🎨 Guia de Estilos Apple + Behaviors

## 📱 **AppleStyles.xaml - Design System Integrado**

Este documento explica como usar os estilos Apple com behaviors integrados para animações e validação.

---

## 🎯 **Estilos Disponíveis**

### 1. **AppleEntry** (Entry Básico)
Entry básico com design iOS/macOS nativo.

```xml
<Entry Style="{StaticResource AppleEntry}"
       Placeholder="Email"
       Keyboard="Email" />
```

**Características:**
- ✅ Animação de scale 1.02 quando em foco
- ✅ Mudança de cor de fundo quando focado
- ✅ Altura 44pt (padrão iOS)
- ✅ Font 17pt (padrão iOS)

---

### 2. **AppleEntryValidated** (Entry com Validação) 🆕
Entry com validação automática e underline colorido.

```xml
<Entry Style="{StaticResource AppleEntry}"
       Placeholder="Email"
       Keyboard="Email">
    <Entry.Behaviors>
        <beh:EntryValidationBehavior ValidationKind="Email" />
    </Entry.Behaviors>
</Entry>
```

**Características:**
- 🔵 **Azul** (#2563EB) quando em foco
- 🟢 **Verde** (#22C55E) quando válido
- 🔴 **Vermelho** (#EF4444) quando inválido
- 🎯 **Validação automática** para: Email, Phone, Numeric, Password

**Tipos de Validação:**
```xml
<!-- Detecção Automática -->
<beh:EntryValidationBehavior ValidationKind="Auto" />

<!-- Email -->
<beh:EntryValidationBehavior ValidationKind="Email" />

<!-- Telefone (formato português) -->
<beh:EntryValidationBehavior ValidationKind="Phone" />

<!-- Numérico -->
<beh:EntryValidationBehavior ValidationKind="Numeric" />

<!-- Password (mínimo 8 caracteres) -->
<beh:EntryValidationBehavior ValidationKind="Password" />

<!-- Sem validação -->
<beh:EntryValidationBehavior ValidationKind="None" />
```

---

### 3. **AppleFilledButton** (Button Básico)
Button com design iOS/macOS nativo.

```xml
<Button Style="{StaticResource AppleFilledButton}"
        Text="Entrar"
        Clicked="OnLoginClicked" />
```

**Características:**
- ✅ Animação de scale 0.98 quando pressionado
- ✅ Animação de scale 1.02 quando hover (macOS)
- ✅ Sombra iOS-style
- ✅ Altura 50pt
- ✅ Corner radius 10pt

---

### 4. **AppleFilledButtonAnimated** (Button com Fade-in) 🆕
Button com animação de fade-in suave ao aparecer.

```xml
<Button Style="{StaticResource AppleFilledButton}"
        Text="Entrar"
        Clicked="OnLoginClicked">
    <Button.Behaviors>
        <beh:ButtonMicroAnimationsBehavior />
    </Button.Behaviors>
</Button>
```

**Características:**
- 🎬 **Fade-in** (Opacity 0→1) ao aparecer na tela
- 🎬 **Slide-up** (TranslationY 8→0) ao aparecer na tela
- 🎯 **Scale 0.98** quando pressionado (80ms CubicOut)
- 🎯 **Scale 1.0** quando solto (120ms CubicOut)

---

## 🛠️ **Como Usar**

### **1. Adicionar namespace no XAML**
```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:beh="clr-namespace:NAVIGEST.iOS.Behaviors"
             x:Class="NAVIGEST.iOS.Pages.LoginPage">
```

### **2. Usar Entry com Validação**
```xml
<!-- Email com validação -->
<Entry Style="{StaticResource AppleEntry}"
       Placeholder="Email"
       Keyboard="Email">
    <Entry.Behaviors>
        <beh:EntryValidationBehavior ValidationKind="Email" />
    </Entry.Behaviors>
</Entry>

<!-- Password com validação (mín. 8 caracteres) -->
<Entry Style="{StaticResource AppleEntry}"
       Placeholder="Password"
       IsPassword="True">
    <Entry.Behaviors>
        <beh:EntryValidationBehavior ValidationKind="Password" />
    </Entry.Behaviors>
</Entry>

<!-- Telefone com validação -->
<Entry Style="{StaticResource AppleEntry}"
       Placeholder="Contacto"
       Keyboard="Telephone">
    <Entry.Behaviors>
        <beh:EntryValidationBehavior ValidationKind="Phone" />
    </Entry.Behaviors>
</Entry>
```

### **3. Usar Button com Animação Fade-in**
```xml
<!-- Button com fade-in suave -->
<Button Style="{StaticResource AppleFilledButton}"
        Text="Guardar"
        Clicked="OnSaveClicked">
    <Button.Behaviors>
        <beh:ButtonMicroAnimationsBehavior />
    </Button.Behaviors>
</Button>
```

---

## 📋 **Exemplo Completo (LoginPage)**

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:beh="clr-namespace:NAVIGEST.iOS.Behaviors"
             x:Class="NAVIGEST.iOS.Pages.LoginPage"
             Title="Entrar">

    <VerticalStackLayout Padding="20" Spacing="16">
        
        <!-- Email com validação automática -->
        <Entry Style="{StaticResource AppleEntry}"
               Placeholder="Email"
               Keyboard="Email"
               x:Name="entryEmail">
            <Entry.Behaviors>
                <beh:EntryValidationBehavior ValidationKind="Email" />
            </Entry.Behaviors>
        </Entry>

        <!-- Password com validação (mín. 8 chars) -->
        <Entry Style="{StaticResource AppleEntry}"
               Placeholder="Password"
               IsPassword="True"
               x:Name="entryPassword">
            <Entry.Behaviors>
                <beh:EntryValidationBehavior ValidationKind="Password" />
            </Entry.Behaviors>
        </Entry>

        <!-- Button com animação fade-in -->
        <Button Style="{StaticResource AppleFilledButton}"
                Text="Entrar"
                Clicked="OnLoginClicked">
            <Button.Behaviors>
                <beh:ButtonMicroAnimationsBehavior />
            </Button.Behaviors>
        </Button>

    </VerticalStackLayout>

</ContentPage>
```

---

## 🎨 **Outros Estilos Disponíveis**

### **Buttons:**
- `AppleFilledButton` - Button primário (azul iOS)
- `AppleTintedButton` - Button secundário (cinza)
- `AppleTextButton` - Button texto (sem fundo)
- `AppleDestructiveButton` - Button destrutivo (vermelho)

### **Outros Componentes:**
- `ApplePicker` - Picker iOS-style
- `AppleCard` - Card com sombra iOS
- `AppleLargeTitle` - Título grande iOS
- `AppleHeadline` - Headline
- `AppleBody` - Texto corpo
- `AppleCaption` - Texto pequeno

---

## 🔧 **Comportamento dos Behaviors**

### **EntryValidationBehavior**
- **Auto-detecção:** Detecta tipo de validação baseado no `Keyboard` e `IsPassword`
- **Validação em tempo real:** Valida ao digitar e ao perder foco
- **Underline colorido:** 
  - Android: Usa `BackgroundTintList`
  - Windows: Usa `BorderBrush`
  - iOS/macOS: Usa cor de fundo

### **ButtonMicroAnimationsBehavior**
- **Fade-in:** Animação suave ao aparecer (160ms CubicOut)
- **Slide-up:** Desliza de baixo para cima (8pt)
- **Press feedback:** Scale 0.98 instantâneo
- **Release animation:** Volta a 1.0 suavemente (120ms)

---

## ⚠️ **Notas Importantes**

1. **Namespace iOS:** Em iOS, usar `xmlns:beh="clr-namespace:NAVIGEST.iOS.Behaviors"`
2. **Namespace macOS:** Em macOS, usar `xmlns:beh="clr-namespace:NAVIGEST.macOS.Behaviors"`
3. **Namespace Android:** Em Android, usar `xmlns:beh="clr-namespace:NAVIGEST.Android.Behaviors"`

4. **Validação é opcional:** Não é obrigatório usar `EntryValidationBehavior` em todos os Entry. Use apenas onde precisar de validação.

5. **Animação fade-in é opcional:** Use `ButtonMicroAnimationsBehavior` apenas se quiser o efeito de fade-in. A animação de press (scale 0.98) já está no estilo base.

---

## 📱 **Plataformas Suportadas**

- ✅ **iOS** - Totalmente suportado
- ✅ **macOS** - Totalmente suportado  
- ⏳ **Android** - Behaviors precisam ser copiados para `NAVIGEST.Android/Behaviors/`
- ❌ **Windows** - Underline validation parcialmente suportado

---

## 🚀 **Próximos Passos**

1. ✅ Aplicar behaviors nas páginas iOS/macOS existentes
2. ⏳ Copiar behaviors para projeto Android
3. ⏳ Testar validação em todas as plataformas
4. ⏳ Adicionar mais tipos de validação (CPF, IBAN, etc.)

---

**Criado:** 2024  
**Última atualização:** Hoje  
**Versão:** 1.0
