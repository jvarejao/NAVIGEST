# NAVIGEST - Multi-Platform Authentication & Biometric System

## 📱 Descrição

NAVIGEST é um sistema de autenticação multi-plataforma baseado em .NET MAUI 9.0 com suporte a:
- **iOS**: Face ID (biometria facial)
- **Android**: Fingerprint (impressão digital)
- **Windows**: Autenticação tradicional (opcional)

## 🏗️ Estrutura do Projeto

```
NAVIGEST/
├── src/                          # Código fonte
│   ├── NAVIGEST.Shared/         # Class library (80% do AppLoginMaui)
│   │   ├── Services/            # Serviços reutilizáveis
│   │   ├── ViewModels/          # ViewModels
│   │   ├── Models/              # Modelos de dados
│   │   └── Resources/           # Recursos compartilhados
│   ├── NAVIGEST.iOS/            # App iOS (em desenvolvimento)
│   ├── NAVIGEST.Android/        # App Android (em desenvolvimento)
│   └── NAVIGEST.Windows/        # App Windows (opcional)
├── docs/                         # Documentação
│   ├── NAVIGEST_QUICK_SETUP.md  # Setup rápido
│   ├── NAVIGEST_CODE_FIXES.md   # Bugs identificados
│   ├── NAVIGEST_ACTION_PLAN.md  # Plano de ação
│   └── ...outros documentos
├── NAVIGEST.sln                  # Solução
└── README.md                      # Este ficheiro
```

## 📋 Estado Atual

### ✅ Completo
- [x] Análise do AppLoginMaui (80% reutilizável)
- [x] Documentação completa do projeto
- [x] Identificação de 3 bugs a corrigir
- [x] Plano de ação detalhado

### 🔄 Em Progresso
- [ ] Criar NAVIGEST.Shared class library
- [ ] Copiar ficheiros do AppLoginMaui
- [ ] Aplicar correções de bugs
- [ ] Estrutura base do projeto

### ⏳ Próximo
- [ ] Criar UI iOS com Face ID
- [ ] Criar UI Android com Fingerprint
- [ ] Integração com base de dados
- [ ] Testes

## 🛠️ Requisitos

- .NET 9.0 SDK
- Visual Studio Code ou Visual Studio
- macOS (para desenvolvimento iOS)
- Android SDK (para desenvolvimento Android)

## 🚀 Como Começar

### 1. Estrutura Básica
```bash
cd /Users/joaovarejao/Dev/NAVIGEST
```

### 2. Criar NAVIGEST.Shared
```bash
# Isto será feito automaticamente
dotnet new classlib -n NAVIGEST.Shared -f net9.0
```

### 3. Criar Solução
```bash
# Isto será feito automaticamente
dotnet new sln -n NAVIGEST
```

### 4. Copiar Ficheiros
Os ficheiros serão copiados do AppLoginMaui para NAVIGEST.Shared

## 📚 Documentação

Consulte a pasta `docs/` para documentação detalhada:
- `NAVIGEST_QUICK_SETUP.md` - Setup passo a passo
- `NAVIGEST_CODE_FIXES.md` - Bugs e como corrigi-los
- `NAVIGEST_ANALYSIS_SUMMARY.md` - Análise técnica
- `NAVIGEST_ACTION_PLAN.md` - Plano de desenvolvimento
- `NAVIGEST_MAPA.md` - Mapa visual do projeto

## 🐛 Bugs Conhecidos

Existem 3 bugs identificados no AppLoginMaui que serão corrigidos em NAVIGEST.Shared:
1. **BiometricAuthService** - Falha ao chamar BiometricService.IsAvailableAsync()
2. **LoginViewModel** - Lógica de autenticação incompleta
3. **DatabaseService** - Conexão à base de dados

Veja `docs/NAVIGEST_CODE_FIXES.md` para detalhes.

## 📝 Próximos Passos

1. ✅ Pasta NAVIGEST criada
2. 🔄 Estrutura de pastas criada
3. 🔄 NAVIGEST.Shared será criada
4. 🔄 Ficheiros serão copiados
5. 🔄 Bugs serão corrigidos

## 👤 Autor

João Varejão

## 📅 Data de Início

20 de outubro de 2025

---

**Status**: 🟡 Estrutura criada, desenvolvimento em progresso
