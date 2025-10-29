namespace NAVIGEST.iOS.Models;

public class ProductFamilyOption
{
    public ProductFamilyOption(string? codigo, string? nome)
    {
        Codigo = (codigo ?? string.Empty).Trim().ToUpperInvariant();
        Nome = (nome ?? string.Empty).Trim().ToUpperInvariant();
    }

    public string Codigo { get; }
    public string Nome { get; }

    public string PickerDisplay => string.IsNullOrWhiteSpace(Nome) ? "Sem descrição" : Nome;
    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? Codigo : Nome;
    public string FullDisplay => string.IsNullOrWhiteSpace(Nome) ? Codigo : $"{Codigo} - {Nome}";

    public override string ToString() => NomeDisplay;
}
