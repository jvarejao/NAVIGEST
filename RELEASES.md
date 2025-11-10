# 📦 Releases e Downloads

Este documento descreve como obter e testar as versões da aplicação NAVIGEST.

## 🔗 Download dos Ficheiros

### Via GitHub Releases (Recomendado)

Os ficheiros compilados estão disponíveis em [**GitHub Releases**](https://github.com/jvarejao/NAVIGEST/releases):

- **Android APK**: [v1.0.2 Release](https://github.com/jvarejao/NAVIGEST/releases/tag/v1.0.2)
- **iOS IPA**: *(em breve)*

### Download Direto (URLs brutas para automação)

```
Android v1.0.2:
https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.2/navigest-1.0.2.apk

iOS v1.0.2:
https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.2/navigest-1.0.2.ipa
```

## 📖 Configuração de Update Checker

A aplicação verifica automaticamente novas versões usando:

**Ficheiro de configuração**: `updates/version.json`
```json
{
  "version": "1.0.2",
  "minSupportedVersion": "1.0.0",
  "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.2/navigest-1.0.2.apk",
  "notes": "✨ Versão 1.0.2 com melhorias"
}
```

## 🚀 Instalação

### Android
1. Ir a [Releases](https://github.com/jvarejao/NAVIGEST/releases)
2. Fazer download do `navigest-X.X.X.apk`
3. Permitir instalação de fontes desconhecidas em Configurações
4. Abrir o arquivo e instalar

### iOS
1. Ir a [Releases](https://github.com/jvarejao/NAVIGEST/releases)
2. Fazer download do `navigest-X.X.X.ipa`
3. Usar Xcode ou aplicativo de sideload (Altstore, etc)
4. Instalar no dispositivo

## 🔄 Fluxo de Atualização

1. **App inicia**: Verifica `updates/version.json` no GitHub
2. **Comparação**: Se versão remota > versão local, mostra alerta
3. **Usuário escolhe**: 
   - "Atualizar" → Abre download do GitHub Release
   - "Depois" → Fecha alerta
4. **Instalação**: Usuário baixa e instala novo APK/IPA

## 📋 Versões Disponíveis

| Versão | Data | Android | iOS | Status |
|--------|------|---------|-----|--------|
| 1.0.2 | 10/11/2025 | ✅ | ⏳ | **Atual** |
| 1.0.1 | 09/11/2025 | ✅ | ✅ | Anterior |
| 1.0.0 | 01/11/2025 | ✅ | ✅ | Inicial |

## 🔧 Como Criar Nova Release

1. **Build local**:
   ```bash
   dotnet publish -f net9.0-android -c Release
   ```

2. **Copiar ficheiro compilado**:
   ```bash
   cp src/NAVIGEST.Android/bin/Release/net9.0-android/com.tuaempresa.navigest-arm64-v8a.apk releases/v1.0.3/android/navigest-1.0.3.apk
   ```

3. **Atualizar version.json**:
   ```json
   {
     "version": "1.0.3",
     "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.3/navigest-1.0.3.apk"
   }
   ```

4. **Commit e push**:
   ```bash
   git add updates/version.json
   git commit -m "chore: update to v1.0.3"
   git push
   ```

5. **Criar Release no GitHub**:
   - Ir a [Releases](https://github.com/jvarejao/NAVIGEST/releases)
   - Clicar "Draft a new release"
   - Tag: `v1.0.3`
   - Upload do APK/IPA
   - Publicar

---

**Nota**: Os ficheiros APK/IPA não são armazenados no Git (muito grandes). Use [GitHub Releases](https://github.com/jvarejao/NAVIGEST/releases) para downloads.
