# ✅ CHECKLIST PRÁTICO - SETUP NAVIGEST + GITHUB

## FASE 0: Preparação (5 minutos)

### Passo 1: Verificar Git instalado
```bash
git --version
```
**O que você deve ver**: `git version 2.46.0` (ou similar)

**Seu resultado**:
- [ ] Comando funcionou
- [ ] Versão apareceu

---

### Passo 2: Configurar Git (PRIMEIRA VEZ APENAS)
```bash
git config --global user.name "João Varejão"
git config --global user.email "seu-email@gmail.com"
```

**Verificar que funcionou**:
```bash
git config --global user.name
git config --global user.email
```

**Seu resultado**:
- [ ] Nome apareceu
- [ ] Email apareceu

---

## FASE 1: Criar NAVIGEST Localmente (10 minutos)

### Passo 3: Criar pasta NAVIGEST

```bash
cd /Users/joaovarejao/Dev
mkdir NAVIGEST
cd NAVIGEST
pwd
```

**O que você deve ver**: `/Users/joaovarejao/Dev/NAVIGEST`

**Seu resultado**:
- [ ] Pasta criada
- [ ] Caminho correto

---

### Passo 4: Inicializar repositório Git

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git init
ls -la
```

**O que você deve ver**: 
```
total 0
drwxr-xr-x   3 user  staff   96 Oct 20 14:30 .
drwxr-xr-x   5 user  staff  160 Oct 20 14:30 ..
drwxr-xr-x  11 user  staff  352 Oct 20 14:30 .git
```

**Seu resultado**:
- [ ] `.git` folder apareceu
- [ ] `git init` funcionou

---

### Passo 5: Criar ficheiro .gitignore

Copie e execute EXATAMENTE isto (todo de uma vez):

```bash
cat > /Users/joaovarejao/Dev/NAVIGEST/.gitignore << 'EOF'
bin/
obj/
*.dll
*.exe
packages/
.vs/
.vscode/
*.suo
*.user
.DS_Store
*.log
.idea/
appsettings.local.json
secrets.json
EOF
```

**Verificar**:
```bash
cat /Users/joaovarejao/Dev/NAVIGEST/.gitignore
```

**Seu resultado**:
- [ ] Ficheiro criado
- [ ] Conteúdo apareceu

---

### Passo 6: Criar README.md

```bash
cat > /Users/joaovarejao/Dev/NAVIGEST/README.md << 'EOF'
# NAVIGEST

Multi-platform mobile and desktop application with .NET MAUI.

## Structure
- `NAVIGEST.Shared` - Common code
- `NAVIGEST.iOS` - iOS app with Face ID
- `NAVIGEST.Android` - Android app with Fingerprint
- `NAVIGEST.Windows` - Windows desktop app

## Build
```bash
dotnet build
```
EOF
```

**Verificar**:
```bash
cat /Users/joaovarejao/Dev/NAVIGEST/README.md
```

**Seu resultado**:
- [ ] Ficheiro criado
- [ ] README apareceu

---

## FASE 2: Primeiro Commit Local (5 minutos)

### Passo 7: Ver status do Git

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git status
```

**O que você deve ver**:
```
On branch master

No commits yet

Untracked files:
  (use "git add <file>..." to include in what will be committed)
        .gitignore
        README.md
```

**Seu resultado**:
- [ ] Vê `.gitignore` e `README.md`

---

### Passo 8: Adicionar ficheiros ao commit

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git add .
```

**Verificar**:
```bash
git status
```

**O que deve mudar**:
```
On branch master

No commits yet

Changes to be committed:
  (use "git rm --cached <file>..." to unstage)
        new file:   .gitignore
        new file:   README.md
```

**Seu resultado**:
- [ ] Ficheiros mostram como "Changes to be committed"

---

### Passo 9: Criar primeiro commit

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git commit -m "Initial commit: project structure and documentation"
```

**O que você deve ver**:
```
[master (root-commit) abc1234] Initial commit: project structure
 2 files changed, 15 insertions(+)
 create mode 100644 .gitignore
 create mode 100644 README.md
```

**Seu resultado**:
- [ ] Commit criado
- [ ] Hash apareceu (ex: abc1234)

---

### Passo 10: Ver histórico de commits

```bash
git log --oneline
```

**O que você deve ver**:
```
abc1234 Initial commit: project structure and documentation
```

**Seu resultado**:
- [ ] Vê seu commit

---

## FASE 3: Conectar ao GitHub (10 minutos)

### Passo 11: Ir ao GitHub e criar novo repositório

1. Abra browser: https://github.com
2. Login com sua conta
3. Clique **"New"** ou vá a https://github.com/new
4. Preencha assim:
   - **Repository name**: `NAVIGEST` (não AppLoginMaui!)
   - **Description**: `Multi-platform mobile and desktop app`
   - **Public** (ou Private se preferir)
   - ❌ **NÃO** marque "Initialize this repository with a README"
