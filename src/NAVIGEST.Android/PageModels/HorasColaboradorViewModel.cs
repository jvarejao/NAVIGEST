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

    // Totais
    public string TotalHorasNormais => $"Total Normal: {HorasList.Sum(h => h.HorasTrab):0.00}h";
    public string TotalHorasExtra => $"Total Extra: {HorasList.Sum(h => h.HorasExtras):0.00}h";
    public string TotalGeral => $"Total Geral: {HorasList.Sum(h => h.HorasTrab + h.HorasExtras):0.00}h";

    public HorasColaboradorViewModel()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorViewModel] Construtor iniciado");
            _ = InicializarAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HorasColaboradorViewModel] Erro no construtor: {ex}");
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
        }
    }

    private async Task InicializarAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorViewModel] InicializarAsync - Iniciando");
            await CarregarColaboradoresAsync();
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorViewModel] InicializarAsync - Colaboradores carregados");
            await CarregarHorasAsync();
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorViewModel] InicializarAsync - Horas carregadas");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HorasColaboradorViewModel] Erro em InicializarAsync: {ex}");
            GlobalErro.TratarErro(ex, mostrarAlerta: false);
            Mensagem = "Erro ao inicializar página";
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

            var horas = await DatabaseService.GetHorasColaboradorAsync(
                ColaboradorSelecionado?.ID == 0 ? null : ColaboradorSelecionado?.ID,
                DataFiltroInicio,
                DataFiltroFim
            );

            HorasList.Clear();
            foreach (var hora in horas.OrderByDescending(h => h.DataTrabalho))
            {
                HorasList.Add(hora);
            }

            AtualizarTotais();
            Mensagem = $"{HorasList.Count} registo(s) carregado(s)";
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
            System.Diagnostics.Debug.WriteLine("[HorasColaboradorViewModel] CarregarColaboradoresAsync - Iniciando");
            var colaboradoresDb = await DatabaseService.GetColaboradoresAsync();
            System.Diagnostics.Debug.WriteLine($"[HorasColaboradorViewModel] CarregarColaboradoresAsync - {colaboradoresDb.Count} colaboradores obtidos");
            
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

    partial void OnColaboradorSelecionadoChanged(Colaborador? value)
    {
        _ = CarregarHorasAsync();
    }

    partial void OnDataFiltroInicioChanged(DateTime value)
    {
        _ = CarregarHorasAsync();
    }

    partial void OnDataFiltroFimChanged(DateTime value)
    {
        _ = CarregarHorasAsync();
    }

    private void AtualizarTotais()
    {
        OnPropertyChanged(nameof(TotalHorasNormais));
        OnPropertyChanged(nameof(TotalHorasExtra));
        OnPropertyChanged(nameof(TotalGeral));
    }
}
