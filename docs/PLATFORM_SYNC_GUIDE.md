# PLATFORM SYNCHRONIZATION GUIDE

**Objetivo**: Facilitar sincronização de mudanças entre plataformas (Android, iOS, macOS) mantendo rastreabilidade clara.

---

## 📋 Estrutura de Documentação

```
docs/
├── PLATFORM_SYNC_GUIDE.md (este ficheiro)
├── PLATFORM_CHANGES/
│   ├── ANDROID_CHANGES.md        ← Mudanças feitas em Android
│   ├── iOS_CHANGES.md            ← Mudanças feitas em iOS
│   └── macOS_CHANGES.md          ← Mudanças feitas em macOS
└── [outras docs]

src/
└── NAVIGEST.Shared/
    └── SYNC_REFERENCE/           ← Código de referência (consulta)
        ├── PageModels/
        ├── Pages/
        └── [estrutura espelho]
```

---

## 🔄 Fluxo de Sincronização

### Quando fazes mudança em Android (exemplo):

1. **Identifica o ficheiro alterado**
   - Ex: `NAVIGEST.Android/Pages/ClientsPage.xaml.cs`

2. **Documenta em `PLATFORM_CHANGES/ANDROID_CHANGES.md`**
   ```markdown
   ### ClientsPage.xaml.cs - OnDeleteSwipeInvoked (2025-11-09)
   
   **Tipo**: Modificação de método existente
   **Arquivo**: Pages/ClientsPage.xaml.cs (Lines 110-135)
   **Propósito**: Adicionar confirmação antes de deletar cliente
   
   **Antes**:
   ```csharp
   private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
   {
       // Apenas executava delete sem confirmação
   }
   ```
   
   **Depois**:
   ```csharp
   private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
   {
       var confirm = await ShowConfirmAsync("Eliminar Cliente", ..., "Eliminar", "Cancelar");
       if (!confirm) return;
       // ... executa delete
   }
   ```
   
   **Dependencies**: ShowConfirmAsync helper (lines 30-50)
   
   **Status**: ✅ Testado em Android
   ```

3. **Copia código de referência para SYNC_REFERENCE**
   - Cria: `src/NAVIGEST.Shared/SYNC_REFERENCE/Pages/ClientsPage.xaml.cs`
   - Marca com comentário: `// ANDROID REFERENCE - Last update: 2025-11-09`
   - Comenta secções relevantes

4. **Quando implementar em iOS/macOS**
   - Abre a doc: `PLATFORM_CHANGES/ANDROID_CHANGES.md`
   - Consulta a referência em `SYNC_REFERENCE`
   - Adapta conforme necessário (UI patterns iOS vs Android)
   - Documenta em `PLATFORM_CHANGES/iOS_CHANGES.md` com status

---

## 📝 Formato Padrão para Mudanças

Sempre que documentares mudança, usa este formato:

```markdown
### [Ficheiro] - [Método/Propriedade] ([DATA])

**Tipo**: 
- Novo método
- Modificação de método existente
- Novo ficheiro
- Remoção
- Refactor

**Arquivo**: [Caminho relativo] (Lines X-Y)

**Propósito**: [O que faz e porquê]

**Antes**:
\`\`\`csharp
// código anterior
\`\`\`

**Depois**:
\`\`\`csharp
// código novo
\`\`\`

**Dependencies**: [Outras mudanças/helpers necessários]

**Notas**:
- [Notas importantes sobre implementação]
- [Diferenças esperadas por plataforma]
- [Gotchas ou armadilhas]

**Status**: 
- ✅ Implementado e testado em [Plataforma]
- 🟡 Implementado em [Plataforma], não testado
- ⏳ Pendente de implementação em [Plataformas]
- ❌ Não aplicável a [Plataforma] por razões [X]
```

---

## 🎯 Checklist para Cada Mudança

Quando fazes mudança em qualquer plataforma:

- [ ] Mudança implementada e testada na plataforma origem
- [ ] Mudança documentada em `PLATFORM_CHANGES/[PLATFORM]_CHANGES.md`
- [ ] Código de referência copiado para `SYNC_REFERENCE`
- [ ] Identificadas plataformas que precisam da mesma mudança
- [ ] Identifiquei diferenças esperadas (UI patterns, APIs, etc.)
- [ ] Commit feito com referência ao doc (ex: "Ref: PLATFORM_CHANGES/ANDROID_CHANGES.md - ClientsPage Delete")

---

## 📊 Status de Sincronização

| Funcionalidade | Android | iOS | macOS | Windows | Notas |
|---|---|---|---|---|---|
| Swipe Delete com Confirmação | ✅ | ✅ | ⏳ | ⏳ | Usar ShowConfirmAsync pattern |
| Swipe Pastas | ✅ | ✅ | ⏳ | ⏳ | Mesmo padrão do Delete |
| Form Delete com Confirmação | ✅ | ✅ | ⏳ | ⏳ | OnDeleteFromFormTapped |
| ... | | | | | |

---

## 🔍 Como Consultar

**Antes de modificar uma página**:
1. Abre `PLATFORM_CHANGES/[PLATFORM]_CHANGES.md`
2. Procura o ficheiro/método
3. Consulta a estrutura em `SYNC_REFERENCE`
4. Verifica `Status` para saber se já foi implementado noutras plataformas

**Para adaptar uma mudança de Android para iOS**:
1. Lê `PLATFORM_CHANGES/ANDROID_CHANGES.md`
2. Consulta código de referência em `SYNC_REFERENCE`
3. Adapta considerando diferenças de UI/APIs iOS
4. Documenta em `PLATFORM_CHANGES/iOS_CHANGES.md`

---

## ⚙️ Regras de Ouro

1. **Documentação ANTES de implementar noutra plataforma**
   - Não implementes cegamente
   - Lê o doc para entender o contexto

2. **SYNC_REFERENCE é apenas consulta**
   - Não é para importar/usar diretamente
   - É um snapshot do que foi feito

3. **Status sempre atualizado**
   - ✅ = Testado e funcionando
   - 🟡 = Implementado mas não testado
   - ⏳ = Ainda não feito
   - ❌ = Não aplicável

4. **Commits referenciam a documentação**
   - Bom: "Add delete confirmation to ClientsPage (Ref: ANDROID_CHANGES.md)"
   - Ruim: "Fix delete button"

5. **Diferenças por plataforma documentadas**
   - iOS uses `ShowConfirmAsync` com GetRootPage
   - Android uses `ShowConfirmAsync` com GetRootPage (MESMO)
   - Diferenças de UI patterns devem estar no doc

---

## 🚀 Próximos Passos

1. Documentar mudanças do Delete/Confirmação que já foram feitas
2. Criar SYNC_REFERENCE com código de referência
3. Para cada nova feature em Android → doc → iOS/macOS
4. Manter tabela de Status atualizada

---

**Atualizado**: 2025-11-09
**Responsável**: Sincronização Multi-Plataforma
