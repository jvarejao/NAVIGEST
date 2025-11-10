# 🚀 Como Executar: Criar GitHub Release v1.0.2

## 📋 Checklist Completo

- [x] App compila sem erros
- [x] Update Checker funcional
- [x] Telefone/Indicativo separados
- [x] Script criado
- [ ] **Token GitHub criado** ← Próximo passo!

---

## ✅ Passo a Passo

### 1️⃣ Gerar Personal Access Token (PAT)

```
1. Abrir: https://github.com/settings/tokens/new
2. Nome: "NAVIGEST Release Script"
3. Expiração: 90 dias
4. Escopos: ✅ repo (toda)
5. Gerar token
6. COPIAR o token (aparece uma única vez!)
```

⚠️ **Não esquecer de copiar - não aparece novamente!**

---

### 2️⃣ Autenticar GitHub CLI

```bash
gh auth login

# Responder às perguntas:
# ? What is your preferred protocol for Git operations? HTTPS
# ? Authenticate Git with your GitHub credentials? Yes
# ? How would you like to authenticate GitHub CLI? Paste an authentication token
# Paste your token here and press Enter
```

Verificar:
```bash
gh auth status
# Deve mostrar: ✓ Logged in to github.com as jvarejao
```

---

### 3️⃣ Criar GitHub Release v1.0.2

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
./scripts/create-release.sh v1.0.2
```

Saída esperada:
```
📦 Criando GitHub Release v1.0.2...
📁 APK: 125M
🚀 Criando release...
✅ Release v1.0.2 criada com sucesso!
🔗 URL: https://github.com/jvarejao/NAVIGEST/releases/tag/v1.0.2
```

---

### 4️⃣ Verificar Release no GitHub

Ir a: https://github.com/jvarejao/NAVIGEST/releases/tag/v1.0.2

Deve mostrar:
- ✅ Tag: v1.0.2
- ✅ APK: navigest-v1.0.2.apk (125 MB)
- ✅ Release notes com funcionalidades
- ✅ Link para download

---

### 5️⃣ Testar Download

Baixar o APK:
```bash
curl -L https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.2/navigest-v1.0.2.apk \
  -o ~/Desktop/navigest-test.apk
```

---

## 🧪 Teste Final: Update Checker

1. Abrir app com v1.0.0
2. Ver alerta: "Atualização Disponível v1.0.2"
3. Clicar "Atualizar"
4. Abre link do GitHub Release
5. Fazer download e instalar APK
6. LoginPage mostra "Versão 1.0.2" ✅

---

## 🔒 Segurança

✅ **O que foi feito certo:**
- Token criado com escopo limitado (`repo`)
- Token NÃO commited no repositório
- Script não contém credenciais
- `gh` CLI armazena token no keychain (macOS)

⚠️ **Importante:**
- Se vazar o token, revogar em: https://github.com/settings/tokens
- Tokens com `repo` podem acessar repositórios privados
- Sempre usar HTTPS para comunicação

---

## 📊 Próximas Versões

Para criar v1.0.3:

```bash
# 1. Atualizar versão em MauiProgram.cs ou App.xaml.cs
# 2. Build novo
# 3. Execute:
./scripts/create-release.sh v1.0.3

# 4. Atualizar updates/version.json:
{
  "version": "1.0.3",
  "downloadUrl": "https://github.com/jvarejao/NAVIGEST/releases/download/v1.0.3/navigest-v1.0.3.apk"
}

# 5. Commit e push
git add updates/version.json
git commit -m "chore: update to v1.0.3"
git push
```

A app detectará automaticamente! 🎉

---

## ❓ FAQ

**P: Posso usar outros repositórios para hosting?**
R: Sim! Alterar `downloadUrl` em `updates/version.json` para:
- AWS S3, Azure Blob Storage, CDN privado
- Qualquer URL HTTP/HTTPS

**P: E se perder o token?**
R: Gerar novo em https://github.com/settings/tokens/new

**P: Como revogar acesso?**
R: https://github.com/settings/tokens → Delete

**P: Quanto espaço no GitHub?**
R: Releases suportam até 2GB por arquivo

---

## 🎯 Status Atual

```
✅ Update Checker: 100% funcional
✅ GitHub Releases: Estruturado
✅ Script de automação: Pronto
⏳ Release v1.0.2: Aguardando criação
```

**Próxima ação**: Executar os 5 passos acima! 🚀
