# macOS Button Cursor Hand - Solução

## Problema
Botões no Mac Catalyst não mostram cursor "hand" (pointer) ao passar o mouse por cima.

## Solução Oficial Apple
Segundo a documentação da Apple, o `UIButton` tem propriedade nativa:
- `isPointerInteractionEnabled` - ativa/desativa interação com pointer
- `pointerStyleProvider` - callback para customizar o estilo do pointer

### API Correta (iOS 13.4+, Mac Catalyst 13.4+)
```csharp
// Signature correta do pointerStyleProvider
button.PointerStyleProvider = (UIButton btn, UIPointerEffect proposedEffect, UIPointerShape proposedShape) =>
{
    // Retorna UIPointerStyle customizado
    return UIPointerStyle.CreateStyle(proposedEffect, proposedShape);
};
```

### Tipos de Pointer Styles
1. **UIPointerStyle.GetHidden()** - Esconde o cursor
2. **UIPointerStyle.CreateStyle(effect, shape)** - Style customizado
3. **Efeitos disponíveis**:
   - UIPointerEffect.CreateHighlight() - Destaque no elemento
   - UIPointerEffect.CreateLift() - Efeito de "levantar"
   - UIPointerEffect.CreateHover() - Efeito hover simples

### Implementação no MAUI
```csharp
ButtonHandler.Mapper.AppendToMapping("MacPointerStyle", (handler, view) =>
{
    #if MACCATALYST
    if (handler.PlatformView is UIButton button)
    {
        button.PointerInteractionEnabled = true;
        
        button.PointerStyleProvider = (UIButton btn, UIPointerEffect proposedEffect, UIPointerShape proposedShape) =>
        {
            // Usa o efeito e forma propostos (padrão do sistema)
            // Isso dá o cursor "hand" automaticamente
            return UIPointerStyle.CreateStyle(proposedEffect, proposedShape);
        };
    }
    #endif
});
```

## Alternativas Simples
Se `pointerStyleProvider` não funcionar, alternativa é usar `UIPointerInteraction`:
```csharp
var interaction = new UIPointerInteraction(new MyPointerDelegate());
button.AddInteraction(interaction);
```

## Referências
- https://developer.apple.com/documentation/uikit/uipointerinteraction
- https://developer.apple.com/documentation/uikit/uibutton/pointerstyleprovider
- iOS 13.4+ required for pointer interactions
