#!/bin/bash

# Script para criar GitHub Release e fazer upload de APK/IPA
# Uso: ./create-release.sh v1.0.3

VERSION=$1

if [ -z "$VERSION" ]; then
    echo "Uso: ./create-release.sh <versão>"
    echo "Exemplo: ./create-release.sh v1.0.3"
    exit 1
fi

echo "📦 Preparando release para $VERSION..."

# Verificar se gh (GitHub CLI) está instalado
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI não instalado"
    echo "Instalar: brew install gh"
    exit 1
fi

# Preparar diretório de release
RELEASE_DIR="releases/${VERSION#v}/android"
mkdir -p "$RELEASE_DIR"

echo "✅ Estrutura criada em $RELEASE_DIR"
echo ""
echo "Próximos passos:"
echo "1. Compilar: dotnet publish -f net9.0-android -c Release"
echo "2. Copiar APK para: $RELEASE_DIR/navigest-${VERSION#v}.apk"
echo "3. Atualizar updates/version.json com version: ${VERSION#v}"
echo "4. Fazer commit e push"
echo "5. Executar: gh release create $VERSION -t 'NAVIGEST $VERSION' -n 'Novidades em v${VERSION#v}' --draft"
echo "6. Upload do APK: gh release upload $VERSION $RELEASE_DIR/*.apk"
echo "7. Publicar release"
