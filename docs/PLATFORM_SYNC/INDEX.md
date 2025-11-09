# 🗂️ PLATFORM SYNC - INDEX

**Documentação centralizada para sincronização multi-plataforma Android/iOS/macOS/Windows**

---

## 📚 Documentação Principal

| Ficheiro | Propósito | Quando Consultar |
|---|---|---|
| **[PLATFORM_SYNC_GUIDE.md](PLATFORM_SYNC_GUIDE.md)** | 📋 Guia central, tabela de status, rules of thumb | Qualquer questão sobre sincronização |
| **[PLATFORM_SYNC_WORKFLOW.md](PLATFORM_SYNC_WORKFLOW.md)** | 🔄 Workflow passo-a-passo para sincronizar | Quando vai fazer mudança em plataforma |

---

## 📝 Logs de Mudanças (por Plataforma)

### Android
**Ficheiro**: [PLATFORM_CHANGES/ANDROID_CHANGES.md](PLATFORM_CHANGES/ANDROID_CHANGES.md)

**Última mudança**: 2025-11-09 - Delete Confirmation Pattern  
**Status**: ✅ Pronto para sincronizar

**Mudanças Documentadas**:
- ✅ ShowConfirmAsync helper (NEW)
- ✅ GetRootPage helper (NEW)
- ✅ OnDeleteSwipeInvoked (MODIFIED - adicionada confirmação)
- ✅ OnDeleteFromFormTapped (MODIFIED - adicionada confirmação)

---

### iOS
**Ficheiro**: [PLATFORM_CHANGES/iOS_CHANGES.md](PLATFORM_CHANGES/iOS_CHANGES.md)

**Status**: ✅ Verificado - JÁ TEM implementação similar  
**Última verificação**: 2025-11-09 (sem alterações necessárias)

---

### macOS
**Ficheiro**: [PLATFORM_CHANGES/macOS_CHANGES.md](PLATFORM_CHANGES/macOS_CHANGES.md)

**Status**: ⏳ Aguardando sincronização  
**Próxima ação**: Verificar se precisa mesmo padrão

---

## 📦 Código de Referência

**Local**: `/src/NAVIGEST.Shared/SYNC_REFERENCE/`

| Ficheiro | Plataforma | Atualizado |
|---|---|---|
| Pages/ClientsPage.xaml.cs | Android | 2025-11-09 |

**Como Usar**: Ver [SYNC_REFERENCE/README.md](/src/NAVIGEST.Shared/SYNC_REFERENCE/README.md)

⚠️ **IMPORTANTE**: Ficheiros em SYNC_REFERENCE são APENAS CONSULTA - não use diretamente em código!

---

## 📊 Status de Sincronização

```
┌────────────────────────────────────────────────────────────────┐
│ FUNCIONALIDADE: Delete with Confirmation (ClientsPage)        │
├────────────────────────────────────────────────────────────────┤
│ Android:  ✅ Implementado, testado                             │
│ iOS:      ✅ Verificado - já tinha implementação              │
│ macOS:    ⏳ Aguardando sincronização                          │
│ Windows:  ⏳ Aguardando (fazer em Visual Studio)              │
└────────────────────────────────────────────────────────────────┘
```

---

## 🔍 Como Navegar

### "Quero saber o que foi mudado em Android"
→ Abre [PLATFORM_CHANGES/ANDROID_CHANGES.md](PLATFORM_CHANGES/ANDROID_CHANGES.md)

### "Quero implementar em iOS o que foi feito em Android"
→ Segue [PLATFORM_SYNC_WORKFLOW.md](PLATFORM_SYNC_WORKFLOW.md) - PASSO 4-7

### "Quero ver código de referência"
→ Abre [SYNC_REFERENCE/Pages/ClientsPage.xaml.cs](/src/NAVIGEST.Shared/SYNC_REFERENCE/Pages/ClientsPage.xaml.cs)

### "Quero saber qual é o status de tudo"
→ Abre [PLATFORM_SYNC_GUIDE.md](PLATFORM_SYNC_GUIDE.md) - tabela "Status de Sincronização"

### "Preciso fazer nova mudança"
→ Segue [PLATFORM_SYNC_WORKFLOW.md](PLATFORM_SYNC_WORKFLOW.md) - Workflow Padrão

---

## ✅ Checklist para Próxima Mudança

Quando quiseres fazer mudança:

1. [ ] Implementa e testa em Android (ou plataforma de origem)
2. [ ] Abre [PLATFORM_SYNC_WORKFLOW.md](PLATFORM_SYNC_WORKFLOW.md)
3. [ ] Segue PASSO 1-9 (ou os aplicáveis)
4. [ ] Documenta em `PLATFORM_CHANGES/[PLATFORM]_CHANGES.md`
5. [ ] Copia código para `SYNC_REFERENCE`
6. [ ] Atualiza status em `PLATFORM_SYNC_GUIDE.md`
7. [ ] Faz commit com referência

---

## 🚀 Quick Links

- **Android Mudanças**: [PLATFORM_CHANGES/ANDROID_CHANGES.md](PLATFORM_CHANGES/ANDROID_CHANGES.md)
- **iOS Mudanças**: [PLATFORM_CHANGES/iOS_CHANGES.md](PLATFORM_CHANGES/iOS_CHANGES.md)
- **macOS Mudanças**: [PLATFORM_CHANGES/macOS_CHANGES.md](PLATFORM_CHANGES/macOS_CHANGES.md)
- **Código de Referência**: [SYNC_REFERENCE/Pages/ClientsPage.xaml.cs](/src/NAVIGEST.Shared/SYNC_REFERENCE/Pages/ClientsPage.xaml.cs)
- **Guia Central**: [PLATFORM_SYNC_GUIDE.md](PLATFORM_SYNC_GUIDE.md)
- **Workflow**: [PLATFORM_SYNC_WORKFLOW.md](PLATFORM_SYNC_WORKFLOW.md)

---

## 📞 Próximas Ações

- [ ] Sincronizar com iOS (se necessário)
- [ ] Sincronizar com macOS
- [ ] Implementar em Windows (Visual Studio)
- [ ] Adicionar confirmação a OnPastasSwipeInvoked (mesmo padrão)

---

**Última Atualização**: 2025-11-09  
**Sistema Ativo**: ✅ Pronto para sincronização
