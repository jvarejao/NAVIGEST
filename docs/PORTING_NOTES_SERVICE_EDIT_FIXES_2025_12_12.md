# Notas de Portabilidade - ServiceEditPage e Correções (12/12/2025)

Estas alterações foram realizadas na versão macOS para corrigir crashes e problemas visuais, e devem ser replicadas/consideradas ao portar para iOS e Android.

## 1. ServiceEditPage (Nova Página / Refatoração)

A página `ServiceEditPage` (e seu ViewModel `ServiceEditPageModel`) contém lógica crítica de edição de serviços que pode não existir ou estar desatualizada nas versões móveis.

### Alterações Críticas de Arquitetura (Crash Fix)
Para resolver um crash (`SIGTRAP` / `UICollectionViewFlowLayout`) no macOS causado por bindings complexos (`RelativeSource`) dentro de `CollectionView`:

1.  **ViewModel (`ServiceEditPageModel.cs`):**
    *   Implementado padrão "Push" para propriedades de visibilidade.
    *   O ViewModel pai (`ServiceEditPageModel`) agora itera sobre os itens filhos (`OrderedProductViewModel`) e define diretamente as propriedades `ShowFinancials` e `FinancialColumnWidth`.
    *   Isto elimina a necessidade de o Item procurar o Pai na árvore visual.

2.  **View (`ServiceEditPage.xaml`):**
    *   Removidos todos os bindings `RelativeSource={RelativeSource AncestorType=...}` dentro do `DataTemplate`.
    *   Substituídos por bindings diretos: `{Binding ShowFinancials}` e `{Binding FinancialColumnWidth}`.

**Ação Necessária:** Ao implementar esta página no iOS/Android, **NÃO** usar `RelativeSource` para estas propriedades. Usar a mesma lógica do ViewModel do macOS.

## 2. Correção Visual (DatePicker)

O `DatePicker` padrão apresentava problemas de contraste (texto ilegível) dependendo do tema (Claro/Escuro).

**Solução Aplicada:**
Forçar o uso das cores de `Entry` (Campos de texto) para garantir consistência.

```xaml
<DatePicker ...
    TextColor="{DynamicResource EntryTextColor}"
    BackgroundColor="{DynamicResource EntryBackgroundColor}" />
```

**Ação Necessária:** Verificar se os recursos `EntryTextColor` e `EntryBackgroundColor` estão definidos nos `ResourceDictionary` do Android e iOS. Se não, defini-los ou usar as cores equivalentes de corpo/fundo, garantindo que o contraste funciona em ambos os temas.

## 3. StatusPickerPopup (Novo)

Foi criado um novo popup `StatusPickerPopup` para seleção de estados, seguindo o padrão visual dos outros popups (tamanho fixo, botão fechar azul, lista pesquisável).

**Ação Necessária:** Copiar/Adaptar este ficheiro para os projetos móveis se a funcionalidade de edição de estado for necessária.
