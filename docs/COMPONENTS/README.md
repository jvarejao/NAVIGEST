# NAVIGEST - Componentes Documentados

## 📚 Índice de Componentes Cross-Platform

Este ficheiro lista todos os componentes reutilizáveis documentados e o seu estado de portação.

---

## 🎯 Como Usar Este Índice

1. **Antes de criar novo componente:** Verificar template em `TEMPLATE_CROSS_PLATFORM.md`
2. **Ao portar para nova plataforma:** Usar checklist "Portação" em cada guia
3. **Ao actualizar componente:** Atualizar versão e modificação em header

---

## 📋 Componentes Estáveis (Prontos para Portação)

### HorasColaboradorPage ✅
- **Ficheiro:** `HORASCOLABORADOR_PAGE_SETUP.md`
- **Descrição:** Página que exibe/filtra horas de trabalho de colaboradores
- **Android:** ✅ Estável (v1.0.30)
- **iOS:** ⏳ Pronto para portar
- **macOS:** ⏳ Pronto para portar
- **Windows:** ⏳ Pronto para portar
- **Versão doc:** 1.0
- **Data criação:** 2024
- **Modificação:** Última

**Dados principais:**
- Models: `HoraColaborador`, `Colaborador`
- ViewModel: `HorasColaboradorViewModel`
- Converters: `StringNullOrEmptyToBoolConverter`
- BD: Tabelas `HORASTRABALHADAS`, `COLABORADORESTRAB`

**Complexidade:** Média (filtros, CollectionView, SwipeView)

**Dependências:**
- CommunityToolkit.Mvvm
- CommunityToolkit.Maui
- MySqlConnector

---

## 🔄 Em Desenvolvimento

| Componente | Plataforma | Status | Próxima Ação |
|-----------|-----------|--------|-------------|
| - | - | - | - |

---

## 📝 Template para Novo Componente

```markdown
### [NOME_COMPONENTE] ✅ (ou ⏳ ou ❌)
- **Ficheiro:** `[NOME_COMPONENTE]_SETUP.md`
- **Descrição:** [Breve descrição do que faz]
- **Android:** ✅ / ⏳ / ❌
- **iOS:** ✅ / ⏳ / ❌
- **macOS:** ✅ / ⏳ / ❌
- **Windows:** ✅ / ⏳ / ❌
- **Versão doc:** 1.0
- **Data criação:** [data]
- **Modificação:** [data]

**Dados principais:**
- Models: [lista]
- ViewModel: [lista]
- Converters: [lista]
- BD: Tabelas [lista]

**Complexidade:** Baixa / Média / Alta

**Dependências:**
- [lista]
```

---

## 🚀 Próximos Componentes a Documentar

Prioridade:
1. ClientesPage (gestão de clientes)
2. ProdutosPage (gestão de produtos)
3. SettingsPage (configurações da app)

---

## 📖 Documentação de Suporte

- 📘 **Template:** `TEMPLATE_CROSS_PLATFORM.md` - Guia passo-a-passo
- 🔗 **Android Specifics:** `../PLATFORMS/ANDROID_SPECIFICS.md`
- 🍎 **iOS Specifics:** `../PLATFORMS/iOS_SPECIFICS.md`
- 🍎 **macOS Specifics:** `../PLATFORMS/macOS_SPECIFICS.md`
- 🪟 **Windows Specifics:** `../PLATFORMS/WINDOWS_SPECIFICS.md`
- 🌐 **Cross-Platform Guide:** `../PLATFORMS/CROSS_PLATFORM_GUIDE.md`

---

## 📊 Estatísticas

- **Total Componentes:** 1
- **Estáveis:** 1 ✅
- **Em Desenvolvimento:** 0
- **Planeados:** 3

**Cobertura de Plataformas:**
- Android: 100% (1/1)
- iOS: 0% (0/1)
- macOS: 0% (0/1)
- Windows: 0% (0/1)

---

## ✍️ Histórico de Atualizações

| Data | Alteração |
|------|-----------|
| 2024-atual | Criado README e template |
| 2024-anterior | Documentado HorasColaboradorPage |

