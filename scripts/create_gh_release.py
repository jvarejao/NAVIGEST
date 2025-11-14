#!/usr/bin/env python3
"""
Script para criar release no GitHub automaticamente.
Usa subprocess para chamar 'gh cli' ou 'curl' com API.
"""

import subprocess
import sys
import os
import json
from pathlib import Path

def create_release_with_gh():
    """Criar release usando GitHub CLI (gh)"""
    version = "1.0.17"
    tag = f"v{version}"
    repo = "jvarejao/NAVIGEST"
    apk_path = f"releases/v{version}/com.navigatorcode.navigest-arm64-v8a-Signed.apk"
    notes_file = "release_notes_v1.0.17.md"
    
    # Verificar se todos os ficheiros existem
    if not Path(apk_path).exists():
        print(f"❌ APK não encontrado: {apk_path}")
        return False
    
    if not Path(notes_file).exists():
        print(f"❌ Release notes não encontrado: {notes_file}")
        return False
    
    # Tentar criar a release
    try:
        print(f"📤 Criando release {tag}...")
        result = subprocess.run(
            [
                "gh", "release", "create", tag,
                apk_path,
                "--title", f"NAVIGEST {tag}",
                "--notes-file", notes_file,
                "--repo", repo,
                "--target", "main"
            ],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode == 0:
            print("✅ Release criada com sucesso!")
            print(result.stdout)
            return True
        else:
            print(f"❌ Erro ao criar release:")
            print(result.stderr)
            return False
            
    except Exception as e:
        print(f"❌ Exceção: {e}")
        return False

def create_release_with_curl():
    """Fallback: criar release usando curl e API do GitHub"""
    version = "1.0.17"
    tag = f"v{version}"
    repo = "jvarejao/NAVIGEST"
    notes_file = "release_notes_v1.0.17.md"
    
    token = os.environ.get("GITHUB_TOKEN", "")
    if not token:
        print("⚠️  GITHUB_TOKEN não definido. Tente: export GITHUB_TOKEN=seu_token")
        return False
    
    # Ler as release notes
    try:
        with open(notes_file, 'r', encoding='utf-8') as f:
            notes = f.read()
    except FileNotFoundError:
        print(f"❌ Release notes não encontrado: {notes_file}")
        return False
    
    # Escapar as notas para JSON
    notes_json = json.dumps(notes)
    
    # Preparar payload
    payload = {
        "tag_name": tag,
        "name": f"NAVIGEST {tag}",
        "body": notes,
        "draft": False,
        "prerelease": False,
        "target_commitish": "main"
    }
    
    try:
        print(f"📤 Criando release {tag} com curl...")
        result = subprocess.run(
            [
                "curl", "-X", "POST",
                "-H", f"Authorization: token {token}",
                "-H", "Accept: application/vnd.github+json",
                f"https://api.github.com/repos/{repo}/releases",
                "-d", json.dumps(payload)
            ],
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode == 0:
            response = json.loads(result.stdout)
            if "id" in response:
                print(f"✅ Release criada! ID: {response['id']}")
                print(f"URL: {response.get('html_url', '')}")
                return True
            else:
                print(f"❌ Erro na resposta: {response.get('message', result.stdout)}")
                return False
        else:
            print(f"❌ Erro curl: {result.stderr}")
            return False
            
    except Exception as e:
        print(f"❌ Exceção: {e}")
        return False

if __name__ == "__main__":
    # Tentar com gh primeiro
    if create_release_with_gh():
        sys.exit(0)
    
    # Se falhar, tentar com curl
    print("\n⚠️  GitHub CLI falhou. Tentando com curl + API...")
    if create_release_with_curl():
        sys.exit(0)
    
    print("\n❌ Falha ao criar release. Tente:")
    print("1. Executar: gh auth login")
    print("2. Ou: export GITHUB_TOKEN=seu_token_aqui")
    sys.exit(1)
