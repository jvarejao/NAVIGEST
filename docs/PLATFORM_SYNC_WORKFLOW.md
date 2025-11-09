# PLATFORM SYNC WORKFLOW

**Objetivo**: Documentar o fluxo exato para sincronizar mudanças entre plataformas.

---

## 📋 Workflow Padrão

### Cenário: Fazer mudança em Android, depois sincronizar em iOS/macOS

---

## **PASSO 1: Implementa e Testa em Android**

```
┌─ Android Development ──────────────────────────────────┐
│                                                        │
│ 1. Abre NAVIGEST.Android/Pages/ClientsPage.xaml.cs  │
│ 2. Faz a mudança (ex: adiciona confirmação ao delete) │
│ 3. Testa em smartphone/emulador Android              │
│ 4. Verifica: ✅ Funciona perfeitamente                │
│                                                        │
└────────────────────────────────────────────────────────┘
```

---

## **PASSO 2: Documenta em ANDROID_CHANGES.md**

**Local**: `/docs/PLATFORM_CHANGES/ANDROID_CHANGES.md`

**Formato** (ver template abaixo):

```markdown
### ClientsPage.xaml.cs - OnDeleteSwipeInvoked (2025-11-09)

**Tipo**: Modificação de método existente

**Arquivo**: Pages/ClientsPage.xaml.cs (Lines 110-135)

**Propósito**: Adicionar confirmação antes de deletar cliente

**Antes**:
[código anterior]

**Depois**:
[código novo]

**Dependencies**: ShowConfirmAsync helper (lines 30-50)

**Notas**:
- Handler é async void (necessário para await)
- Não fazer await do DeleteCommand (fire-and-forget)

**Teste Manual**:
1. Swipe em cliente
2. Deve aparecer confirmação
3. Testar "Cancelar" e "Eliminar"

**Status**: ✅ Implementado e testado em Android

**Aplicável em**: iOS ✅, macOS ⏳, Windows ⏳
```

---

## **PASSO 3: Copia Código de Referência**

**Local**: `/src/NAVIGEST.Shared/SYNC_REFERENCE/Pages/`

**Ações**:
1. Cria ou atualiza `ClientsPage.xaml.cs`
2. Copia o código relevante de Android
3. Adiciona comentários explicativos
4. Header obrigatório:

```csharp
// SYNC REFERENCE - ClientsPage.xaml.cs
// ANDROID REFERENCE - Last update: 2025-11-09
// 
// This file contains reference code from Android implementation.
// Copy-paste for reference when implementing in other platforms.
// See: /docs/PLATFORM_CHANGES/ANDROID_CHANGES.md
// 
// DO NOT USE DIRECTLY - FOR REFERENCE ONLY
```

---

## **PASSO 4: Prepara iOS (Quando necessário)**

```
┌─ iOS Preparation ──────────────────────────────────────┐
│                                                        │
│ 1. Abre /docs/PLATFORM_CHANGES/ANDROID_CHANGES.md   │
│    → Lê o que foi mudado                              │
│    → Entende o propósito                              │
│                                                        │
│ 2. Consulta SYNC_REFERENCE/Pages/ClientsPage.xaml.cs │
│    → Vê o código de referência                         │
│    → Lê os comentários                                 │
│                                                        │
│ 3. Verifica /docs/PLATFORM_CHANGES/iOS_CHANGES.md   │
│    → Vê se já foi implementado em iOS                 │
│    → Vê diferenças conhecidas                         │
│                                                        │
│ 4. Abre NAVIGEST.iOS/Pages/ClientsPage.xaml.cs      │
│    → Verifica estado atual                            │
│    → Identifica onde fazer mudança                    │
│                                                        │
└────────────────────────────────────────────────────────┘
```

---

## **PASSO 5: Implementa em iOS (Se necessário)**

```
┌─ iOS Implementation ──────────────────────────────────┐
│                                                       │
│ 1. Adapta o padrão do Android para iOS              │
│    ❌ NÃO copia literalmente                         │
│    ✅ Copia a lógica e padrão                        │
│                                                       │
│ 2. Considera diferenças iOS:                         │
│    - APIs disponíveis em iOS                         │
│    - UI patterns típicos iOS                         │
│    - Performance considerations                      │
│                                                       │
│ 3. Testa em iPhone/simulador                        │
│    ✅ Funciona?                                      │
│    ✅ Parece bem?                                    │
│    ✅ Sem crashes?                                   │
│                                                       │
└───────────────────────────────────────────────────────┘
```

---

## **PASSO 6: Documenta em iOS_CHANGES.md**

**Local**: `/docs/PLATFORM_CHANGES/iOS_CHANGES.md`

**Formato** (similar a Android):

```markdown
### ClientsPage.xaml.cs - OnDeleteSwipeInvoked (2025-11-09)

**Tipo**: Modificação de método existente

**Arquivo**: Pages/ClientsPage.xaml.cs (Lines X-Y)

**Propósito**: Sincronizar com Android - adicionar confirmação

**Referência**: Android (ANDROID_CHANGES.md - OnDeleteSwipeInvoked)

**Diferenças iOS vs Android**:
- [Listar diferenças encontradas]
- [Se nenhuma: Padrão idêntico]

**Implementação**:
[Código iOS]

**Teste Manual**:
[Steps específicos para iOS]

**Status**: ✅ Implementado e testado em iOS

**Notas**:
[Qualquer coisa relevante para iOS]
```

