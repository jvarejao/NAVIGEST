#!/bin/bash

# Script para criar GitHub Release com upload de APK
# Uso: ./create-release.sh v1.0.2

set -e

VERSION=${1:-v1.0.2}
REPO="jvarejao/NAVIGEST"
APK_PATH="src/NAVIGEST.Android/bin/Debug/net9.0-android/com.tuaempresa.navigest-arm64-v8a-Signed.apk"
RELEASE_NAME="NAVIGEST $VERSION"

echo "📦 Criando GitHub Release $VERSION..."

# Verificar se APK existe
if [ ! -f "$APK_PATH" ]; then
    echo "❌ Erro: APK não encontrado em $APK_PATH"
    exit 1
fi

# Obter tamanho do APK
APK_SIZE=$(ls -lh "$APK_PATH" | awk '{print $5}')
echo "📁 APK: $APK_SIZE"

# Verificar se gh CLI está instalado
if ! command -v gh &> /dev/null; then
    echo "❌ Erro: GitHub CLI (gh) não está instalado"
    echo "   Instale com: brew install gh"
    exit 1
fi

# Verificar autenticação
if ! gh auth status &> /dev/null; then
    echo "❌ Erro: Não autenticado no GitHub"
    echo "   Execute: gh auth login"
    exit 1
fi

# Criar a release
echo "🚀 Criando release..."
gh release create "$VERSION" \
    --repo "$REPO" \
    --title "$RELEASE_NAME" \
    --notes "## ✨ Versão $VERSION

### 🎯 Principais Melhorias
- ✅ App Update Checker com detecção automática
- ✅ Indicativo e telefone separados (correção)
- ✅ Download seguro com validação de URL
- ✅ Versão exibida no LoginPage

### 📥 Instalação
1. Fazer download do APK
2. Ativar 'Fontes desconhecidas' em Segurança
3. Instalar o arquivo

### 📋 Requisitos Mínimos
- Android 8.0+ (API 26)

### 🔗 Links
- [GitHub Releases](https://github.com/jvarejao/NAVIGEST/releases)
- [Documentação](https://github.com/jvarejao/NAVIGEST/blob/main/RELEASES.md)
" \
    --draft=false \
    "$APK_PATH#navigest-${VERSION}.apk"

echo "✅ Release $VERSION criada com sucesso!"
echo "🔗 URL: https://github.com/$REPO/releases/tag/$VERSION"
