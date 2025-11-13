# 📦 Estrutura de Releases

```
releases/
├── v1.0.2/
│   ├── android/
│   │   └── navigest-1.0.2.apk
│   └── ios/
│       └── navigest-1.0.2.ipa (em breve)
├── v1.0.6/
│   └── android/
  └── com.tuaempresa.navigest-arm64-v8a-Signed.apk
└── README.md
```

## 📥 Downloads Diretos

### v1.0.6 (Pré-release)
- **Android APK arm64** (asset da Release GitHub):
  ```
  https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.6/com.tuaempresa.navigest-arm64-v8a-Signed.apk
  ```

### v1.0.2 (Arquivo)
- **Android APK arm64**: 
  ```
  https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/releases/v1.0.2/android/navigest-1.0.2.apk
  ```
- **iOS IPA**: *(em breve)*

## 🔧 Como Adicionar Nova Versão

1. Criar diretório: `releases/vX.X.X/android` e `releases/vX.X.X/ios`
2. Copiar os ficheiros compilados (APK/IPA)
3. Atualizar `updates/version.json` com URL nova
4. Fazer commit e push para GitHub

## 📋 Exemplo de version.json

```json
{
  "version": "1.0.2",
  "minSupportedVersion": "1.0.0",
  "downloadUrl": "https://raw.githubusercontent.com/jvarejao/NAVIGEST/main/releases/v1.0.2/android/navigest-1.0.2.apk",
  "notes": "✨ Versão 1.0.2 com melhorias"
}
```

## 🚀 Fluxo de Atualização

1. App verifica `updates/version.json` no GitHub
2. Se versão no servidor > versão local, mostra alerta
3. Usuário clica "Atualizar"
4. App abre URL de download (APK do GitHub ou Play Store)
5. Usuário baixa e instala

---

**Nota**: Durante desenvolvimento, usamos GitHub como servidor de downloads. Em produção, migrar para App Store e Google Play Store.