5. Clique **"Create repository"**

**Você verá uma página com**:
```
…or push an existing repository from the command line

git remote add origin https://github.com/SEU-USERNAME/NAVIGEST.git
git branch -M main
git push -u origin main
```

**Seu resultado**:
- [ ] Repositório criado no GitHub
- [ ] Vê a URL (cópia para próximo passo)

---

### Passo 12: Adicionar GitHub como repositório remoto

```bash
cd /Users/joaovarejao/Dev/NAVIGEST

# IMPORTANTE: Mude SEU-USERNAME para o seu username do GitHub
git remote add origin https://github.com/SEU-USERNAME/NAVIGEST.git

# Verificar
git remote -v
```

**O que você deve ver**:
```
origin  https://github.com/SEU-USERNAME/NAVIGEST.git (fetch)
origin  https://github.com/SEU-USERNAME/NAVIGEST.git (push)
```

**Seu resultado**:
- [ ] Remote adicionado
- [ ] Mostra fetch e push

---

### Passo 13: Renomear branch para "main"

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git branch -M main
```

**Verificar**:
```bash
git branch
```

**O que você deve ver**:
```
* main
```

**Seu resultado**:
- [ ] Branch renomeado para main

---

### Passo 14: Fazer primeiro PUSH para GitHub

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git push -u origin main
```

**Primeira vez**: Pode pedir login. Se usar HTTPS:
- Username: seu username GitHub
- Password: **Personal Access Token** (não senha!)

**Como gerar Personal Access Token**:
1. GitHub → Settings (canto superior direito)
2. Developer settings → Personal access tokens → Tokens (classic)
3. "Generate new token"
4. Marque apenas `repo`
5. Clique "Generate token"
6. **COPIE o token** (nunca mais aparece!)
7. Guarde num ficheiro seguro ou no Keychain

**O que você deve ver**:
```
Enumerating objects: 3, done.
Counting objects: 100% (3/3), done.
Writing objects: 100% (3/3), 267 bytes | 267.00 KiB/s, done.
Total 3 (delta 0), reused 0 (delta 0), pack-reused 0
To https://github.com/SEU-USERNAME/NAVIGEST.git
 * [new branch]      main -> main
Branch 'main' set up to track remote branch 'main' from 'origin'.
```

**Seu resultado**:
- [ ] Push bem-sucedido
- [ ] Vê "new branch"

---

## FASE 4: Verificação Final (5 minutos)

### Passo 15: Verificar no GitHub

1. Abra: https://github.com/SEU-USERNAME/NAVIGEST
2. Você deve ver:
   - [ ] 2 ficheiros (README.md, .gitignore)
   - [ ] Branch "main"
   - [ ] 1 commit

---

## ✅ CHECKLIST FINAL

Marque quando terminar:

### Setup Local
- [ ] Git instalado e configurado
- [ ] Pasta NAVIGEST criada
- [ ] `.git` inicializado
- [ ] `.gitignore` criado
- [ ] `README.md` criado
- [ ] Primeiro commit feito

### GitHub
- [ ] Repositório criado no GitHub
- [ ] Remote adicionado (`git remote -v`)
- [ ] Branch renomeado para `main`
- [ ] Push bem-sucedido
- [ ] Ficheiros visíveis no GitHub

---

## 🚀 PRÓXIMO PASSO

Quando tudo acima estiver ✅:

1. **PARAR AQUI**
2. Avisar-me que terminou
3. Vamos começar **FASE 1** do projeto:
   - Criar `NAVIGEST.Shared` (class library)
   - Copiar ficheiros do AppLoginMaui
   - Fazer commits

---

## 🆘 SE ALGO DER ERRADO

### Erro: "fatal: not a git repository"
```bash
# Você está na pasta errada, ir para NAVIGEST
cd /Users/joaovarejao/Dev/NAVIGEST
```

### Erro: "Username for 'https://github.com':"
```bash
# Digitar seu username GitHub e pressionar Enter
# Depois: colar Personal Access Token e pressionar Enter
```

### Erro: "remote origin already exists"
```bash
# Removeu origin e adicionar novamente
git remote remove origin
git remote add origin https://github.com/SEU-USERNAME/NAVIGEST.git
```

### Não conseguir gerar Personal Access Token?
1. GitHub → Settings
2. Developer settings
3. Personal access tokens
4. Tokens (classic)
5. "Generate new token (classic)"
6. Marque apenas `repo`
7. Expire: 90 days
8. Clique "Generate token"

---

## 📞 CONTACTE QUANDO:

- [ ] Terminou todos os passos
- [ ] Algo não funcionou
- [ ] Tem dúvida num passo

