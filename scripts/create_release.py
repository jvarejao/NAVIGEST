#!/usr/bin/env python3
"""
Script para criar GitHub Release v1.0.2 com upload de APK
Uso: python3 create_release.py <GITHUB_TOKEN>
"""

import os
import sys
import subprocess
import json

def create_release():
    """Criar release via GitHub CLI sem input interativo"""
    
    print("📦 Criando GitHub Release v1.0.2...")
    
    # Caminhos
    repo = "jvarejao/NAVIGEST"
    apk_path = "src/NAVIGEST.Android/bin/Debug/net9.0-android/com.tuaempresa.navigest-arm64-v8a-Signed.apk"
    
    # Verificar APK
    if not os.path.exists(apk_path):
        print(f"❌ APK não encontrado: {apk_path}")
        return False
    
    apk_size = os.path.getsize(apk_path) / (1024**2)
    print(f"✅ APK encontrado: {apk_size:.1f}MB")
    
    # Release notes
    notes = """## ✨ Versão 1.0.2 - Melhorias Gerais

### 🎯 Principais Funcionalidades
- ✅ App Update Checker com detecção automática de versões
- ✅ Indicativo e telefone em campos separados (correção)
- ✅ Download seguro com validação de URL
- ✅ Versão exibida na página de LoginPage
- ✅ Modal alert seguro (não dismissível)
- ✅ Fallback para Browser se Launcher falhar

### 🐛 Correções
- Corrigido erro 'Data too long' no campo TELEFONE
- MainThread enforcement para operações críticas
- HTTP cache bypass para atualização de versão

### 📥 Instalação
1. Fazer download do APK
2. Permitir instalação de fontes desconhecidas
3. Instalar o arquivo

### 📋 Requisitos
- Android 8.0+ (API 26)

### 🔗 Links
- [Documentação](https://github.com/jvarejao/NAVIGEST/blob/main/RELEASES.md)
- [Update Checker](https://github.com/jvarejao/NAVIGEST/blob/main/updates/version.json)"""
    
    # Criar release com gh CLI
    cmd = [
        "gh", "release", "create", "v1.0.2",
        "--repo", repo,
        "--title", "NAVIGEST v1.0.2",
        "--notes", notes,
        apk_path + "#navigest-1.0.2.apk"
    ]
    
    print("🚀 Executando comando...")
    print(f"   {' '.join(cmd[:5])} ...")
    
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        print("✅ Release criada com sucesso!")
        print(f"🔗 https://github.com/{repo}/releases/tag/v1.0.2")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Erro ao criar release:")
        print(f"   {e.stderr}")
        return False
    except FileNotFoundError:
        print("❌ GitHub CLI (gh) não está instalado")
        print("   Instale com: brew install gh")
        return False

if __name__ == "__main__":
    # Verificar autenticação
    try:
        result = subprocess.run(["gh", "auth", "status"], capture_output=True, text=True)
        if result.returncode != 0:
            print("❌ Não autenticado no GitHub")
            print("   Execute: gh auth login")
            sys.exit(1)
    except FileNotFoundError:
        print("❌ GitHub CLI não está instalado")
        sys.exit(1)
    
    # Criar release
    if not create_release():
        sys.exit(1)
