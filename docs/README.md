# NAVIGEST - Documentação Central

## 🎯 Bem-vindo à Documentação do NAVIGEST

Este é o ponto central para toda a documentação do projeto NAVIGEST - uma app de gestão de horas de trabalho e produtos, desenvolvida em .NET MAUI com suporte multi-plataforma (Android, iOS, macOS, Windows).

---

## 🚀 Começar Aqui

Se é novo no projeto:

1. **Primeiras Passos:** Leia `GUIA_INICIO.md` (a criar na consolidação)
2. **Quick Setup:** `NAVIGEST_QUICK_SETUP.md`
3. **Mapa do Projeto:** `NAVIGEST_MAPA.md` - Visão visual da arquitetura

---

## 📚 Estrutura da Documentação

### 🔵 [COMPONENTS/](./COMPONENTS/) - Documentação de Features

Guias completos para implementar e portar cada componente entre plataformas.

- **Template:** `TEMPLATE_CROSS_PLATFORM.md` - Padrão para documentar novo componente
- **Índice:** `README.md` - Lista de todos componentes
- **Exemplo:** `HORASCOLABORADOR_PAGE_SETUP.md` - HorasColaboradorPage (Android ✅, iOS ⏳)

**Use esta pasta quando:**
- Implementar novo componente/página
- Portar componente para nova plataforma
- Procurar padrão de implementação

---

### 🌐 [PLATFORMS/](./PLATFORMS/) - Platform-Specific

Guias específicos de cada plataforma e padrões cross-platform.

- `ANDROID_SPECIFICS.md` - Considerações Android
- `iOS_SPECIFICS.md` - Considerações iOS
- `iOS_PROVISIONING.md` - Setup provisioning Apple
- `macOS_SPECIFICS.md` - Considerações macOS
- `WINDOWS_SPECIFICS.md` - Considerações Windows
- `CROSS_PLATFORM_GUIDE.md` - Padrões reutilizáveis

**Use esta pasta quando:**
- Trabalhar com platform-specifics
- Entender limitações de plataforma
- Adaptar UI para diferentes resoluções/formatos

---

### 📖 [GUIDES/](./GUIDES/) - Guias Temáticos

Guias práticos para tarefas comuns (deployment, debugging, etc).

- `GITHUB_WORKFLOW.md` - Como usar GitHub (workflow básico)
- `GITHUB_RELEASES.md` - Como fazer releases (processo completo)
- `GITHUB_CHECKLIST.md` - Checklist setup inicial GitHub
- `UPDATE_SERVICE.md` - Sistema de auto-update da app
- `TROUBLESHOOTING.md` - Problemas comuns e soluções
- `PATTERNS.md` - Padrões de implementação (SwipeView, etc)

**Use esta pasta quando:**
- Precisar fazer release (GITHUB_RELEASES.md)
- Debug de problemas (TROUBLESHOOTING.md)
- Implementar padrão comum (PATTERNS.md)

---

### 🔄 [PLATFORM_SYNC/](./PLATFORM_SYNC/) - Sincronização Entre Plataformas

Arquitetura e processo de sincronização de código entre Android/iOS/macOS/Windows.

- `ARCHITECTURE.md` - Como funciona a sincronização
- `WORKFLOW.md` - Workflow prático de sincronização
- `GUIDE.md` - Guia passo-a-passo

**Use esta pasta quando:**
- Entender como código é sincronizado
- Trabalhar em mudanças que afetam todas plataformas

---

### 📦 [PLATFORM_CHANGES/](./PLATFORM_CHANGES/) - Histórico de Mudanças

Histórico de mudanças feitas em cada plataforma.

- `ANDROID_CHANGES.md` - Mudanças Android
- `iOS_CHANGES.md` - Mudanças iOS

**Referência histórica** (não é ativo development).

---

### 🗄️ [_ARCHIVE/](./ARCHIVE/) - Histórico & Análises

