using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NAVIGEST.macOS.Models;

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

    [ObservableProperty]
    private string? icon; // Icon from AbsenceType

    // Computed properties for display
    public string DataFormatted => DataTrabalho.ToString("dd/MM/yyyy");
    public float HorasTotais => HorasTrab + HorasExtras;
    public string HorasTotaisFormatted => $"{HorasTrab:0.00}h norm / {HorasExtras:0.00}h extra";
    public string ResumoLinha => $"{DataFormatted} | {NomeColaborador}\n{HorasTrab:0.00}h normais + {HorasExtras:0.00}h extras = {HorasTotais:0.00}h total";
    public string ClienteInfo => !string.IsNullOrEmpty(Cliente) ? Cliente : "Sem cliente";
    public string CentroCustoInfo => !string.IsNullOrEmpty(DescCentroCusto) ? DescCentroCusto : "Sem centro de custo";

    public string DisplayIcon
    {
        get
        {
            if (IdCentroCusto.HasValue && IdCentroCusto.Value > 0)
            {
                return !string.IsNullOrEmpty(Icon) ? Icon : "\uf071"; // Warning icon
            }
            return "\uf1ad"; // Building icon
        }
    }

    public string DisplayText
    {
        get
        {
            if (IdCentroCusto.HasValue && IdCentroCusto.Value > 0)
            {
                return DescCentroCusto ?? "Sem descrição";
            }
            return !string.IsNullOrEmpty(Cliente) ? Cliente : "Sem cliente";
        }
    }

    public string DisplayInfo 
    {
        get 
        {
            if (IdCentroCusto.HasValue && IdCentroCusto.Value > 0)
            {
                // Use the Icon property if available, otherwise fallback or empty
                var displayIcon = !string.IsNullOrEmpty(Icon) ? Icon : "⚠️";
                return $"{displayIcon} {DescCentroCusto}";
            }
            return !string.IsNullOrEmpty(Cliente) ? $"🏢 {Cliente}" : "🏢 Sem cliente";
        }
    }

    // Calcula automaticamente as horas (considera máximo 8h normais)
    public void CalcularHoras(float totalHoras)
    {
        if (totalHoras <= 8)
        {
            HorasTrab = totalHoras;
            HorasExtras = 0;
        }
        else
        {
            HorasTrab = 8;
            HorasExtras = totalHoras - 8;
        }
    }
}
