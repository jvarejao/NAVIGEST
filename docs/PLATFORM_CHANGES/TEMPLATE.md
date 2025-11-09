# TEMPLATE - Change Documentation

**Usar este template para documentar cada mudança em PLATFORM_CHANGES/**

---

## Template Padrão

```markdown
### [Ficheiro] - [Método/Propriedade/Classe] ([DATA])

**Tipo**: 
- [ ] Novo método
- [ ] Novo ficheiro
- [ ] Modificação de método existente
- [ ] Refactor
- [ ] Remoção
- [ ] Outro: ___________

**Arquivo**: 
`[Caminho relativo]` (Lines X-Y)

**Propósito**: 
[Explicação clara do porquê - o que isto resolve ou melhora?]

**Contexto**: 
[Background/context se relevante - relacionado com issue #123, etc]

**Antes**:
\`\`\`csharp
// Código anterior (se modificação)
\`\`\`

**Depois**:
\`\`\`csharp
// Código novo
\`\`\`

**Explicação da Mudança**:
[Linha por linha explicação se complexo]

**Dependencies**: 
- [Outra mudança necessária]
- [Helper necessário]
- [Classe/serviço necessário]

**Notas Importantes**:
- [Gotcha 1]
- [Gotcha 2]
- [Por que foi feito assim]

**Teste Manual**:
1. Passo 1
2. Passo 2
3. Verificar: X funciona
4. Verificar: Y não quebrou

**Teste Esperado**: 
[Qual é o comportamento esperado?]

**Status**: 
- [ ] ✅ Implementado e testado em [Platform]
- [ ] 🟡 Implementado em [Platform], não testado
- [ ] ⏳ Pendente de implementação em [Platforms]
- [ ] ❌ Não aplicável a [Platform] por razões: ________

**Aplicável em**: 
- Android: ✅ / 🟡 / ⏳ / ❌
- iOS: ✅ / 🟡 / ⏳ / ❌
- macOS: ✅ / 🟡 / ⏳ / ❌
- Windows: ✅ / 🟡 / ⏳ / ❌

**Diferenças por Plataforma**:
| Aspecto | Android | iOS | macOS | Windows |
|---|---|---|---|---|
| API usado | X | Y | Y | Z |
| UI Pattern | Swipe | Swipe | Menu | Menu |
| Notas | ... | ... | ... | ... |

**Relacionados**: 
- [Outra mudança relacionada]
- Issue: #123
- Commit: abc123

**Revisor**: [Quem revisou]

**Data de Revisão**: [Data]
```

---

## ⚡ Quick Template (Mínimo)

Para mudanças simples, usa versão curta:

```markdown
### [Ficheiro] - [Método] ([DATA])

**Tipo**: Modificação de método existente

**Arquivo**: Pages/ClientsPage.xaml.cs (Lines 110-135)

**Propósito**: [O que faz]

**Antes/Depois**:
- Antes: [Uma linha]
- Depois: [Uma linha]

**Dependencies**: ShowConfirmAsync helper

**Teste**: 
1. Ação X
2. Resultado esperado: Y

**Status**: ✅ Implementado e testado em Android

**Aplicável em**: iOS ✅, macOS ⏳
```

---

## 📋 Exemplo Preenchido

```markdown
### ClientsPage.xaml.cs - OnDeleteSwipeInvoked (2025-11-09)

**Tipo**: Modificação de método existente

**Arquivo**: Pages/ClientsPage.xaml.cs (Lines 110-135)

**Propósito**: 
Adicionar confirmação antes de deletar cliente via swipe. Impede deletions acidentais.

**Contexto**: 
Root cause do problema "Swipe Delete não funciona" foi `async void` handler + DisplayAlert = deadlock.
Resolvido copiando padrão de Form Delete que já funcionava.

**Antes**:
\`\`\`csharp
private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
{
    // Apenas executava delete
    vm.DeleteCommand.Execute(cliente);
}
\`\`\`

**Depois**:
\`\`\`csharp
private async void OnDeleteSwipeInvoked(object sender, EventArgs e)
{
    var confirm = await ShowConfirmAsync("Eliminar Cliente", ..., "Eliminar", "Cancelar");
    if (!confirm) return;
    vm.DeleteCommand.Execute(cliente);
}
\`\`\`

**Explicação da Mudança**:
- Linha 1: Handler permanece `async void` (necessário para `await`)
- Linha 2: Chama ShowConfirmAsync (thread-safe via MainThread.InvokeOnMainThreadAsync)
- Linha 3: Se cancelar (confirm=false), retorna sem fazer nada
- Linha 4: Se confirmar, executa delete (fire-and-forget, sem await)

**Dependencies**: 
- ShowConfirmAsync helper (ClientsPage.xaml.cs lines 30-50)
- GetRootPage helper (ClientsPage.xaml.cs lines 50-70)

**Notas Importantes**:
- ❌ NÃO fazer `await vm.DeleteCommand.Execute()` - causa deadlock!
- ✅ Usar `vm.DeleteCommand.Execute()` fire-and-forget
- Handler é `async void` (não `private async Task`)

**Teste Manual**:
1. Abrir app, navegar a ClientsPage
2. Swipe em cliente da lista
3. Deve aparecer: "Tem a certeza que deseja eliminar 'NomeCliente'?"
4. Clicar "Cancelar" → swipe fecha, cliente continua na lista
5. Swipe novamente no mesmo cliente
6. Clicar "Eliminar" → cliente é removido da lista

**Status**: ✅ Implementado e testado em Android

**Aplicável em**: 
- Android: ✅ Implementado
- iOS: ✅ Já tinha (verificado)
- macOS: ⏳ Pendente
- Windows: ⏳ Pendente

**Relacionados**: 
- OnDeleteFromFormTapped (mesma sessão)
- OnPastasSwipeInvoked (mesmo padrão de confirmação)
- SWIPE_DELETE_PATTERN_LESSON.md (análise completa)
```

---

## ✨ Boas Práticas

### ✅ Bom

```markdown
**Propósito**: 
Adicionar confirmação para evitar deletions acidentais, sincronizando 
com padrão já existente em Form Delete que funciona perfeitamente.

**Dependencies**: 
- ShowConfirmAsync helper (lines 30-50)
- vm.DeleteCommand (já existia, não alterado)

**Notas Importantes**:
- ❌ CRÍTICO: NÃO fazer await do DeleteCommand
- ✅ Usar fire-and-forget execute
- Handler deve ser async void (não Task)
```

### ❌ Ruim

```markdown
**Propósito**: 
Adiciona confirmação

**Dependencies**: 
ShowConfirmAsync

**Notas**:
- Funciona
```

---

## 🎯 Quando Completar

**SEMPRE quando**:
- Fazes mudança em qualquer plataforma
- Adicionas novo método/ficheiro
- Modificas comportamento existente
- Crias novo helper/pattern

**Documentação é ANTES de próxima pessoa usar ou sincronizar!**

---

## 📍 Onde Colocar

Documentação vai em:
- `/docs/PLATFORM_CHANGES/ANDROID_CHANGES.md` (Android)
- `/docs/PLATFORM_CHANGES/iOS_CHANGES.md` (iOS)
- `/docs/PLATFORM_CHANGES/macOS_CHANGES.md` (macOS)

Código de referência vai em:
- `/src/NAVIGEST.Shared/SYNC_REFERENCE/Pages/[Ficheiro]`

---

**Template Version**: 1.0  
**Última Atualização**: 2025-11-09
