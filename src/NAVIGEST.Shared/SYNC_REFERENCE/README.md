# SYNC_REFERENCE - How to Use

**Objetivo**: Fornecer código de referência para sincronização entre plataformas.

---

## ⚠️ IMPORTANTE

**ESTA PASTA É APENAS PARA CONSULTA**

- ❌ NÃO é para importar/referenciar diretamente no código
- ❌ NÃO é para compilar
- ✅ É para copiar padrões e adaptar em cada plataforma

---

## 📖 Como Usar

### 1. **Quando implementar mudança de Android em iOS/macOS**

```
1. Lê /docs/PLATFORM_CHANGES/ANDROID_CHANGES.md
2. Consulta SYNC_REFERENCE/Pages/ClientsPage.xaml.cs (este ficheiro)
3. COPIA o padrão (não copia literalmente)
4. ADAPTA para iOS/macOS (pode ter diferenças de UI/APIs)
5. TESTA em iOS/macOS
6. DOCUMENTA em /docs/PLATFORM_CHANGES/iOS_CHANGES.md ou macOS_CHANGES.md
```

### 2. **Exemplo Prático**

**Cenário**: Queres implementar Delete com confirmação em iOS

**Passos**:
```
1. Abre /docs/PLATFORM_CHANGES/ANDROID_CHANGES.md
   → Vês que ClientsPage OnDeleteSwipeInvoked foi modificado
   → Vês que ShowConfirmAsync helper foi criado

2. Consulta SYNC_REFERENCE/Pages/ClientsPage.xaml.cs
   → Vês ShowConfirmAsync e GetRootPage helpers
   → Vês OnDeleteSwipeInvoked implementação
   → Vês comentários explicando por que existe

3. Em iOS ClientsPage.xaml.cs:
   → Verifica se já tem ShowConfirmAsync helper (provavelmente tem)
   → Verifica se já tem OnDeleteSwipeInvoked com confirmação (provavelmente tem)
   → Se não tem, copia o padrão de SYNC_REFERENCE e adapta

4. Depois de implementar:
   → Testa em iOS
   → Documenta em /docs/PLATFORM_CHANGES/iOS_CHANGES.md
   → Marca como ✅ Implementado em iOS
```

---

## 📁 Estrutura

```
SYNC_REFERENCE/
├── README.md (este ficheiro)
└── Pages/
    └── ClientsPage.xaml.cs
        └── Código de referência do Android
```

---

## 🏷️ Formato de Ficheiros

Cada ficheiro em SYNC_REFERENCE tem header:

```csharp
// SYNC REFERENCE - [Nome do Ficheiro]
// [PLATFORM] REFERENCE - Last update: [DATA]
// 
// This file contains reference code from [Platform] implementation.
// Copy-paste for reference when implementing in other platforms.
// See: /docs/PLATFORM_CHANGES/[PLATFORM]_CHANGES.md
// 
// DO NOT USE DIRECTLY - FOR REFERENCE ONLY
```

---

## 💡 Notas Importantes

1. **Código pode ter diferenças**
   - Android pode usar APIs que não existem em iOS
   - iOS pode usar UIKit patterns que Android não tem
   - Adapta conforme necessário

2. **Comentários incluem detalhes**
   - Porquê o código é assim
   - Casos edge que foram encontrados
   - Como testar

3. **SYNC_REFERENCE é snapshot**
   - Atualizado quando mudanças são feitas
   - Não é código vivo (não é compilado)
   - É documentação + referência

---

## 🔄 Workflow Completo

```
┌─────────────────────────────────────┐
│ Mudança feita em ANDROID            │
├─────────────────────────────────────┤
│ 1. Implementa e testa em Android    │
│ 2. Documenta em ANDROID_CHANGES.md  │
│ 3. Copia código para SYNC_REFERENCE │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ Quando sinc em iOS                  │
├─────────────────────────────────────┤
│ 1. Lê ANDROID_CHANGES.md            │
│ 2. Consulta SYNC_REFERENCE          │
│ 3. Implementa em iOS/macOS          │
│ 4. Testa                            │
│ 5. Documenta em iOS_CHANGES.md      │
└─────────────────────────────────────┘
```

---

**Última Atualização**: 2025-11-09
