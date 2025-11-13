#!/bin/bash

# Automação para criar release Android no GitHub.
# Pré-requisitos: dotnet, gh, git, ambiente limpo e versão já atualizada no código.
# Uso: ./scripts/create-release.sh 1.0.7 [--notes ficheiro] [--skip-build] [--target main]

set -euo pipefail

usage() {
    echo "Uso: $0 <versao> [--notes ficheiro] [--skip-build] [--target branch]" >&2
    exit 1
}

if [[ $# -lt 1 ]]; then
    usage
fi

VERSION=""
NOTES_FILE=""
SKIP_BUILD=0
TARGET="main"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --notes|-n)
            shift
            [[ $# -gt 0 ]] || usage
            NOTES_FILE="$1"
            ;;
        --skip-build)
            SKIP_BUILD=1
            ;;
        --target)
            shift
            [[ $# -gt 0 ]] || usage
            TARGET="$1"
            ;;
        -h|--help)
            usage
            ;;
        *)
            if [[ -z "$VERSION" ]]; then
                VERSION="$1"
            else
                usage
            fi
            ;;
    esac
    shift
done

if [[ -z "$VERSION" ]]; then
    usage
fi

if [[ "$VERSION" == v* ]]; then
    TAG="$VERSION"
    VERSION="${VERSION:1}"
else
    TAG="v$VERSION"
fi

REPO="${REPO:-jvarejao/NAVIGEST}"
PROJECT="src/NAVIGEST.Android/NAVIGEST.Android.csproj"
FRAMEWORK="net9.0-android"
APK_PATH="src/NAVIGEST.Android/bin/Release/${FRAMEWORK}/com.tuaempresa.navigest-arm64-v8a-Signed.apk"
ASSET_LABEL="com.tuaempresa.navigest-arm64-v8a-Signed.apk"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Erro: dotnet não encontrado no PATH." >&2
    exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
    echo "Erro: GitHub CLI (gh) não encontrado." >&2
    exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
    echo "Erro: autenticação do gh não configurada. Executa 'gh auth login'." >&2
    exit 1
fi

if ! command -v git >/dev/null 2>&1; then
    echo "Erro: git não encontrado." >&2
    exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
    echo "Erro: diretório git com alterações pendentes. Faz commit ou stash antes de continuar." >&2
    exit 1
fi

if [[ -n "$NOTES_FILE" && ! -f "$NOTES_FILE" ]]; then
    echo "Erro: ficheiro de notas '$NOTES_FILE' não encontrado." >&2
    exit 1
fi

if ! grep -q "<ApplicationDisplayVersion>$VERSION</ApplicationDisplayVersion>" "$PROJECT"; then
    echo "Erro: ApplicationDisplayVersion em $PROJECT não está definido para $VERSION." >&2
    exit 1
fi

if ! grep -q "\"version\":\"$VERSION\"" updates/version.json; then
    echo "Erro: updates/version.json não está alinhado com a versão $VERSION." >&2
    exit 1
fi

CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
echo "Branch atual: $CURRENT_BRANCH"

echo "⬆️  A sincronizar $CURRENT_BRANCH com origin..."
git push origin "$CURRENT_BRANCH"

if [[ $SKIP_BUILD -eq 0 ]]; then
    echo "🔨 A publicar APK Release (${FRAMEWORK})..."
    dotnet publish "$PROJECT" -c Release -f "$FRAMEWORK"
else
    echo "⚠️  A publicar foi ignorado (--skip-build)."
fi

if [[ ! -f "$APK_PATH" ]]; then
    echo "Erro: APK Release não encontrado em $APK_PATH" >&2
    exit 1
fi

if [[ -z "$NOTES_FILE" ]]; then
    TEMP_NOTES=$(mktemp /tmp/navigest-release-notes-XXXX.md)
    cat <<EOF > "$TEMP_NOTES"
## Destaques
- Atualizações para a versão $VERSION.

## Build
- $ASSET_LABEL
EOF
    NOTES_FILE="$TEMP_NOTES"
    trap 'rm -f "$TEMP_NOTES"' EXIT
fi

if gh release view "$TAG" >/dev/null 2>&1; then
    echo "Erro: a release $TAG já existe. Remove-a ou usa outra versão." >&2
    exit 1
fi

echo "� A criar release $TAG no repositório $REPO..."
gh release create "$TAG" \
    "$APK_PATH#$ASSET_LABEL" \
    --repo "$REPO" \
    --title "NAVIGEST $TAG" \
    --notes-file "$NOTES_FILE" \
    --target "$TARGET"

echo "✅ Release publicada: https://github.com/$REPO/releases/tag/$TAG"
