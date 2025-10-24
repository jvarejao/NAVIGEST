# 📚 GUIA GITHUB PARA NAVIGEST - Para Iniciantes

## 🎯 O QUE VAMOS FAZER

1. ✅ Limpar o repositório GitHub (remover AppLoginMaui)
2. ✅ Criar NAVIGEST localmente
3. ✅ Fazer upload para GitHub
4. ✅ Aprender a fazer commits regularmente

---

## 📋 ESTRUTURA FINAL NO GITHUB

```
seu-username/AppLoginMaui (na verdade vai ser NAVIGEST)
├── NAVIGEST.Shared/
│   ├── Services/
│   ├── ViewModels/
│   ├── Models/
│   ├── Resources/
│   └── ...
├── NAVIGEST.iOS/
├── NAVIGEST.Android/
└── NAVIGEST.Windows/
```

---

## 🚀 PASSO A PASSO - LINHA POR LINHA

### PASSO 1: Verificar se você tem Git instalado

```bash
git --version
```

✅ Se aparecer versão (ex: `git version 2.46.0`), você já tem Git.

---

### PASSO 2: Configurar sua identidade Git (PRIMEIRA VEZ APENAS)

```bash
git config --global user.name "João Varejão"
git config --global user.email "seu-email@example.com"
```

⚠️ Use o **mesmo email que registou no GitHub!**

---

### PASSO 3: Criar pasta NAVIGEST localmente

```bash
cd /Users/joaovarejao/Dev
mkdir -p NAVIGEST
cd NAVIGEST
```

---

### PASSO 4: Inicializar repositório Git LOCAL

```bash
cd /Users/joaovarejao/Dev/NAVIGEST
git init
```

Isso cria uma pasta `.git` escondida que rastreia mudanças.

---

### PASSO 5: Criar ficheiro .gitignore (muito importante!)

Este ficheiro **diz ao Git o que NÃO guardar** (arquivos desnecessários):

```bash
cat > /Users/joaovarejao/Dev/NAVIGEST/.gitignore << 'EOF'
# Build
bin/
obj/
*.dll
*.exe
*.o
*.so
*.a

# Visual Studio & Code
.vs/
.vscode/
*.suo
*.user
*.userosscache
*.sln.docstates

# Dependencies
packages/
.nuget/

# macOS
.DS_Store
.AppleDouble
.LSOverride

# Temporary files
*.tmp
*.temp
*~
.swp

# IDE
.idea/
*.swp
*.swo

# Secrets
appsettings.local.json
secrets.json

# Logs
*.log
logs/
EOF
cat /Users/joaovarejao/Dev/NAVIGEST/.gitignore
```

---

### PASSO 6: Criar README.md (apresentação do projeto)

```bash
cat > /Users/joaovarejao/Dev/NAVIGEST/README.md << 'EOF'
# NAVIGEST - Multi-Platform Mobile & Desktop Application

## Overview
NAVIGEST is a modern .NET MAUI application that provides:
- **iOS**: Face ID authentication
- **Android**: Fingerprint authentication
- **Windows**: Desktop desktop application
- **Shared Core**: Common services, models, and business logic

## Architecture
- `NAVIGEST.Shared` - Shared code (Services, ViewModels, Models, Resources)
- `NAVIGEST.iOS` - iOS-specific implementation
- `NAVIGEST.Android` - Android-specific implementation
- `NAVIGEST.Windows` - Windows desktop application

## Building

### Prerequisites
- .NET 9 SDK
- Visual Studio 2022 or Visual Studio Code with C# extension

### Build NAVIGEST.Shared
```bash
cd NAVIGEST.Shared
dotnet build
```

### Build iOS
```bash
cd NAVIGEST.iOS
dotnet build -f net9.0-ios
```

### Build Android
```bash
cd NAVIGEST.Android
dotnet build -f net9.0-android
```

## License
Proprietary

## Author
João Varejão
EOF
cat /Users/joaovarejao/Dev/NAVIGEST/README.md
```

---

### PASSO 7: Fazer primeiro COMMIT local

Um **commit** é como um "checkpoint" que guarda suas mudanças.

