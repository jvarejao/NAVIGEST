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

    // Computed properties for display
    public string DataFormatted => DataTrabalho.ToString("dd/MM/yyyy");
    public float HorasTotais => HorasTrab + HorasExtras;
    public string HorasTotaisFormatted => $"{HorasTrab:0.00}h norm / {HorasExtras:0.00}h extra";
    public string ResumoLinha => $"{DataFormatted} | {NomeColaborador}\n{HorasTrab:0.00}h normais + {HorasExtras:0.00}h extras = {HorasTotais:0.00}h total";
    public string ClienteInfo => !string.IsNullOrEmpty(Cliente) ? Cliente : "Sem cliente";
    public string CentroCustoInfo => !string.IsNullOrEmpty(DescCentroCusto) ? DescCentroCusto : "Sem centro de custo";

    // Calcula automaticamente as horas (considera máximo 8h normais)
    public void CalcularHoras(float totalHoras)
    {
        if (totalHoras <= 0)
        {
            HorasTrab = 0;
            HorasExtras = 0;
            return;
        }

        HorasTrab = Math.Min(totalHoras, 8.0f);
        HorasExtras = Math.Max(totalHoras - 8.0f, 0.0f);

        OnPropertyChanged(nameof(HorasTotais));
        OnPropertyChanged(nameof(HorasTotaisFormatted));
        OnPropertyChanged(nameof(ResumoLinha));
    }
}
