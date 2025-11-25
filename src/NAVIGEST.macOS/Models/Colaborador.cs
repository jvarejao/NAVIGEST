namespace NAVIGEST.macOS.Models;

public class Colaborador
{
    public int ID { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Funcao { get; set; }
    public decimal? ValorHora { get; set; }

    // Para display no Picker
    public string DisplayText => $"{Nome}" + (string.IsNullOrEmpty(Funcao) ? "" : $" - {Funcao}");
}
