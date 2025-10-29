namespace NAVIGEST.Android.Models;

public class Vendedor
{
    public int Id { get; set; }
    public string? Nome { get; set; }

    public string NomeDisplay => string.IsNullOrWhiteSpace(Nome) ? "(SEM NOME)" : Nome;
}
