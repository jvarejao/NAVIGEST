namespace NAVIGEST.Android.Models;

public class Product
{
    public string? PRODCODIGO { get; set; }
    public string? PRODNOME { get; set; }
    public string? FAMILIA { get; set; }
    public string? COLABORADOR { get; set; }

    // Display properties for UI bindings
    public string? AvatarColor => GetAvatarColor();
    public string? Initials => GetInitials();
    public string? Descricao => PRODNOME ?? "Sem descrição";
    public string? Codigo => PRODCODIGO ?? "N/A";

    private string GetAvatarColor()
    {
        if (string.IsNullOrWhiteSpace(PRODNOME)) return "#FF9E9E9E";
        
        var colors = new[]
        {
            "#FFFF6B6B", // Red
            "#FFFF922B", // Orange
            "#FFC92A2A", // Dark Red
            "#FF4DABF7", // Blue
            "#FF339AF0", // Dark Blue
            "#FF748FFC", // Purple
            "#FF748FFC", // Purple
            "#FF82C91E"  // Green
        };
        
        int hash = PRODNOME.GetHashCode();
        int index = Math.Abs(hash) % colors.Length;
        return colors[index];
    }

    private string GetInitials()
    {
        if (string.IsNullOrWhiteSpace(PRODNOME)) return "?";
        
        var words = PRODNOME.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "?";
        if (words.Length == 1) return words[0].Substring(0, 1).ToUpper();
        
        return $"{words[0][0]}{words[words.Length - 1][0]}".ToUpper();
    }

    public Product Clone() => new()
    {
        PRODCODIGO = PRODCODIGO,
        PRODNOME = PRODNOME,
        FAMILIA = FAMILIA,
        COLABORADOR = COLABORADOR
    };
}
