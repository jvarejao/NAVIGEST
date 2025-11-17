# NAVIGEST - Platform-Specific Guides

## 📱 Documentação de Plataformas

Guias detalhados para cada plataforma suportada pelo NAVIGEST.

---

## Plataformas Suportadas

### 📱 Android
**Ficheiro:** `ANDROID_SPECIFICS.md`

Características específicas do Android:
- Rotação de ecrã (portrait/landscape)
- Teclado virtual
- Back button navigation
- Permissions runtime
- Performance em dispositivos variados

**Use quando:** Desenvolver em Android ou adaptar componente para Android.

---

### 🍎 iOS
**Ficheiro:** `iOS_SPECIFICS.md`

Características específicas do iOS:
- Safe areas (notch handling)
- Gestos (swipe, pull-to-refresh)
- Permissões (Info.plist)
- Font scaling
- App Store requirements

**Use quando:** Portar componente para iOS.

---

### 🍎 macOS
**Ficheiro:** `macOS_SPECIFICS.md`

Características específicas do macOS:
- Window management
- Retina display (2x density)
- Trackpad gestures
- Keyboard navigation
- Menu bar patterns

**Use quando:** Portar componente para macOS ou desktop features.

---

### 🪟 Windows
**Ficheiro:** `WINDOWS_SPECIFICS.md`

Características específicas do Windows:
- DPI scaling (100-200%)
- Window resizing
- File dialogs
- Keyboard shortcuts
- MSIX packaging

**Use quando:** Portar componente para Windows.

---

## 🔗 Cross-Platform

### CROSS_PLATFORM_GUIDE.md
Padrões e boas práticas que funcionam em todas as plataformas.

---

## 📋 Comparação Rápida

| Característica | Android | iOS | macOS | Windows |
|-----------|---------|-----|-------|---------|
| Rotação | ✅ Portrait/Landscape | ⏸️ Portrait | ✅ Any | ✅ Any |
| Safe Area | ❌ | ✅ (notch) | ⚠️ Menu bar | ❌ |
| Gestos | ❌ (swipe lib) | ✅ Native | ✅ Trackpad | ⏸️ Mouse |
| Keyboard Nav | ✅ Basic | ✅ Basic | ✅ Full (Tab, ⌘) | ✅ Full (Tab, CTRL) |
| Storage | ✅ Sandbox | ✅ Sandbox | ✅ Sandbox | ✅ Sandbox |
| Permissions | ✅ Runtime | ✅ Runtime | ⚠️ Entitlements | ⚠️ UAC |
| Dark Mode | ✅ | ✅ | ✅ | ✅ |
| DPI Scaling | ✅ xhdpi/xxhdpi | ✅ @2x/@3x | ✅ @2x (Retina) | ✅ 100-200% |

---

## 🚀 Workflow de Portação

### Passo 1: Implementar em Android
- Plataforma padrão de desenvolvimento
- Ler `ANDROID_SPECIFICS.md`
- Testar em device/emulator

### Passo 2: Documentar
- Usar `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md`
- Indicar padrões reutilizáveis

### Passo 3: Portar para iOS
- Ler `iOS_SPECIFICS.md`
- Seguir checklist em componente
- Testar em simulator

### Passo 4: Portar para macOS
- Ler `macOS_SPECIFICS.md`
- Adaptar layout (window resizing)
- Testar em Mac ou simulator

### Passo 5: Portar para Windows
- Ler `WINDOWS_SPECIFICS.md`
- Testar em múltiplas resoluções DPI
- Testar keyboard shortcuts

---

## 🎯 Common Pitfalls

### Safe Area (iOS/macOS)
```xaml
<!-- ❌ ERRADO -->
<Label Text="Title" 
       Margin="0,0,0,0" />

<!-- ✅ CORRETO -->
<Label Text="Title" 
       Margin="{OnPlatform iOS='0,20,0,0', Default='0,0,0,0'}" />
```

### Keyboard Navigation (Windows/macOS)
```xaml
<!-- ❌ ERRADO - Sem ordem de navegação -->
<Entry />
<Button />

<!-- ✅ CORRETO - Define ordem com TabIndex -->
<Entry TabIndex="0" />
<Button TabIndex="1" />
```

### DPI Scaling (Windows)
```xaml
<!-- ❌ ERRADO - Pixel-perfect sizes -->
<Frame WidthRequest="200" HeightRequest="100" />

<!-- ✅ CORRETO - Relative sizes -->
<Frame WidthRequest="200" HeightRequest="100" />
<!-- A escalar automático no render -->
```

---

## 📚 Recursos Adicionais

- 🔗 MAUI Docs: https://docs.microsoft.com/maui
- 🔗 COMPONENTS: Consultar `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md` para template
- 🔗 iOS Provisioning: `iOS_PROVISIONING.md`

---

## ✅ Checklist Universal para Qualquer Plataforma

- [ ] Compilação sem erros
- [ ] Execução sem crashes
- [ ] Testes básicos funcionam
- [ ] UI não está distorcida
- [ ] Performance aceitável (< 1s load)
- [ ] Sem console errors/warnings
- [ ] Documentação atualizada

---

## 🔄 Sincronização de Mudanças

Quando fazer mudança que afeta múltiplas plataformas:

1. Fazer mudança em Android (referência)
2. Validar em Android
3. Copiar para iOS/macOS/Windows
4. Testar em cada plataforma
5. Documentar em `PLATFORM_SYNC/`
6. Commit com mensagem clear (ex: "feat(core): Add X support to all platforms")

Ver: `PLATFORM_SYNC/WORKFLOW.md`