Documentação histórica, análises de trabalho passado, action plans, etc.

Útil para referência, **não é documentation ativa**.

---

## 🔍 Índice Rápido por Tarefa

### Vou desenvolver um novo componente
1. Ler `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md`
2. Usar template como guia
3. Documentar seguindo secções obrigatórias
4. Exemplo: `COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md`

### Vou portar componente para iOS
1. Ler `COMPONENTS/[COMPONENTE]_SETUP.md`
2. Seguir checklist "Portação" ao fim
3. Consultar `PLATFORMS/iOS_SPECIFICS.md` se problemas UI
4. Testar em simulator

### Vou fazer release (v1.0.X)
1. Ler `GUIDES/GITHUB_RELEASES.md`
2. Seguir processo passo-a-passo
3. Usar `GUIDES/GITHUB_CHECKLIST.md` se primeira vez

### App está a crashar
1. Ler `GUIDES/TROUBLESHOOTING.md`
2. Procurar erro específico
3. Se não encontrar, consultar `COMPONENTS/` relevante

### Não sei por onde começar
1. Ler `NAVIGEST_QUICK_SETUP.md`
2. Depois `NAVIGEST_MAPA.md`
3. Depois escolher tarefa específica e seguir guia acima

---

## 📊 Índice de Componentes

| Componente | Status Android | iOS | macOS | Windows | Documentação |
|-----------|---------|-------|-------|---------|-------------|
| **HorasColaboradorPage** | ✅ Estável | ⏳ Pronto | ⏳ Pronto | ⏳ Pronto | [SETUP](./COMPONENTS/HORASCOLABORADOR_PAGE_SETUP.md) |
| ClientesPage | ⏳ | ⏳ | ⏳ | ⏳ | - |
| ProdutosPage | ⏳ | ⏳ | ⏳ | ⏳ | - |

[Ver índice completo em COMPONENTS/README.md](./COMPONENTS/README.md)

---

## 🛠️ Ferramentas & Tecnologia

**Stack:**
- .NET MAUI 9.0
- C# 12
- MySqlConnector (banco de dados)
- CommunityToolkit.Mvvm (padrão MVVM)
- CommunityToolkit.Maui (UI components)
- GitHub API (automation)

**Plataformas:**
- Android 21+
- iOS 14+
- macOS 11+
- Windows 10+

---

## 🔗 Links Úteis

- 📁 Código-fonte: `src/`
- 🔧 Release scripts: `scripts/`
- 📦 Release history: `releases/`
- ⚙️ Configuração: `NAVIGEST.sln`

---

## 📝 Processo de Documentação

Seguir este processo para qualquer novo componente/feature:

1. **Implementar em Android** (plataforma de referência)
2. **Testar** e validar funcionamento
3. **Documentar** usando `COMPONENTS/TEMPLATE_CROSS_PLATFORM.md`
4. **Commit** e push ao repositório
5. **Portar** para iOS/macOS/Windows (usando doc como referência)

---

## 📞 Contacto & Suporte

Para dúvidas sobre documentação:
- Consultar `GUIDES/TROUBLESHOOTING.md`
- Verificar `COMPONENTS/` relevante
- Se não encontrar, criar novo issue com tag `documentation`

---

## 📈 Versão Atual

- **App Version:** v1.0.30
- **Doc Version:** 1.0 (reorganizada)
- **Última Atualização:** 2024

---

## ✅ Checklist para Novo Dev

- [ ] Li `NAVIGEST_QUICK_SETUP.md`
- [ ] Fiz setup local
- [ ] Li `NAVIGEST_MAPA.md`
- [ ] Entendi estrutura de pastas
- [ ] Identifiquei componente a trabalhar
- [ ] Li documentação do componente (COMPONENTS/)
- [ ] Compilei e testei localmente

Quando tudo ✅, está pronto para começar!

---

**Generated:** 2024 | **Structure:** Cross-Platform Documentation Framework

