namespace AppLoginMaui.Models;

public class Product
{
    public string? PRODCODIGO { get; set; }
    public string? PRODNOME { get; set; }
    public string? FAMILIA { get; set; }
    public string? COLABORADOR { get; set; }

    public Product Clone() => new()
    {
        PRODCODIGO = PRODCODIGO,
        PRODNOME = PRODNOME,
        FAMILIA = FAMILIA,
        COLABORADOR = COLABORADOR
    };
}
