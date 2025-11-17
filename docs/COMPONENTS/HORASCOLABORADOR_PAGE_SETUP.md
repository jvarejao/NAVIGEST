# HorasColaboradorPage - Setup Guide

## Visão Geral

A `HorasColaboradorPage` é uma página que exibe e permite filtrar horas de trabalho dos colaboradores através de dois filtros principais:
- **Filtro por Colaborador**: Seleciona um colaborador específico ou "Todos"
- **Filtro por Data**: Intervalo de datas (data início e fim)

A página carrega dados da tabela `HORASTRABALHADAS` e `COLABORADORESTRAB` no banco de dados.

---

## 1. Estrutura de Pastas

```
src/NAVIGEST.Android/
├── Models/
│   ├── HoraColaborador.cs          # Modelo de registo de horas
│   └── Colaborador.cs              # Modelo de colaborador
├── PageModels/
│   └── HorasColaboradorViewModel.cs # ViewModel com lógica de filtros
├── Pages/
│   ├── HorasColaboradorPage.xaml   # UI (XAML)
│   └── HorasColaboradorPage.xaml.cs # Code-behind
├── Converters/
│   └── StringNullOrEmptyToBoolConverter.cs # Converter para mostrar/ocultar
└── Services/
    └── DatabaseService.cs          # Métodos GetHorasColaboradorAsync, GetColaboradoresAsync
```

---

## 2. Models

### 2.1 HoraColaborador.cs

```csharp
using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NAVIGEST.Android.Models;

public partial class HoraColaborador : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private DateTime dataTrabalho = DateTime.Today;

    [ObservableProperty]
    private int idColaborador;

    [ObservableProperty]
    private string nomeColaborador = string.Empty;

    [ObservableProperty]
    private string? idCliente;

    [ObservableProperty]
    private string? cliente;

    [ObservableProperty]
    private int? idCentroCusto;

    [ObservableProperty]
    private string? descCentroCusto;

    [ObservableProperty]
    private float horasTrab;

    [ObservableProperty]
    private float horasExtras;

    [ObservableProperty]
    private string? observacoes;

    // Propriedades computadas para display
    public string DataFormatted => DataTrabalho.ToString("dd/MM/yyyy");
    public float HorasTotais => HorasTrab + HorasExtras;
    public string HorasTotaisFormatted => $"{HorasTrab:0.00}h norm / {HorasExtras:0.00}h extra";
    public string ResumoLinha => $"{DataFormatted} | {NomeColaborador}\n{HorasTrab:0.00}h normais + {HorasExtras:0.00}h extras = {HorasTotais:0.00}h total";
    public string ClienteInfo => !string.IsNullOrEmpty(Cliente) ? Cliente : "Sem cliente";
    public string CentroCustoInfo => !string.IsNullOrEmpty(DescCentroCusto) ? DescCentroCusto : "Sem centro de custo";
}
```

**Notas:**
- Herda `ObservableObject` do MVVM Toolkit
- Todas as propriedades públicas utilizam `[ObservableProperty]` para binding automático
- Propriedades computadas (sem setter) para exibição formatada

### 2.2 Colaborador.cs

```csharp
namespace NAVIGEST.Android.Models;

public class Colaborador
{
    public int ID { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public decimal? ValorHora { get; set; }

    // Para display no Picker
    public string DisplayText => $"{Nome}" + (string.IsNullOrEmpty(Funcao) ? "" : $" - {Funcao}");
}
```

**Notas:**
- Classe simples sem ObservableObject
- `DisplayText` é propriedade computada usada no Picker binding

---

## 3. ViewModel

### 3.1 HorasColaboradorViewModel.cs

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Maui.Views;
using NAVIGEST.Android.Models;
using NAVIGEST.Android.Services;

namespace NAVIGEST.Android.PageModels;

