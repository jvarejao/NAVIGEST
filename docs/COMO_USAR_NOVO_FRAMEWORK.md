# NOVO MÉTODO DE DOCUMENTAÇÃO - GUIA RÁPIDO

## 📋 Resumo do Novo Framework

A partir de agora, toda a documentação do NAVIGEST segue este padrão:

```
docs/
├── README.md                              # Entrada principal
├── COMPONENTS/                            # ⭐ Componentes reutilizáveis
│   ├── TEMPLATE_CROSS_PLATFORM.md        # Template para novo componente
│   ├── HORASCOLABORADOR_PAGE_SETUP.md    # Exemplo completo
│   └── README.md                          # Índice de componentes
├── PLATFORMS/                             # ⭐ Platform-specific
│   ├── ANDROID_SPECIFICS.md
│   ├── iOS_SPECIFICS.md
│   ├── macOS_SPECIFICS.md
│   ├── WINDOWS_SPECIFICS.md
│   └── README.md
└── CONSOLIDACAO_DOCUMENTACAO.md           # Análise dos 36 docs existentes
```

---

## 🎯 Como Usar - Cenários Práticos

### Cenário 1: Vou implementar novo componente (ex: ClientesPage)

1. **Implementar em Android** (plataforma de referência)
2. **Abrir template:** `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md`
3. **Criar novo ficheiro:** `COMPONENTS/CLIENTES_PAGE_SETUP.md`
4. **Preencher todas as secções:** Models, ViewModel, UI, DB, etc
5. **Seguir exemplo:** `COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md`
6. **Adicionar a README:** `COMPONENTS/README.md`
7. **Commit:** `git commit -m "Docs: Adiciona ClientesPage documentation"`

**Tempo estimado:** 1-2 horas

---

### Cenário 2: Vou portar componente para iOS

1. **Ler doc do componente:** `COMPONENTS/[COMPONENTE]_SETUP.md`
2. **Consultar iOS specifics:** `PLATFORMS/iOS_SPECIFICS.md`
3. **Seguir checklist "Portação"** ao fim do doc do componente:
   - [ ] Models - Copiar direto
   - [ ] ViewModel - Copiar direto
   - [ ] XAML - Copiar, adaptar UI (safe area, etc)
   - [ ] Code-behind - Copiar, alterar namespace
   - [ ] Testes - Testar em simulator
4. **Atualizar doc:** Mudar `iOS: ⏳` para `iOS: ✅` em COMPONENTS/README.md
5. **Commit:** `git commit -m "feat(iOS): Port ClientesPage to iOS"`

**Tempo estimado:** 30-60 minutos por componente

---

### Cenário 3: App está a crashar, procuro solução

1. **Ler troubleshooting (quando criado):** `GUIDES/TROUBLESHOOTING.md`
2. **Procurar erro específico**
3. **Se não encontrar**, consultar:
   - `COMPONENTS/[COMPONENTE_RELEVANTE]_SETUP.md` → Secção "Problems Resolved"
   - `PLATFORMS/[PLATAFORMA]_SPECIFICS.md` → Secção "Known Issues"

---

### Cenário 4: Não sei por onde começar no projeto

1. **Ler:** `docs/README.md` (nova entrada central)
2. **Depois:** `NAVIGEST_QUICK_SETUP.md`
3. **Depois:** `NAVIGEST_MAPA.md`
4. **Depois:** Escolher tarefa e seguir guias específicos

---

## 📚 Estrutura de Documentação

### ✅ COMPONENTS/ (Novo Padrão)

**Propósito:** Documentação de features implementadas.

**Conteúdo:**
- `TEMPLATE_CROSS_PLATFORM.md` - Guia passo-a-passo para documentar
- `[COMPONENTE]_SETUP.md` - Um ficheiro por feature/componente
- `README.md` - Índice e status de portação

**Quando usar:**
- Implementar novo componente
- Portar para nova plataforma
- Procurar padrão de implementação

---

### ✅ PLATFORMS/ (Novo Padrão)

**Propósito:** Características técnicas de cada plataforma.

**Conteúdo:**
- `ANDROID_SPECIFICS.md` - Android considerations
- `iOS_SPECIFICS.md` - iOS considerations
- `macOS_SPECIFICS.md` - macOS considerations
- `WINDOWS_SPECIFICS.md` - Windows considerations
- `README.md` - Overview e comparação

**Quando usar:**
- Portar componente (adaptar UI)
- Entender limitações de plataforma
- Debug de platform-specific issues

---

### ⏳ GUIDES/ (A Criar - Consolidação Futura)

**Propósito:** Guias temáticos (deployment, debugging, etc).

**Planeado:**
- `GITHUB_WORKFLOW.md` - Como usar GitHub
- `GITHUB_RELEASES.md` - Como fazer release
- `UPDATE_SERVICE.md` - Sistema de auto-update
- `TROUBLESHOOTING.md` - Problemas comuns

