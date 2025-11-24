using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using NAVIGEST.iOS.Models;
using NAVIGEST.iOS.Services;
using NAVIGEST.iOS.Popups; // Assuming I'll create this namespace
using CommunityToolkit.Maui.Views;

namespace NAVIGEST.iOS.ViewModels;

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

    [ObservableProperty]
    private bool filtrosAbertos = false;

    [ObservableProperty]
    private int tabAtiva = 1; // 1=Resumo, 2=Lista, 3=Calendário

    // Flag para prevenir recarregos durante inicialização
    private bool _isInitializing = true;

    // Totais simples
    public string TotalHorasNormais => $"{HorasList.Sum(h => h.HorasTrab):0.00}h";
    public string TotalHorasExtra => $"{HorasList.Sum(h => h.HorasExtras):0.00}h";
    public string TotalGeral => $"{HorasList.Sum(h => h.HorasTrab + h.HorasExtras):0.00}h";

    // Stats Inteligentes
    public int TotalColaboradores => HorasList.Select(h => h.IdColaborador).Distinct().Count();
    public int TotalDias => HorasList.Select(h => h.DataTrabalho.Date).Distinct().Count();
    public float MediaHorasDia => TotalDias > 0 ? (float)HorasList.Sum(h => h.HorasTrab + h.HorasExtras) / TotalDias : 0;
    public string AlertaExtras => HorasList.Sum(h => h.HorasExtras) > 10 ? $"⚠️ {HorasList.Sum(h => h.HorasExtras):0.00}h extras" : "";
    public bool TemExtras => HorasList.Sum(h => h.HorasExtras) > 0;
    public string PeriodoSelecionado => $"{DataFiltroInicio:dd/MM} → {DataFiltroFim:dd/MM}";
    public string ColaboradorDisplay => ColaboradorSelecionado?.Nome ?? "Selecione";

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
            System.Diagnostics.Debug.WriteLine($"Erro ao inicializar: {ex.Message}");
            Mensagem = "Erro ao inicializar página";
        }
        finally
        {
            _isInitializing = false;
        }
    }

    [RelayCommand]
    private void AlternarFiltros()
    {
        FiltrosAbertos = !FiltrosAbertos;
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
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar horas: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Erro ao carregar colaboradores: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task AbrirNovoRegistoAsync()
    {
        try
        {
            // TODO: Implement NovaHoraPopup in iOS
            /*
            var popup = new Popups.NovaHoraPopup(new HoraColaborador
            {
                DataTrabalho = DateTime.Today
            }, Colaboradores.Where(c => c.ID > 0).ToList());

            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is HoraColaborador novaHora && novaHora.Id >= 0)
            {
                await CarregarHorasAsync();
                await Shell.Current.DisplayAlert("Sucesso", "Registo guardado com sucesso", "OK");
            }
            */
            await Shell.Current.DisplayAlert("Info", "Funcionalidade de novo registo em desenvolvimento", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao criar registo: {ex.Message}");
            await Shell.Current.DisplayAlert("Erro", "Erro ao criar registo", "OK");
        }
    }

    [RelayCommand]
    private async Task EditarHoraAsync(HoraColaborador hora)
    {
        if (hora == null) return;

        try
        {
            // TODO: Implement NovaHoraPopup in iOS
            /*
            var popup = new Popups.NovaHoraPopup(hora, Colaboradores.Where(c => c.ID > 0).ToList());
            var result = await Shell.Current.ShowPopupAsync(popup);

            if (result is HoraColaborador horaEditada && horaEditada.Id >= 0)
            {
                await CarregarHorasAsync();
                await Shell.Current.DisplayAlert("Sucesso", "Registo atualizado com sucesso", "OK");
            }
            */
            await Shell.Current.DisplayAlert("Info", "Funcionalidade de edição em desenvolvimento", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao editar registo: {ex.Message}");
            await Shell.Current.DisplayAlert("Erro", "Erro ao editar registo", "OK");
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
            await Shell.Current.DisplayAlert("Sucesso", "Registo eliminado com sucesso", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erro ao eliminar registo: {ex.Message}");
            await Shell.Current.DisplayAlert("Erro", "Erro ao eliminar registo", "OK");
        }
    }

    partial void OnColaboradorSelecionadoChanged(Colaborador? value)
    {
        // Só recarrega se a inicialização terminou
        if (!_isInitializing && value != null)
        {
            _ = CarregarHorasAsync();
        }
    }

    partial void OnDataFiltroInicioChanged(DateTime value)
    {
        // Só recarrega se a inicialização terminou
        if (!_isInitializing)
        {
            _ = CarregarHorasAsync();
        }
    }

    partial void OnDataFiltroFimChanged(DateTime value)
    {
        // Só recarrega se a inicialização terminou
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
        OnPropertyChanged(nameof(TotalColaboradores));
        OnPropertyChanged(nameof(TotalDias));
        OnPropertyChanged(nameof(MediaHorasDia));
        OnPropertyChanged(nameof(AlertaExtras));
        OnPropertyChanged(nameof(TemExtras));
        OnPropertyChanged(nameof(PeriodoSelecionado));
        OnPropertyChanged(nameof(ColaboradorDisplay));
    }

    // Métodos de atalho de período
    [RelayCommand]
    private void SelecionarHoje()
    {
        DataFiltroInicio = DateTime.Today;
        DataFiltroFim = DateTime.Today;
    }

    [RelayCommand]
    private void SelecionarEstaSemana()
    {
        var hoje = DateTime.Today;
        var diaSemana = (int)hoje.DayOfWeek;
        var inicioSemana = hoje.AddDays(-(diaSemana == 0 ? 6 : diaSemana - 1)); // Segunda-feira
        
        DataFiltroInicio = inicioSemana;
        DataFiltroFim = hoje;
    }

    [RelayCommand]
    private void SelecionarEsteMes()
    {
        var hoje = DateTime.Today;
        DataFiltroInicio = new DateTime(hoje.Year, hoje.Month, 1);
        DataFiltroFim = hoje;
    }

    [RelayCommand]
    private void SelecionarUltimos30Dias()
    {
        DataFiltroInicio = DateTime.Today.AddDays(-30);
        DataFiltroFim = DateTime.Today;
    }

    [RelayCommand]
    private void SelecionarMes(DateTime mesSelecionado)
    {
        DataFiltroInicio = new DateTime(mesSelecionado.Year, mesSelecionado.Month, 1);
        DataFiltroFim = new DateTime(mesSelecionado.Year, mesSelecionado.Month, DateTime.DaysInMonth(mesSelecionado.Year, mesSelecionado.Month));
    }
}
