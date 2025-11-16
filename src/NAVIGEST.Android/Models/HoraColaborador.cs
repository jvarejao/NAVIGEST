using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NAVIGEST.Android.Models;

public partial class HoraColaborador : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string codColab = string.Empty;

    [ObservableProperty]
    private string nomeColab = string.Empty;

    [ObservableProperty]
    private DateTime data = DateTime.Today;

    [ObservableProperty]
    private TimeSpan horaInicio = new TimeSpan(8, 0, 0);

    [ObservableProperty]
    private TimeSpan horaFim = new TimeSpan(17, 0, 0);

    [ObservableProperty]
    private decimal horasNormais;

    [ObservableProperty]
    private decimal horasExtra;

    [ObservableProperty]
    private string tarefa = string.Empty;

    [ObservableProperty]
    private string? obs;

    [ObservableProperty]
    private bool validado;

    [ObservableProperty]
    private string utilizador = string.Empty;

    // Computed properties for display
    public string DataFormatted => Data.ToString("dd/MM/yyyy");
    public string HoraInicioFormatted => HoraInicio.ToString(@"hh\:mm");
    public string HoraFimFormatted => HoraFim.ToString(@"hh\:mm");
    public string HorasTotaisFormatted => $"{HorasNormais:0.00}h norm / {HorasExtra:0.00}h extra";
    public string ResumoLinha => $"{DataFormatted} | {NomeColab}\n{HoraInicioFormatted}–{HoraFimFormatted} ({HorasTotaisFormatted})";

    // Calcula automaticamente as horas
    public void CalcularHoras()
    {
        if (HoraFim <= HoraInicio)
        {
            HorasNormais = 0;
            HorasExtra = 0;
            return;
        }

        var duracao = (HoraFim - HoraInicio).TotalHours;
        HorasNormais = (decimal)Math.Min(duracao, 8.0);
        HorasExtra = (decimal)Math.Max(duracao - 8.0, 0.0);

        OnPropertyChanged(nameof(HorasTotaisFormatted));
        OnPropertyChanged(nameof(ResumoLinha));
    }
}