---

### 📦 _ARCHIVE/ (A Criar - Referência Histórica)

**Propósito:** Análises e documentação histórica.

**Planeado:**
- Mover 7 ficheiros de análise passada
- Manter acessível, mas "hidden"

---

## 🔄 Exemplo Prático: Documentação Completa

### Estrutura do ficheiro `[COMPONENTE]_SETUP.md`

```markdown
# [NOME_COMPONENTE] - Cross-Platform Setup Guide

**Versão:** 1.0  
**Plataformas:** Android ✅ | iOS ⏳ | macOS ⏳ | Windows ⏳  

## 1. Visão Geral
[Descrição breve]

## 2. Estrutura de Pastas
[Estrutura IDÊNTICA em todas plataformas]

## 3. Models
[Código C# - IDÊNTICO em todas plataformas]

## 4. ViewModel
[Código C# - IDÊNTICO em todas plataformas]

## 5. UI (XAML)
[XAML + code-behind]

## 6. Converters
[Se houver]

## 7. DatabaseService
[Métodos SQL]

## 8. DI Setup
[MauiProgram.cs]

## 9. Navegação
[MainYahPage route]

## 10. Tabelas BD
[SQL CREATE TABLE]

## 11. Checklist de Portação
- [ ] Models - Copiar, alterar namespace
- [ ] ViewModel - Copiar, alterar namespace
- [ ] XAML - Copiar, adaptar UI
- [ ] Code-behind - Copiar, alterar namespace
- [ ] DatabaseService - Adicionar métodos
- [ ] MauiProgram.cs - Adicionar DI
- [ ] MainYahPage - Adicionar route
- [ ] Testes - Testar em device/simulator
```

---

## ✨ Benefícios do Novo Framework

✅ **Reutilizável:** Models/ViewModel/Converters são 100% idênticos em todas plataformas  
✅ **Modular:** Pode copiar um componente sem perder contexto  
✅ **Expandível:** Template fácil de usar para novo componente  
✅ **Claro:** Novo dev sabe exatamente onde procurar  
✅ **Sem redundância:** Informação existe num único lugar  
✅ **Cross-platform:** Planeado desde o início  

---

## 🚀 Próximos Passos (Recomendados)

### Imediato (Esta semana)
- [ ] Revisar novo framework em `docs/README.md`
- [ ] Adicionar checkout tag v1.0.30 (estável)
- [ ] Começar porting HorasColaboradorPage para iOS

### Curto Prazo (Próximas 2 semanas)
- [ ] Documentar ClientesPage (novo componente)
- [ ] Portar HorasColaboradorPage para iOS (completo)
- [ ] Portar HorasColaboradorPage para macOS (completo)
- [ ] Portar HorasColaboradorPage para Windows (completo)

### Médio Prazo (Próximos 30 dias)
- [ ] Criar GUIDES/ pasta (consolidar GitHub, Update Service, etc)
- [ ] Criar _ARCHIVE/ pasta (mover análises históricas)
- [ ] Documentar ProdutosPage
- [ ] Documentar ClientesPage

---

## 📝 Comandos Úteis

```bash
# Ver estrutura de docs
tree -d -L 2 docs/

# Ver ficheiros em COMPONENTS
ls -lh docs/COMPONENTS/

# Ver ficheiros em PLATFORMS
ls -lh docs/PLATFORMS/

# Ver análise de consolidação
cat docs/CONSOLIDACAO_DOCUMENTACAO.md
```

---

## 🎓 Aprender pelo Exemplo

### Exemplo Completo: HorasColaboradorPage

Localização: `docs/COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md`

Este ficheiro contém exemplo completo de:
- Como estruturar um componente
- Como documentar Models, ViewModel, UI
- Como adicionar Converters
- Como setup DI
- Como fazer Checklist de Portação

**Use como referência ao documentar novo componente.**

---

## 📞 Dúvidas?

- Reler `docs/README.md` (visão geral)
- Consultar `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md` (para novo componente)
- Consultar `PLATFORMS/[PLATAFORMA]_SPECIFICS.md` (para platform issues)
- Ver exemplo: `COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md`

---

## ✅ Checklist: Pronto para Usar?

- [ ] Li `docs/README.md`
- [ ] Entendi estrutura COMPONENTS + PLATFORMS
- [ ] Vi exemplo `HORASCOLABORADOR_PAGE_SETUP.md`
- [ ] Entendi template para novo componente
- [ ] Entendi checklist de portação
- [ ] Pronto para começar

**Se tudo ✅, pode começar a documentar!**

---

**Versão:** 1.0  
**Criado:** 2024  
**Framework:** Cross-Platform Documentation System