public partial class HorasColaboradorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<HoraColaborador> horasList = new();

    [ObservableProperty]
    private ObservableCollection<Colaborador> colaboradores = new();

    [ObservableProperty]
    private Colaborador? colaboradorSelecionado;

    [ObservableProperty]
    private DateTime dataFiltroInicio = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime dataFiltroFim = DateTime.Today;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string mensagem = string.Empty;

    // Flag para prevenir recarregos durante inicialização
    private bool _isInitializing = true;

    // Totais computados
    public string TotalHorasNormais => $"Total Normal: {HorasList.Sum(h => h.HorasTrab):0.00}h";
    public string TotalHorasExtra => $"Total Extra: {HorasList.Sum(h => h.HorasExtras):0.00}h";
    public string TotalGeral => $"Total Geral: {HorasList.Sum(h => h.HorasTrab + h.HorasExtras):0.00}h";

    public HorasColaboradorViewModel()
    {
        _ = InicializarAsync();
    }

    private async Task InicializarAsync()
    {
        try
        {
            _isInitializing = true;
            await CarregarColaboradoresAsync();
            await CarregarHorasAsync();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            Mensagem = "Erro ao inicializar página";
        }
        finally
        {
            _isInitializing = false;
        }
    }

    [RelayCommand]
    private async Task CarregarHorasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            Mensagem = "A carregar...";

            // Se "Todos" estiver selecionado (ID = 0), passa null para obter todos
            int? idColaboradorFiltro = ColaboradorSelecionado?.ID == 0 ? null : ColaboradorSelecionado?.ID;

            var horas = await DatabaseService.GetHorasColaboradorAsync(
                idColaboradorFiltro,
                DataFiltroInicio,
                DataFiltroFim
            );

            HorasList.Clear();
            foreach (var hora in horas.OrderByDescending(h => h.DataTrabalho))
            {
                HorasList.Add(hora);
            }

            AtualizarTotais();
            Mensagem = $"{HorasList.Count} registo(s) encontrado(s)";
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            Mensagem = "Erro ao carregar horas";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CarregarColaboradoresAsync()
    {
        try
        {
            var colaboradoresDb = await DatabaseService.GetColaboradoresAsync();
            
            Colaboradores.Clear();
            Colaboradores.Add(new Colaborador { ID = 0, Nome = "Todos" });
            
            foreach (var colab in colaboradoresDb.OrderBy(c => c.Nome))
            {
                Colaboradores.Add(colab);
            }

            ColaboradorSelecionado = Colaboradores.FirstOrDefault();
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    [RelayCommand]
    private async Task AbrirNovoRegistoAsync()
    {
        try
        {
            var popup = new Popups.NovaHoraPopup(new HoraColaborador
            {
                DataTrabalho = DateTime.Today
            }, Colaboradores.Where(c => c.ID > 0).ToList());

            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is HoraColaborador novaHora && novaHora.Id >= 0)
            {
                await CarregarHorasAsync();
                await AppShell.DisplayToastAsync("Registo guardado com sucesso");
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            await AppShell.DisplayToastAsync("Erro ao criar registo");
        }
    }

    [RelayCommand]
    private async Task EditarHoraAsync(HoraColaborador hora)
    {
        if (hora == null) return;

        try
        {
            var popup = new Popups.NovaHoraPopup(hora, Colaboradores.Where(c => c.ID > 0).ToList());
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is HoraColaborador horaEditada && horaEditada.Id >= 0)
            {
                await CarregarHorasAsync();
                await AppShell.DisplayToastAsync("Registo atualizado com sucesso");
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            await AppShell.DisplayToastAsync("Erro ao editar registo");
        }
    }

    [RelayCommand]
    private async Task EliminarHoraAsync(HoraColaborador hora)
    {
        if (hora == null) return;

        try
        {
            bool confirmacao = await Shell.Current.DisplayAlert(
                "Confirmar",
                $"Eliminar registo de {hora.NomeColaborador} do dia {hora.DataFormatted}?",
                "Sim",
                "Não"
            );

            if (!confirmacao) return;

            await DatabaseService.DeleteHoraColaboradorAsync(hora.Id);
            HorasList.Remove(hora);
            AtualizarTotais();
            await AppShell.DisplayToastAsync("Registo eliminado");
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            await AppShell.DisplayToastAsync("Erro ao eliminar registo");
        }
    }

    // Callbacks que só reagem após inicialização
    partial void OnColaboradorSelecionadoChanged(Colaborador? value)
    {
        if (!_isInitializing && value != null)
        {
            _ = CarregarHorasAsync();
        }
    }

    partial void OnDataFiltroInicioChanged(DateTime value)
    {
        if (!_isInitializing)
        {
            _ = CarregarHorasAsync();
        }
    }

    partial void OnDataFiltroFimChanged(DateTime value)
    {
        if (!_isInitializing)
        {
            _ = CarregarHorasAsync();
        }
    }

    private void AtualizarTotais()
    {
        OnPropertyChanged(nameof(TotalHorasNormais));
        OnPropertyChanged(nameof(TotalHorasExtra));
        OnPropertyChanged(nameof(TotalGeral));
    }
}
```

**Pontos Importantes:**
- `_isInitializing` flag previne ciclo infinito de recarregos durante setup
- Callbacks `OnColaboradorSelecionadoChanged`, `OnDataFiltroInicioChanged`, `OnDataFiltroFimChanged` só reagem após inicialização
- `CarregarHorasAsync()` é RelayCommand que pode ser chamado do XAML ou código
- Totais são computados a partir da lista
- Tratamento de erros centralizado via `GlobalErro.TratarErro()`

---

## 4. Converter

### 4.1 StringNullOrEmptyToBoolConverter.cs

```csharp
using System.Globalization;
using Microsoft.Maui.Controls;

namespace NAVIGEST.Android.Converters;

/// <summary>
/// Conversor que transforma uma string vazia ou null em false (visibilidade)
/// Útil para mostrar/ocultar elementos baseado se uma string tem conteúdo
/// </summary>
public class StringNullOrEmptyToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrEmpty(str);
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
```

**Uso no XAML:**
```xaml
IsVisible="{Binding Observacoes, Converter={StaticResource StringNullOrEmptyToBoolConverter}}"
```

---

## 5. UI - XAML Page

### 5.1 HorasColaboradorPage.xaml

**Estrutura:**
- ResourceDictionary com converters
- Grid com 4 linhas: Filtros, Botões, Lista, Rodapé
- CollectionView para lista de horas
- RefreshView para recarregar com gesto
- EmptyView quando não há registos

**Elementos Principais:**
1. **Filtros** (Grid.Row="0"):
   - Picker para selecionar Colaborador
   - DatePicker para Data Início
   - DatePicker para Data Fim
   - Label com mensagem de status

2. **Botões** (Grid.Row="1"):
   - "🔄 Atualizar" - Executa `CarregarHorasCommand`
   - "➕ Novo Registo" - Executa `AbrirNovoRegistoCommand`

3. **Lista** (Grid.Row="2"):
   - RefreshView com binding `IsBusy` e `CarregarHorasCommand`
   - CollectionView com binding `HorasList`
   - ItemTemplate com Border e informações formatadas
   - Swipe Actions para Editar e Eliminar

4. **Rodapé** (Grid.Row="3"):
   - Labels com `TotalHorasNormais`, `TotalHorasExtra`, `TotalGeral`

### 5.2 HorasColaboradorPage.xaml.cs

```csharp
using System;
using Microsoft.Maui.Controls;
using NAVIGEST.Android.PageModels;
using NAVIGEST.Android.Models;
using Microsoft.Extensions.DependencyInjection;

namespace NAVIGEST.Android.Pages;

public partial class HorasColaboradorPage : ContentPage
{
    public HorasColaboradorPage()
    {
        InitializeComponent();
        
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            var vm = services?.GetService<HorasColaboradorViewModel>() ?? new HorasColaboradorViewModel();
            BindingContext = vm;
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            BindingContext = new HorasColaboradorViewModel();
        }
    }

    public HorasColaboradorPage(HorasColaboradorViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }

    private async void OnItemTapped(object sender, TappedEventArgs e)
    {
        try
        {
            if (sender is Border border && border.BindingContext is HoraColaborador hora)
            {
                if (BindingContext is HorasColaboradorViewModel vm)
                {
                    await vm.EditarHoraCommand.ExecuteAsync(hora);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private async void OnEditarSwipe(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItemView siv && siv.BindingContext is HoraColaborador hora)
            {
                if (BindingContext is HorasColaboradorViewModel vm)
                {
                    await vm.EditarHoraCommand.ExecuteAsync(hora);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private async void OnEliminarSwipe(object sender, EventArgs e)
    {
        try
        {
            if (sender is SwipeItemView siv && siv.BindingContext is HoraColaborador hora)
            {
                if (BindingContext is HorasColaboradorViewModel vm)
                {
                    await vm.EliminarHoraCommand.ExecuteAsync(hora);
                }
            }
        }
        catch (Exception ex)
        {
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }
}
```

**Notas:**
- Code-behind mínimo (apenas inicialização e event handlers)
- Dois construtores: um com DI, outro com ViewModel passado
- Métodos de swipe actions passam o contexto para o ViewModel

---

## 6. DatabaseService - Métodos Necessários

### 6.1 GetHorasColaboradorAsync

```csharp
public static async Task<List<HoraColaborador>> GetHorasColaboradorAsync(
    int? idColaborador = null,
    DateTime? dataInicio = null,
    DateTime? dataFim = null)
{
    var list = new List<HoraColaborador>();
    try
    {
        using var conn = new MySqlConnection(GetConnectionString());
        await conn.OpenAsync();

        var sql = @"SELECT ID, DataTrabalho, IDColaborador, NomeColaborador, 
                           IDCliente, Cliente, IDCentroCusto, DescCentroCusto,
                           HorasTrab, HorasExtras, Observacoes
                    FROM HORASTRABALHADAS 
                    WHERE (@IDColaborador IS NULL OR IDColaborador = @IDColaborador)
                      AND (@DataInicio IS NULL OR DataTrabalho >= @DataInicio)
                      AND (@DataFim IS NULL OR DataTrabalho <= @DataFim)
                    ORDER BY DataTrabalho DESC";

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@IDColaborador", idColaborador.HasValue ? idColaborador.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@DataInicio", dataInicio.HasValue ? dataInicio.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@DataFim", dataFim.HasValue ? dataFim.Value : DBNull.Value);

        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new HoraColaborador
            {
                Id = rd.GetInt32(0),
                DataTrabalho = rd.GetDateTime(1),
                IdColaborador = rd.GetInt32(2),
                NomeColaborador = rd.IsDBNull(3) ? string.Empty : rd.GetString(3),
                IdCliente = rd.IsDBNull(4) ? null : rd.GetString(4),
                Cliente = rd.IsDBNull(5) ? null : rd.GetString(5),
                IdCentroCusto = rd.IsDBNull(6) ? null : rd.GetInt32(6),
                DescCentroCusto = rd.IsDBNull(7) ? null : rd.GetString(7),
                HorasTrab = rd.GetFloat(8),
                HorasExtras = rd.GetFloat(9),
                Observacoes = rd.IsDBNull(10) ? null : rd.GetString(10)
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Erro ao obter horas: {ex.Message}");
        throw;
    }
    return list;
}
```

### 6.2 GetColaboradoresAsync

```csharp
public static async Task<List<Colaborador>> GetColaboradoresAsync()
{
    var list = new List<Colaborador>();
    try
    {
        using var conn = new MySqlConnection(GetConnectionString());
        await conn.OpenAsync();

        var sql = @"SELECT ID, Nome, Funcao, ValorHora 
                    FROM COLABORADORESTRAB
                    ORDER BY Nome";

        using var cmd = new MySqlCommand(sql, conn);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            list.Add(new Colaborador
            {
                ID = rd.GetInt32(0),
                Nome = rd.IsDBNull(1) ? string.Empty : rd.GetString(1),
                Funcao = rd.IsDBNull(2) ? null : rd.GetString(2),
                ValorHora = rd.IsDBNull(3) ? null : rd.GetDecimal(3)
            });
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Erro ao obter colaboradores: {ex.Message}");
        return list;
    }
    return list;
}
```

---

## 7. Dependency Injection - MauiProgram.cs

```csharp
// Horas Colaborador
builder.Services.AddTransient<HorasColaboradorViewModel>();
builder.Services.AddTransient<HorasColaboradorPage>();
```

---

## 8. Navegação - MainYahPage.xaml.cs

```csharp
case "horas":
{
    try
    {
        // Resolve a página via DI (respeita o teu construtor com VM)
        var services = this.Handler?.MauiContext?.Services;
        var page = services?.GetService<HorasColaboradorPage>();

        if (page == null)
        {
            // fallback defensivo
            page = new HorasColaboradorPage(new HorasColaboradorViewModel());
        }

        var pageContent = page.Content;

        if (BindingContext is MainYahPageViewModel vmX)
        {
            vmX.IsConfigExpanded = false;
        }

        if (pageContent is not null)
        {
            pageContent.BindingContext = page.BindingContext ?? pageContent.BindingContext;
            ShowContent(pageContent);

            // opcional: invocar OnAppearing
            try
            {
                var mi = page.GetType().GetMethod("OnAppearing",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                mi?.Invoke(page, null);
            }
            catch { }
        }
        else
        {
            await DisplayToastAsync("HorasColaboradorPage sem conteúdo.");
        }
    }
    catch (Exception ex) { TratarErro(ex); }
    CloseSidebarMobileIfNeeded();
    break;
}
```

---

## 9. Tabelas de Banco de Dados

### 9.1 HORASTRABALHADAS

```sql
CREATE TABLE HORASTRABALHADAS (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    DataTrabalho DATE NOT NULL,
    IDColaborador INT NOT NULL,
    NomeColaborador VARCHAR(100),
    IDCliente VARCHAR(50),
    Cliente VARCHAR(100),
    IDCentroCusto INT,
    DescCentroCusto VARCHAR(100),
    HorasTrab FLOAT NOT NULL DEFAULT 0,
    HorasExtras FLOAT NOT NULL DEFAULT 0,
    Observacoes TEXT,
    FOREIGN KEY (IDColaborador) REFERENCES COLABORADORESTRAB(ID)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 9.2 COLABORADORESTRAB

```sql
CREATE TABLE COLABORADORESTRAB (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    Nome VARCHAR(100) NOT NULL,
    Funcao VARCHAR(100),
    ValorHora DECIMAL(10, 2)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

---

## 10. Checklist para iOS/macOS

Ao portar para iOS/macOS:

- [ ] Criar `Models/HoraColaborador.cs` (copiar de Android)
- [ ] Criar `Models/Colaborador.cs` (copiar de Android)
- [ ] Criar `PageModels/HorasColaboradorViewModel.cs` (copiar de Android, sem alterações)
- [ ] Criar `Converters/StringNullOrEmptyToBoolConverter.cs` (copiar de Android)
- [ ] Criar `Pages/HorasColaboradorPage.xaml` (adaptar UI se necessário para plataforma)
- [ ] Criar `Pages/HorasColaboradorPage.xaml.cs` (copiar de Android)
- [ ] Adicionar métodos em `DatabaseService.cs` (GetHorasColaboradorAsync, GetColaboradoresAsync)
- [ ] Registar no `MauiProgram.cs`
- [ ] Adicionar rota em `AppShell.xaml.cs`
- [ ] Adicionar case "horas" em `MainYahPage.xaml.cs`

---

## 11. Problemas Resolvidos

### Problema 1: Ciclo Infinito de Recarregos
**Sintoma:** App congelava ao abrir HorasColaboradorPage  
**Causa:** ViewModel carregava dados, mudanças de propriedades disparavam mais recarregos  
**Solução:** Flag `_isInitializing` protege callbacks durante setup

### Problema 2: XamlParseException - StaticResource not found
**Sintoma:** App crashava com "StringNullOrEmptyToBoolConverter not found"  
**Causa:** Converter usado no XAML mas não registado em ResourceDictionary  
**Solução:** Adicionar `<ContentPage.Resources>` com `StringNullOrEmptyToBoolConverter`

---

## 12. Notas de Performance

- Carregamentos são **lazy** - só quando necessário (botão ou filtro muda)
- Lista está ordenada **descendente por data** (mais recentes primeiro)
- Totais são computados apenas quando lista muda (não a cada frame)
- Usar `RefreshView` para pull-to-refresh ao invés de polling contínuo