---

## **PASSO 7: Atualiza Status na Tabela**

**Local**: `/docs/PLATFORM_SYNC_GUIDE.md` > "Status de Sincronização"

**Antes**:
```markdown
| Funcionalidade | Android | iOS | macOS | Windows | Notas |
|---|---|---|---|---|---|
| Swipe Delete com Confirmação | ✅ | ? | ⏳ | ⏳ | ... |
```

**Depois**:
```markdown
| Funcionalidade | Android | iOS | macOS | Windows | Notas |
|---|---|---|---|---|---|
| Swipe Delete com Confirmação | ✅ | ✅ | ⏳ | ⏳ | Ref: ANDROID_CHANGES.md |
```

---

## **PASSO 8: macOS (Mesmos Passos)**

Repete PASSO 4-7 para macOS:
1. Lê ANDROID_CHANGES.md
2. Consulta SYNC_REFERENCE
3. Verifica macOS_CHANGES.md
4. Implementa em macOS
5. Testa em Mac
6. Documenta em macOS_CHANGES.md
7. Atualiza tabela de Status

---

## **PASSO 9: Windows (Visual Studio - Manual)**

Para Windows:
1. Lê toda a documentação de Android/iOS/macOS
2. Abre projeto em Visual Studio
3. Adapta considerando APIs Windows (.NET MAUI para Windows)
4. Testa em Windows
5. Cria `/docs/PLATFORM_CHANGES/WINDOWS_CHANGES.md` (se necessário)

---

## 📊 Checklist Completo

**Quando fazes mudança em qualquer plataforma**:

- [ ] **Implementação**
  - [ ] Código implementado na plataforma origem
  - [ ] Código testado e funciona
  - [ ] Sem regressions em outras features

- [ ] **Documentação**
  - [ ] Mudança documentada em `PLATFORM_CHANGES/[PLATFORM]_CHANGES.md`
  - [ ] Usa template padrão (Tipo, Arquivo, Propósito, Antes/Depois, Dependencies, Status)
  - [ ] Inclui instruções de teste manual
  - [ ] Identifica plataformas aplicáveis (iOS?, macOS?, Windows?)

- [ ] **Referência**
  - [ ] Código de referência copiado para `SYNC_REFERENCE`
  - [ ] Header incluído no ficheiro de referência
  - [ ] Comentários explicativos adicionados

- [ ] **Status**
  - [ ] Tabela de Status em `PLATFORM_SYNC_GUIDE.md` atualizada
  - [ ] Status marcado como ✅ (implementado), 🟡 (não testado), ⏳ (pendente), ou ❌ (não aplicável)

- [ ] **Commit**
  - [ ] Commit feito com mensagem descritiva
  - [ ] Referência ao documento (ex: "Ref: PLATFORM_CHANGES/ANDROID_CHANGES.md")
  - [ ] Exemplo: `"Add delete confirmation to ClientsPage (Ref: ANDROID_CHANGES.md)"`

---

## 🎯 Quick Reference

### Ficheiros-chave

| Ficheiro | Propósito |
|---|---|
| `/docs/PLATFORM_SYNC_GUIDE.md` | Guia central, tabela de status |
| `/docs/PLATFORM_CHANGES/ANDROID_CHANGES.md` | Log de mudanças Android |
| `/docs/PLATFORM_CHANGES/iOS_CHANGES.md` | Log de mudanças iOS |
| `/docs/PLATFORM_CHANGES/macOS_CHANGES.md` | Log de mudanças macOS |
| `/src/NAVIGEST.Shared/SYNC_REFERENCE/` | Código de referência (consulta) |

### Quando pedir sincronização

**User**: "Sincroniza delete confirmation com iOS"

**Agent**:
1. Abre `PLATFORM_CHANGES/ANDROID_CHANGES.md` → lê o quê foi feito
2. Consulta `SYNC_REFERENCE/Pages/ClientsPage.xaml.cs` → vê código de referência
3. Abre `NAVIGEST.iOS/Pages/ClientsPage.xaml.cs` → verifica se precisa fazer algo
4. Implementa em iOS (se necessário)
5. Testa em iOS
6. Documenta em `PLATFORM_CHANGES/iOS_CHANGES.md`
7. Atualiza status em `PLATFORM_SYNC_GUIDE.md`
8. Faz commit

---

## 🚀 Vantagens desta Abordagem

✅ **Rastreabilidade**: Sabe-se exatamente o quê foi mudado e onde  
✅ **Visibilidade**: Fácil ver status de cada plataforma  
✅ **Referência**: Código de exemplo disponível sem poluir Shared  
✅ **Sincronização**: Estrutura clara para adaptar entre plataformas  
✅ **Documentação**: Futuras mudanças fáceis de rastrear  
✅ **Autonomia**: Outro dev consegue continuar sem perguntar  

---

**Última Atualização**: 2025-11-09
