# 📋 Processo de Release

Guia completo para criar e publicar novas versões da aplicação NAVIGEST.

## 🎯 Fluxo de Release

```
1. Desenvolvimento → 2. Build → 3. GitHub Release → 4. Update Checker → 5. Usuário Atualiza
```

## 📝 Pré-requisitos

- GitHub CLI instalado: `brew install gh`
- Acesso de push ao repositório
- APK compilado em Release mode
- Versão atualizada em código

## 🚀 Passo a Passo

### 1. Atualizar Versão em Código

Atualizar `MauiProgram.cs` ou arquivo de versão:
```csharp
// MauiProgram.cs ou local equivalente
const string APP_VERSION = "1.0.3";
```

### 2. Compilar em Release

```bash
# Android
dotnet publish -f net9.0-android -c Release

# iOS (opcional)
dotnet publish -f net9.0-ios -c Release
```

### 3. Organizar Ficheiros

```bash
# Criar diretório da versão
mkdir -p releases/v1.0.3/android
mkdir -p releases/v1.0.3/ios

# Copiar APK compilado
cp src/NAVIGEST.Android/bin/Release/net9.0-android/com.tuaempresa.navigest-arm64-v8a.apk \
   releases/v1.0.3/android/navigest-1.0.3.apk

# Copiar IPA (se disponível)
# cp build_output/navigest.ipa releases/v1.0.3/ios/navigest-1.0.3.ipa
```

### 4. Atualizar version.json

```json
{
  "version": "1.0.3",
  "minSupportedVersion": "1.0.0",
  "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.3/navigest-1.0.3.apk",
  "notes": "✨ Novidades em v1.0.3\n🐛 Correções de bugs\n🚀 Melhorias de performance"
}
```

### 5. Commit e Push

```bash
git add updates/version.json releases/v1.0.3/
git commit -m "chore: Prepare v1.0.3 release"
git push
```

### 6. Criar Release no GitHub

**Opção A: Via GitHub CLI**

```bash
# Criar release (draft)
gh release create v1.0.3 \
  --title "NAVIGEST v1.0.3" \
  --notes "✨ Novidades em v1.0.3" \
  --draft

# Upload dos ficheiros
gh release upload v1.0.3 \
  releases/v1.0.3/android/navigest-1.0.3.apk \
  releases/v1.0.3/ios/navigest-1.0.3.ipa

# Publicar
gh release edit v1.0.3 --draft=false
```

**Opção B: Via Interface Web**

1. Ir a: https://github.com/jvarejao/NAVIGEST/releases
2. Clicar "Draft a new release"
3. Tag: `v1.0.3`
4. Title: `NAVIGEST v1.0.3`
5. Description: Descrever novidades
6. Upload do APK/IPA
7. Publicar

### 7. Verificar Update Checker

1. Instalar versão anterior (1.0.2)
2. Iniciar app
3. Deve mostrar alerta de atualização disponível
4. Clicar "Atualizar"
5. Deve fazer download do GitHub Release

## 📦 Estrutura de Ficheiros

```
releases/
├── v1.0.2/
│   ├── android/
│   │   ├── .gitkeep
│   │   └── navigest-1.0.2.apk (em GitHub Releases, não no Git)
│   └── ios/
│       ├── .gitkeep
│       └── navigest-1.0.2.ipa (em GitHub Releases, não no Git)
├── v1.0.3/
│   ├── android/
│   │   └── .gitkeep
│   └── ios/
│       └── .gitkeep
└── README.md
```

## 🔒 Update Checker (Versão Obrigatória)

Para forçar atualização (exemplo: segurança crítica):

```json
{
  "version": "1.0.4",
  "minSupportedVersion": "1.0.4",  // Força atualização
  "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.4/navigest-1.0.4.apk",
  "notes": "🔒 Atualização obrigatória - Correção de segurança crítica"
}
```

## ⚙️ Automação (Futuro)

Pode-se usar GitHub Actions para:
- Build automático ao fazer tag
- Upload automático para Release
- Atualizar version.json automaticamente

Exemplo workflow:
```yaml
name: Release Build
on:
  push:
    tags: ['v*']
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Build Android
        run: dotnet publish -f net9.0-android -c Release
      - name: Upload to Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            src/NAVIGEST.Android/bin/Release/net9.0-android/*.apk
```

## 📊 Histórico de Releases

Ver todas as releases: https://github.com/jvarejao/NAVIGEST/releases

## 🆘 Troubleshooting

### APK muito grande (>100MB)
- Normal para MAUI com muitas dependências
- GitHub suporta ficheiros até 2GB
- Solução futura: comprimir ou splitar por ABI

### Download lento
- GitHub CDN é robusto mas pode variar por localização
- Considerar mirror secundário (Azure Blob Storage, etc)

### versão.json não atualiza
- Limpar cache do navegador (força refresh)
- Verificar URL está correta
- Adicionar header `Cache-Control: no-cache` em requisições

---

**Última atualização**: 10 de Novembro de 2025
**Versão**: 1.0.2
