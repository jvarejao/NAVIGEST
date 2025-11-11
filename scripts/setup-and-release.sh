#!/bin/bash

# Script para autenticar GitHub CLI e criar release com segurança

echo "╔════════════════════════════════════════════════════════════════════╗"
echo "║          GitHub Release Authentication & Upload                   ║"
echo "╚════════════════════════════════════════════════════════════════════╝"
echo ""

# Verificar se gh está instalado
if ! command -v gh &> /dev/null; then
    echo "❌ GitHub CLI não está instalado"
    echo "   Instale com: brew install gh"
    exit 1
fi

echo "📋 PASSO 1: Gerar Personal Access Token"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "1. Abrir: https://github.com/settings/tokens/new"
echo "2. Nome: NAVIGEST Release Script"
echo "3. Expiração: 90 dias"
echo "4. Escopos: ✅ repo (toda)"
echo "5. Gerar token"
echo "6. COPIAR o token (aparece uma única vez!)"
echo ""

read -p "Press ENTER quando tiver copiado o token..."

echo ""
echo "📋 PASSO 2: Autenticar GitHub CLI"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Será aberto um browser para autenticação segura."
echo "Se preferir modo interativo, será pedido o token..."
echo ""

# Tentar autenticação interativa
gh auth login --web 2>/dev/null || gh auth login

# Verificar se conseguiu autenticar
if ! gh auth status &> /dev/null; then
    echo "❌ Falha na autenticação"
    exit 1
fi

echo ""
echo "✅ Autenticado com sucesso!"
gh auth status

echo ""
echo "📋 PASSO 3: Criar GitHub Release"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd /Users/joaovarejao/Dev/NAVIGEST

./scripts/create-release.sh v1.0.2

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Release criada com sucesso!"
    echo ""
    echo "📋 PASSO 4: Verificar Release"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "Abrir no browser:"
    echo "  https://github.com/jvarejao/NAVIGEST/releases/tag/v1.0.2"
    echo ""
    echo "Deve mostrar:"
    echo "  ✅ Tag: v1.0.2"
    echo "  ✅ APK: navigest-v1.0.2.apk (125 MB)"
    echo "  ✅ Release notes automáticas"
    echo ""
    echo "📋 PASSO 5: Testar App"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "1. Abrir app com v1.0.0"
    echo "2. Ver alerta: 'Atualização Disponível v1.0.2'"
    echo "3. Clicar 'Atualizar'"
    echo "4. Abre GitHub Release (fazer download)"
    echo "5. Instalar APK"
    echo "6. LoginPage mostra 'Versão 1.0.2' ✅"
    echo ""
    echo "╔════════════════════════════════════════════════════════════════════╗"
    echo "║  🎉 RELEASE v1.0.2 CRIADA COM SUCESSO!                            ║"
    echo "╚════════════════════════════════════════════════════════════════════╝"
else
    echo "❌ Erro ao criar release"
    exit 1
fi
