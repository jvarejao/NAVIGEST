# NAVIGEST v1.0.19 - Correção Crítica

## 🐛 Correções de Bugs

### Correção do Popup de Seleção de Famílias
- ✅ **Corrigido erro de sintaxe XAML** no AppThemeBinding (linha 116)
- ✅ **Alterado método de invocação do popup** para `Application.Current.MainPage.ShowPopupAsync`
- ✅ Popup de famílias agora abre corretamente ao clicar no botão +
- ✅ Funcionalidade de criação de novas famílias totalmente operacional

### Detalhes Técnicos
- Corrigido: `Light:#007AFF` → `Light=#007AFF` no AppThemeBinding
- Alterado: `this.ShowPopupAsync()` → `Application.Current.MainPage.ShowPopupAsync()`
- Plataforma: Android

---

## 📥 Download

**APK Android:** [com.navigatorcode.navigest-arm64-v8a-Signed.apk](https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.19/com.navigatorcode.navigest-arm64-v8a-Signed.apk)

---

## 🔄 Atualização Automática

Esta versão inclui sistema de auto-update. A aplicação verificará automaticamente por novas versões ao iniciar.

---

## 📱 Plataformas Suportadas

- ✅ **Android** (arm64-v8a, armeabi-v7a, x86, x86_64)
- 🚧 iOS (em desenvolvimento)
- 🚧 macOS Catalyst (em desenvolvimento)

---

**Data de lançamento:** 14 de novembro de 2025
