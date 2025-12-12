# Notas de Portabilidade - Atualização Clientes (12/12/2025)

Estas alterações foram realizadas na versão macOS e devem ser replicadas nas versões iOS e Android para manter a consistência.

## 1. Base de Dados (`DatabaseService.cs`)

### Método `GetClientesAsync`
A query SQL foi alterada para incluir os totais de vendas do ano atual e anterior.

**Alterações Críticas:**
- Adicionadas subqueries para `VendasAnoAtual` e `VendasAnoAnterior`.
- **IMPORTANTE:** Foi necessário forçar o `COLLATE utf8mb4_unicode_ci` nas comparações de strings dentro das subqueries para evitar erros de "Illegal mix of collations" no MySQL.

```sql
SELECT ...,
       (SELECT COALESCE(SUM(TotalAmount), 0) FROM OrderInfo o WHERE o.CustomerNo COLLATE utf8mb4_unicode_ci = c.CLICODIGO COLLATE utf8mb4_unicode_ci AND YEAR(o.OrderDate) = YEAR(CURDATE())) as VendasAnoAtual,
       (SELECT COALESCE(SUM(TotalAmount), 0) FROM OrderInfo o WHERE o.CustomerNo COLLATE utf8mb4_unicode_ci = c.CLICODIGO COLLATE utf8mb4_unicode_ci AND YEAR(o.OrderDate) = YEAR(CURDATE()) - 1) as VendasAnoAnterior
FROM CLIENTES c ...
```

## 2. Interface de Utilizador (`ClientsPage.xaml`)

### Lista de Clientes (ItemTemplate)
- **Removido:** Campo `VALORCREDITO`.
- **Adicionado:** Estatísticas de Vendas (Ano Anterior e Ano Atual).
- **Estilos:**
  - **Fonte:** Tamanho **14** para o Nome do Vendedor e Valores de Vendas.
  - **Cores:**
    - Ano Atual: `#28CD41` (Verde)
    - Ano Anterior: `BodyTextColor` (Padrão)
    - Labels descritivas: `EntryPlaceholderColor`

### Exemplo de Layout (XAML)
```xaml
<!-- Vendedor -->
<Label Text="{Binding VENDEDOR, StringFormat='Vend: {0}'}" FontSize="14" ... />

<!-- Sales Stats -->
<HorizontalStackLayout ...>
    <!-- Ano Anterior -->
    <Label Text="{Binding VendasAnoAnterior, StringFormat='{0:N2} €'}" FontSize="14" ... />
    <!-- Ano Atual -->
    <Label Text="{Binding VendasAnoAtual, StringFormat='{0:N2} €'}" FontSize="14" TextColor="#28CD41" ... />
</HorizontalStackLayout>
```

## 3. Lógica de Negócio (`ClientsPageModel.cs`)

### Filtro de Pesquisa (`ApplyFilterImmediate`)
- A pesquisa agora inclui o campo `VENDEDOR`.
- Certificar que a lógica de filtro nas outras plataformas também verifica esta propriedade.

```csharp
var filtered = _all.Where(c =>
    ...
    (c.VENDEDOR ?? string.Empty).ToLowerInvariant().Contains(q));
```

## 4. Modelo de Dados (`Cliente.cs`)
- Certificar que a classe `Cliente` nas outras plataformas possui as propriedades:
  - `decimal VendasAnoAtual`
  - `decimal VendasAnoAnterior`
  - `string VENDEDOR`
