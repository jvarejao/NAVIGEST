#!/bin/bash

# Script para criar GitHub Personal Access Token de forma segura
# Este script abre o GitHub no browser para gerar o token

set -e

echo "=================================="
echo "🔐 GitHub PAT Setup"
echo "=================================="
echo ""
echo "Este script vai abrir o GitHub para criar um Personal Access Token."
echo ""
echo "Instruções:"
echo "1. Vai abrir GitHub.com no browser automaticamente"
echo "2. Na página, clica em 'Generate new token (classic)'"
echo "3. Preenche:"
echo "   - Token name: NAVIGEST-Release (ou o que quiseres)"
echo "   - Expiration: 90 days (ou mais)"
echo "   - Scopes: MARCA APENAS 'repo' (todo o acesso ao repo)"
echo "4. Clica 'Generate token'"
echo "5. COPIA O TOKEN (só vais ver uma vez!)"
echo "6. Cola aqui:"
echo ""

# Abrir a página de criar token
TOKEN_URL="https://github.com/settings/tokens/new?scopes=repo&description=NAVIGEST-Release"
echo "Abrindo: $TOKEN_URL"

# Tentar diferentes formas de abrir browser
if command -v open &> /dev/null; then
    open "$TOKEN_URL"
elif command -v xdg-open &> /dev/null; then
    xdg-open "$TOKEN_URL"
elif command -v start &> /dev/null; then
    start "$TOKEN_URL"
else
    echo "⚠️  Não consegui abrir o browser. Abre manualmente:"
    echo "$TOKEN_URL"
fi

echo ""
echo "=================================="
echo "Aguardando token..."
echo "=================================="
echo ""

# Ler token do utilizador (sintaxe macOS/bash)
read -sp "Cole o token aqui (não vai aparecer texto): " TOKEN

if [ -z "$TOKEN" ]; then
    echo ""
    echo "❌ Token vazio. Abortando."
    exit 1
fi

echo ""
echo ""

# Guardar token em ~/.config/navigest/github-token (seguro)
TOKEN_DIR="$HOME/.config/navigest"
TOKEN_FILE="$TOKEN_DIR/github-token"

mkdir -p "$TOKEN_DIR"
chmod 700 "$TOKEN_DIR"

# Guardar token
echo "$TOKEN" > "$TOKEN_FILE"
chmod 600 "$TOKEN_FILE"

echo "✅ Token guardado em: $TOKEN_FILE"
echo ""
echo "=================================="
echo "Testando autenticação..."
echo "=================================="
echo ""

# Testar o token
export GITHUB_TOKEN="$TOKEN"

if gh auth status; then
    echo ""
    echo "✅ Autenticação bem-sucedida!"
    echo ""
    echo "Token pode ser usado assim:"
    echo "  export GITHUB_TOKEN=\"\$(cat $TOKEN_FILE)\""
    echo ""
    echo "Ou automaticamente (próxima vez):"
    echo "  source ~/.config/navigest/github-token.env"
    echo ""
else
    echo ""
    echo "❌ Autenticação falhou. Verifica o token."
    rm "$TOKEN_FILE"
    exit 1
fi

# Criar arquivo para sourcing
cat > "$TOKEN_DIR/github-token.env" << 'EOF'
#!/bin/bash
# Carregar GitHub token automaticamente
if [ -f "$HOME/.config/navigest/github-token" ]; then
    export GITHUB_TOKEN="$(cat "$HOME/.config/navigest/github-token")"
fi
EOF

chmod 600 "$TOKEN_DIR/github-token.env"

echo "🎉 Pronto! Token configurado com sucesso!"
echo ""
echo "Próximo passo: Criar a release"
echo "  ./scripts/create-release.sh v1.0.2"