```bash
cd /Users/joaovarejao/Dev/NAVIGEST

# Adicionar todos os ficheiros ao commit
git add .

# Criar o commit com uma mensagem
git commit -m "Initial commit: project setup"

# Ver o histórico de commits
git log --oneline
```

Você verá algo como:
```
abc1234 Initial commit: project setup
```

---

### PASSO 8: Conectar a um repositório no GitHub

Primeiro, você precisa:

1. **Ir para GitHub.com**
2. **Criar novo repositório**:
   - Nome: `NAVIGEST` (não AppLoginMaui!)
   - Descrição: "NAVIGEST - Multi-platform mobile and desktop app"
   - ✅ Público (ou Privado se preferir)
   - ❌ NÃO marque "Initialize with README"
   - Clique "Create Repository"

3. **Copiar o URL** que aparece (ex: `https://github.com/seu-username/NAVIGEST.git`)

---

### PASSO 9: Adicionar repositório remoto ao Git local

```bash
cd /Users/joaovarejao/Dev/NAVIGEST

# Adicionar repositório GitHub como "origin" (servidor remoto)
git remote add origin https://github.com/seu-username/NAVIGEST.git

# Verificar que foi adicionado
git remote -v
```

Você verá:
```
origin  https://github.com/seu-username/NAVIGEST.git (fetch)
origin  https://github.com/seu-username/NAVIGEST.git (push)
```

---

### PASSO 10: Fazer PUSH (enviar código para GitHub)

```bash
# Renomear branch master para main (padrão GitHub moderno)
git branch -M main

# Enviar commits para GitHub
git push -u origin main
```

⚠️ **Primeira vez**: Pode pedir credenciais. Use:
- Username: seu username GitHub
- Password: **seu Personal Access Token** (não senha normal!)

**Como gerar Personal Access Token**:
1. GitHub.com → Settings → Developer settings → Personal access tokens
2. Tokens (classic)
3. "Generate new token"
4. Marque: `repo` (acesso completo a repositórios)
5. Copie o token e guarde num ficheiro seguro

---

## 📖 DEPOIS - WORKFLOW NORMAL (CADA VEZ QUE FIZER MUDANÇAS)

### Ciclo Típico: Edit → Commit → Push

```bash
# 1. Fazer mudanças nos ficheiros (criar código, etc)
#    (usando VS Code ou editor)

# 2. Ver quais ficheiros mudaram
cd /Users/joaovarejao/Dev/NAVIGEST
git status

# Você verá algo como:
# On branch main
# Changes not staged for commit:
#   modified: NAVIGEST.Shared/Services/DatabaseService.cs
#   new file: NAVIGEST.iOS/Pages/LoginPage.xaml

# 3. Adicionar mudanças ao próximo commit
git add .

# 4. Criar commit (checkpoint com mensagem)
git commit -m "feat: add DatabaseService to NAVIGEST.Shared"

# 5. Enviar para GitHub
git push
```

---

## 🎯 COMMITS BONS vs RUINS

### ❌ COMMITS RUINS
```bash
git commit -m "fix"
git commit -m "alterações"
git commit -m "asdada"
```

### ✅ COMMITS BONS
```bash
# Adicionar feature
git commit -m "feat: add Face ID authentication to BiometricAuthService"

# Corrigir bug
git commit -m "fix: correct DisableBiometricLoginAsync await in BiometricAuthService"

# Atualizar documentação
git commit -m "docs: add NAVIGEST setup guide"

# Refactoring
git commit -m "refactor: extract DatabaseService to interface for DI"

# Copiar ficheiros
git commit -m "chore: copy DatabaseService and helpers from AppLoginMaui"
```

---

## 📊 COMANDO CHEAT SHEET (Mais Usados)

