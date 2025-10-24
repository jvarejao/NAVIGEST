# macOS Entry Border Fix - Solução Definitiva

## Problema Original
- Entry do macOS (Mac Catalyst) tinha border nativo branco/cinza que não combinava com o design
- Ao clicar, aparecia uma "caixa atrás" do campo redondo
- Alguns campos não mostravam texto (EntryValidationBehavior estava mudando TextColor)

## Solução Implementada (23 de outubro de 2025)

### 1. Handler Customizado no MauiProgram.cs
```csharp
#if IOS || MACCATALYST
EntryHandler.Mapper.AppendToMapping("CustomBorder", (handler, view) =>
{
    if (handler.PlatformView is UITextField textField && view is Entry entry)
    {
        // Remove border nativo
        textField.BorderStyle = UITextBorderStyle.None;
        
        // Padding interno (12px laterais)
        textField.LeftView = new UIView(new CoreGraphics.CGRect(0, 0, 12, textField.Frame.Height));
        textField.LeftViewMode = UITextFieldViewMode.Always;
        
        // Border customizado com cantos arredondados
        textField.Layer.CornerRadius = 10;
        textField.Layer.BorderWidth = 1.5f;
        textField.Layer.MasksToBounds = true;
        
        // Animação no foco (azul iOS + mais grosso)
        textField.EditingDidBegin += (s, e) => 
        {
            UIView.Animate(0.2, () => {
                textField.Layer.BorderWidth = 2f;
                textField.Layer.BorderColor = UIColor.FromRGB(0, 122, 255).CGColor;
            });
        };
    }
});
#endif
```

### 2. Correção do EntryValidationBehavior
**Arquivo**: `Behaviors/EntryValidationBehavior.cs`

**Problema**: O método `SetVisual` estava mudando o `TextColor` do Entry, causando campos invisíveis quando a cor do texto era igual ao fundo.

**Solução**: Remover a alteração do TextColor e deixar o estilo do Entry controlar:
```csharp
private void SetVisual(Color underlineColor, Color placeholder)
{
    if (_entry == null) return;

    // NÃO alterar TextColor - deixar o estilo do Entry controlar
    // Apenas muda placeholder e underline
    _entry.PlaceholderColor = placeholder.WithAlpha(0.8f);
    UpdatePlatformUnderline(underlineColor);
}
```

### 3. Resultado Final
✅ Border bonito com cantos arredondados (10px)
✅ Animação suave ao clicar (0.2s)
✅ Cor azul iOS (#007AFF) no foco
✅ Padding interno de 12px
✅ Texto sempre visível em ambos os temas
✅ Funciona em Light e Dark mode

## Não Fazer
❌ Não usar `BorderStyle.RoundedRect` nativo (fica feio)
❌ Não tentar mudar TextColor no validation behavior
❌ Não usar `Scale` animation no Entry (cria problema visual com border)

## Arquivos Modificados
1. `/src/NAVIGEST.macOS/MauiProgram.cs` - Handler customizado
2. `/src/NAVIGEST.macOS/Behaviors/EntryValidationBehavior.cs` - Remoção de TextColor
3. `/src/NAVIGEST.macOS/Resources/Styles/AppleStyles.xaml` - Estilo AppleEntry
