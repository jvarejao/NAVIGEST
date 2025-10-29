namespace NAVIGEST.Android.Models;

public record ProductFamilyOption(string Codigo, string Nome)
{
    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? Codigo : $"{Codigo} · {Nome}";
}