```bash
# VER STATUS
git status

# VER HISTÓRICO
git log --oneline

# VER DIFERENÇAS (o que mudou)
git diff

# ADICIONAR MUDANÇAS
git add .                    # todos os ficheiros
git add ficheiro.cs          # ficheiro específico

# CRIAR COMMIT
git commit -m "mensagem descritiva"

# ENVIAR PARA GITHUB
git push

# BAIXAR MUDANÇAS DE GITHUB (para trabalhar em vários computadores)
git pull

# DESFAZER MUDANÇAS (cuidado!)
git reset --hard HEAD       # desfaz tudo desde último commit
git checkout ficheiro.cs    # desfaz mudanças num ficheiro
```

---

## 🔄 BRANCHING (Avançado - DEPOIS)

Branches são "caminhos" de desenvolvimento. Exemplo:

```bash
# Criar branch para nova feature
git checkout -b feature/face-id-authentication

# Fazer mudanças e commits nesse branch
# Depois...

# Voltar para main
git checkout main

# Mesclar changes de volta
git merge feature/face-id-authentication

# Enviar para GitHub
git push
```

---

## ⚠️ CUIDADOS IMPORTANTES

### 1. **Guarde seu Personal Access Token numa pasta segura!**
```bash
echo "seu-token-aqui" > ~/.github-token
chmod 600 ~/.github-token
```

### 2. **NUNCA guarde senhas ou secrets no GitHub!**
Use `.gitignore` para:
- `appsettings.local.json`
- `secrets.json`
- Ficheiros `.pfx` (certificados)

### 3. **Antes de começar a programar:**
```bash
# Sempre fazer pull para ter versão mais recente
git pull
```

### 4. **Antes de ir embora:**
```bash
# Sempre fazer push para guardar no GitHub
git push
```

---

## 🎬 RESUMO DO WORKFLOW PARA VOCÊ

**Dia 1** (hoje):
```bash
1. mkdir /Users/joaovarejao/Dev/NAVIGEST
2. cd /Users/joaovarejao/Dev/NAVIGEST
3. git init
4. Criar .gitignore
5. Criar README.md
6. git add .
7. git commit -m "Initial commit: project structure"
8. Criar repositório no GitHub
9. git remote add origin https://github.com/seu-user/NAVIGEST.git
10. git push -u origin main
```

**Dia 2+** (repetir sempre que fizer mudanças):
```bash
1. Editar ficheiros
2. git status
3. git add .
4. git commit -m "descrição clara"
5. git push
```

---

## 🆘 SE ALGO DER ERRADO

### Erro: "fatal: not a git repository"
```bash
# Esqueceu de fazer git init
cd /Users/joaovarejao/Dev/NAVIGEST
git init
```

### Erro: "Permission denied" ao fazer push
```bash
# Problema com Personal Access Token
# Gere novo token no GitHub e tente novamente
```

### Erro: "Your branch and 'origin/main' have diverged"
```bash
# Alguém fez mudanças no GitHub
# Você também fez mudanças localmente
git pull                    # baixa e mescla
# Resolva conflitos se houver
git add .
git commit -m "Merge remote changes"
git push
```

### Esqueceu de fazer push e perdeu trabalho?
```bash
# Se ainda está no computador:
git log                     # encontra seus commits
git push                    # envia para GitHub

# Se foi ao GitHub e quer voltar:
git reset --hard <hash-do-commit>
```

---

## 📞 DÚVIDAS FREQUENTES

**P: Posso trabalhar em vários computadores?**
R: Sim! Em cada computador:
```bash
git clone https://github.com/seu-user/NAVIGEST.git
cd NAVIGEST
# Faça mudanças
git push
```

**P: E se fizer erro num commit?**
R: Pode desfazer:
```bash
git reset --soft HEAD~1     # desfaz commit mas guarda mudanças
git commit -m "mensagem corrigida"
git push --force-with-lease
```

**P: Preciso de Git no Mac?**
R: Sim, vem pré-instalado desde macOS 10.9. Verifique com `git --version`.

**P: Posso apagar o repositório no GitHub?**
R: Sim. GitHub → Settings → Danger Zone → Delete this repository.

---

## ✅ PRÓXIMO PASSO

Quando estiver pronto:
1. Leia este ficheiro completo
2. Siga o resumo do workflow
3. Crie a pasta NAVIGEST e primeiro commit
4. Diga-me quando tiver feito para continuarmos

