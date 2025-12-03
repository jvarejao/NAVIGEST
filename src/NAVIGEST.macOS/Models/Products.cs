namespace NAVIGEST.macOS.Models;

public class Product
{
    public string? PRODCODIGO { get; set; }
    public string? PRODNOME { get; set; }
    public string? FAMILIA { get; set; }
    public string? COLABORADOR { get; set; }
    public decimal PRECOCUSTO { get; set; }
    public decimal PRECOVENDA { get; set; }
    public decimal TOTALVENDAS { get; set; }

    public Product Clone() => new()
    {
        PRODCODIGO = PRODCODIGO,
        PRODNOME = PRODNOME,
        FAMILIA = FAMILIA,
        COLABORADOR = COLABORADOR,
        PRECOCUSTO = PRECOCUSTO,
        PRECOVENDA = PRECOVENDA,
        TOTALVENDAS = TOTALVENDAS
    };
}
