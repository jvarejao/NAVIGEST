# Guia de Portabilidade: Validação de Duplicados e Lista de Sugestões

Este documento descreve como implementar a funcionalidade de **Validação de Duplicados** e **Lista de Sugestões (Type-ahead)** nas páginas de **Clientes** e **Produtos**. Esta funcionalidade foi implementada inicialmente na versão macOS e deve ser portada para iOS, Android e Windows.

## 1. Visão Geral da Funcionalidade

### Objetivo
Prevenir a criação de registos duplicados (mesmo nome) e facilitar a pesquisa de registos existentes durante a criação/edição.

### Comportamento
1.  Ao digitar no campo "Nome" (ou "Descrição"), uma lista de sugestões aparece logo abaixo do campo.
2.  A lista mostra registos existentes que contêm o texto digitado.
3.  A lista **não deve tapar** o campo de entrada (input).
4.  Ao tentar gravar, o sistema valida se o nome já existe e impede a gravação se for um duplicado (exceto se for o próprio registo a ser editado).
5.  A interface distingue entre "Novo" (Botão "Guardar", Título "Adicionar") e "Editar" (Botão "Atualizar", Título "Editar...").

---

## 2. Implementação no ViewModel (`PageModel`)

A lógica é partilhada e deve ser implementada no `ClientsPageModel` e `ProductsPageModel`.

### 2.1. Propriedades Necessárias

```csharp
// Coleção para a lista de sugestões
public ObservableCollection<Cliente> NameSuggestions { get; } = new();

// Controlo de visibilidade da lista
private bool _isSuggestionsVisible;
public bool IsSuggestionsVisible
{
    get => _isSuggestionsVisible;
    set
    {
        if (_isSuggestionsVisible != value)
        {
            _isSuggestionsVisible = value;
            OnPropertyChanged();
        }
    }
}

// Texto dinâmico para o botão de ação
public string SaveButtonText => IsNew ? "Guardar" : "Atualizar";
```

### 2.2. Método de Atualização de Sugestões

```csharp
public void UpdateNameSuggestions(string query)
{
    if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
    {
        NameSuggestions.Clear();
        IsSuggestionsVisible = false;
        return;
    }

    var normalizedQuery = query.Trim().ToUpperInvariant();
    
    // Ignora o próprio registo que estamos a editar
    var currentCode = EditModel?.CLICODIGO; // ou PRODREF para produtos

    var matches = _all
        .Where(c => c.CLINOME != null && 
                    c.CLINOME.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) &&
                    c.CLICODIGO != currentCode)
        .Take(5)
        .ToList();

    NameSuggestions.Clear();
    foreach (var m in matches)
        NameSuggestions.Add(m);

    IsSuggestionsVisible = NameSuggestions.Count > 0;
}
```

### 2.3. Validação no `OnSaveAsync`

```csharp
// Validação de duplicados (Nome)
var duplicate = _all.FirstOrDefault(c => 
    c.CLINOME.Equals(EditModel.CLINOME, StringComparison.OrdinalIgnoreCase) && 
    c.CLICODIGO != EditModel.CLICODIGO);

if (duplicate != null)
{
    await AppShell.DisplayToastAsync($"Já existe um registo com este nome: {duplicate.CLICODIGO}", ToastTipo.Erro, 4000);
    return;
}
```

---

## 3. Implementação na View (XAML)

A estrutura visual requer um `Grid` para sobrepor a lista de sugestões ao resto do formulário, sem empurrar o conteúdo (overlay).

### 3.1. Estrutura do Campo "Nome"

```xaml
<VerticalStackLayout Spacing="6">
    <Label Text="Nome" ... />
    
    <!-- Grid Wrapper para Overlay -->
    <Grid>
        <!-- Campo de Entrada -->
        <Entry Text="{Binding Editing.CLINOME}" 
               TextChanged="OnNameTextChanged"
               VerticalOptions="Start" ... />
        
        <!-- Lista de Sugestões (Overlay) -->
        <Border IsVisible="{Binding IsSuggestionsVisible}"
                VerticalOptions="Start"
                Margin="0,60,0,0" 
                ZIndex="999" ...>
            <!-- Margin Top 60 garante que aparece DEBAIXO do Entry (altura ~44 + margem) -->
            
            <CollectionView ItemsSource="{Binding NameSuggestions}" ...>
                ... Template da lista ...
            </CollectionView>
        </Border>
    </Grid>
</VerticalStackLayout>
```

### 3.2. Notas Importantes de UI (Cross-Platform)

*   **`ZIndex="999"`**: Essencial para garantir que a lista flutua sobre os outros campos (Telefone, Email, etc.).
*   **`Margin="0,60,0,0"`**:
    *   **macOS**: 60 funciona bem para um Entry de altura 44.
    *   **iOS/Android**: Pode ser necessário ajustar este valor dependendo da altura do Entry e densidade do ecrã. Testar se 60 é suficiente ou excessivo.
*   **`VerticalOptions="Start"`**: Importante no `Border` para que ele "pendure" do topo do Grid e respeite a margem superior.

### 3.3. Distinção Novo/Editar (Header e Botão)

**Header (Label):**
```xaml
<Label>
    <Label.Style>
        <Style TargetType="Label">
            <Setter Property="Text" Value="{Binding Editing.CLINOME, StringFormat='Editar {0}'}" />
            <Style.Triggers>
                <DataTrigger TargetType="Label" Binding="{Binding IsNew}" Value="True">
                    <Setter Property="Text" Value="Adicionar Cliente" />
                </DataTrigger>
            </Style.Triggers>
        </Style>
    </Label.Style>
</Label>
```

**Botão de Ação:**
```xaml
<Button Text="{Binding SaveButtonText}" Command="{Binding SaveCommand}" ... />
```

---

## 4. Code-Behind (`.xaml.cs`)

É necessário ligar o evento `TextChanged` para invocar a lógica do ViewModel.

```csharp
private void OnNameTextChanged(object sender, TextChangedEventArgs e)
{
    if (BindingContext is ClientsPageModel vm)
    {
        // Opcional: Forçar Uppercase na UI
        if (sender is Entry entry && !string.IsNullOrEmpty(entry.Text))
        {
            var upper = entry.Text.ToUpper();
            if (entry.Text != upper)
            {
                entry.Text = upper;
                entry.CursorPosition = upper.Length;
            }
        }

        // Invocar pesquisa
        vm.UpdateNameSuggestions(e.NewTextValue);
    }
}
```

## 5. Checklist de Portabilidade

Ao implementar em **iOS, Android e Windows**, verificar:

1.  [ ] **Sobreposição UI**: A lista de sugestões aparece *por cima* dos campos seguintes (Telefone/Email) e não empurra o layout?
2.  [ ] **Posicionamento**: A margem superior (`60`) está correta? A lista não tapa o próprio campo de texto?
3.  [ ] **Toque**: Em dispositivos móveis, selecionar um item da lista de sugestões funciona ao primeiro toque? (Verificar `InputTransparent` ou conflitos de gestos).
4.  [ ] **Teclado**: Em mobile, a lista de sugestões fica visível com o teclado virtual aberto? (Pode ser necessário usar um `ScrollView` ou ajustar o `HeightRequest` máximo da lista).
