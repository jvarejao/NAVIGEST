#!/usr/bin/env python3
"""
Script para criar release no GitHub rapidamente.
Otimizado com gh CLI - muito mais rápido e direto.
"""

import subprocess
import sys
import os
from pathlib import Path

def get_version_from_args():
    """Obter versão dos argumentos ou de updates/version.json"""
    if len(sys.argv) > 1:
        return sys.argv[1]
    
    # Ler de updates/version.json
    import json
    try:
        with open("updates/version.json", "r") as f:
            data = json.load(f)
            return data.get("version", "")
    except:
        return ""

def create_release(version):
    """Criar release rapidamente usando gh CLI"""
    tag = f"v{version}"
    repo = "jvarejao/NAVIGEST"
    apk_path = f"releases/{tag}/com.navigatorcode.navigest-arm64-v8a-Signed.apk"
    notes_file = f"release_notes_{tag}.md"
    
    # Validar ficheiros
    if not Path(apk_path).exists():
        print(f"❌ APK não encontrado: {apk_path}")
        return False
    
    if not Path(notes_file).exists():
        print(f"❌ Release notes não encontrado: {notes_file}")
        return False
    
    # Criar release
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
            print(f"✅ Release criada: {tag}")
            
            # Mostrar info da release
            info_result = subprocess.run(
                [
                    "gh", "release", "view", tag,
                    "--json", "assets",
                    "-q", "'.assets[0] | \"\(.name) - \(.url)\"'",
                    "--repo", repo
                ],
                capture_output=True,
                text=True,
                timeout=30
            )
            
            if info_result.returncode == 0:
                print(f"📦 {info_result.stdout.strip()}")
            
            print(f"🔗 https://github.com/{repo}/releases/tag/{tag}")
            return True
        else:
            print(f"❌ Erro: {result.stderr}")
            return False
            
    except Exception as e:
        print(f"❌ Erro: {e}")
        return False

if __name__ == "__main__":
    version = get_version_from_args()
    
    if not version:
        print("Uso: python3 create_gh_release.py [versão]")
        print("Exemplo: python3 create_gh_release.py 1.0.17")
        sys.exit(1)
    
    if create_release(version):
        sys.exit(0)
    else:
        sys.exit(1)
