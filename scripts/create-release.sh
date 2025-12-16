#!/bin/bash

# Automação para criar release Android no GitHub.
# Pré-requisitos: dotnet, gh, git, ambiente limpo e versão já atualizada no código.
# Uso: ./scripts/create-release.sh [versao] [--notes ficheiro] [--skip-build] [--target main]

set -euo pipefail

usage() {
    echo "Uso: $0 [versao] [--notes ficheiro] [--skip-build] [--target branch]" >&2
    echo "A versão é lida de Directory.Build.props; argumento é opcional e só valida coerência." >&2
    exit 1
}

VERSION_ARG=""
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
            if [[ -z "$VERSION_ARG" ]]; then
                VERSION_ARG="$1"
            else
                usage
            fi
            ;;
    esac
    shift
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROPS_FILE="$REPO_ROOT/Directory.Build.props"
VERSION_JSON="$REPO_ROOT/updates/version.json"

if [[ ! -f "$PROPS_FILE" ]]; then
    echo "Erro: Directory.Build.props não encontrado em $PROPS_FILE" >&2
    exit 1
fi

PROPS_VERSION=$(grep -oE '<Version>[^<]+' "$PROPS_FILE" | head -1 | sed 's/<Version>//')

if [[ -z "$PROPS_VERSION" ]]; then
    echo "Erro: não foi possível ler <Version> de $PROPS_FILE" >&2
    exit 1
fi

if [[ -n "$VERSION_ARG" && "$VERSION_ARG" != "$PROPS_VERSION" ]]; then
    echo "Erro: versão fornecida ($VERSION_ARG) difere da versão em Directory.Build.props ($PROPS_VERSION)." >&2
    exit 1
fi

VERSION="$PROPS_VERSION"

if [[ "$VERSION" == v* ]]; then
    TAG="$VERSION"
    VERSION="${VERSION:1}"
else
    TAG="v$VERSION"
fi

REPO="${REPO:-jvarejao/NAVIGEST}"
PROJECT="$REPO_ROOT/src/NAVIGEST.Android/NAVIGEST.Android.csproj"
FRAMEWORK="net9.0-android"
APK_PATH="$REPO_ROOT/src/NAVIGEST.Android/bin/Release/${FRAMEWORK}/com.navigatorcode.navigest-arm64-v8a-Signed.apk"
ASSET_LABEL="com.navigatorcode.navigest-arm64-v8a-Signed.apk"

cd "$REPO_ROOT"

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

if [[ ! -f "$VERSION_JSON" ]]; then
    echo "Erro: $VERSION_JSON não encontrado." >&2
    exit 1
fi

if ! grep -q "\"version\":\"$VERSION\"" "$VERSION_JSON"; then
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
