# 🔐 Como Criar GitHub Release com Segurança

## Opção 1: GitHub CLI com Token (Recomendado)

### Passo 1: Gerar Personal Access Token (PAT)

1. Ir a: https://github.com/settings/tokens/new
2. Criar novo token com escopo `repo` (apenas repos)
3. Copiar o token (aparece uma única vez!)
4. **NÃO commitar** o token no código!

### Passo 2: Autenticar GitHub CLI

```bash
gh auth login
# Escolher: GitHub.com
# Protocolo: HTTPS
# Y para usar GitHub CLI
# Colar o token quando pedido
```

### Passo 3: Criar Release

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
chmod +x scripts/create-release.sh
./scripts/create-release.sh v1.0.2
```

O script fará:
- ✅ Verificar APK
- ✅ Criar release v1.0.2 no GitHub
- ✅ Upload do APK automaticamente
- ✅ Atualizar release notes

---

## Opção 2: Upload Manual (Mais Seguro)

Se preferir não usar token, fazer upload manual:

1. Ir a: https://github.com/jvarejao/NAVIGEST/releases/new
2. Tag: `v1.0.2`
3. Title: `NAVIGEST v1.0.2`
4. Release notes (copiar do `RELEASES.md`)
5. Arrastar APK para upload
6. Publicar

---

## Segurança do Token

⚠️ **IMPORTANTE**:
- Tokens são sensíveis como senhas
- Nunca commitar em repositório
- Revogar se vazado: https://github.com/settings/tokens
- Token com escopo `repo` só acessa repositórios públicos
- Validade: pode definir expiração (ex: 90 dias)

---

## Verificar Autenticação

```bash
gh auth status
# Deve mostrar: Logged in to github.com
```

---

## Após Criar Release

A URL estará disponível para download:
```
https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.2/navigest-v1.0.2.apk
```

A app irá detectar automaticamente quando `updates/version.json` for atualizado!

---

## Troubleshooting

### "gh auth status" retorna erro
```bash
# Reautenticar
gh auth logout
gh auth login
```

### APK muito grande (>100MB)
GitHub permite até 2GB por arquivo em releases.

### Token expirou
Gerar novo token em https://github.com/settings/tokens/new

---

**Proxima Release**: Após fazer v1.0.2, para v1.0.3:
1. Atualizar versão no código
2. Build novo
3. `./scripts/create-release.sh v1.0.3`
4. Pronto!
