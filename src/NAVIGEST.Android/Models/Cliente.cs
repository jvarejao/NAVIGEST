namespace NAVIGEST.Android.Models
{
    public class Cliente
    {
        public string? CLINOME { get; set; }
        public string? CLICODIGO { get; set; }
        public string? TELEFONE { get; set; }
        public string? INDICATIVO { get; set; }
        public string? EMAIL { get; set; }
        public bool EXTERNO { get; set; }
        public bool ANULADO { get; set; }
        public string? VENDEDOR { get; set; }
        public string? VALORCREDITO { get; set; }
        public bool PastasSincronizadas { get; set; }
        
        // Display properties for UI
        public string? AvatarColor => GetAvatarColor();
        public string? Initials => GetInitials();
        public string? TelefoneDisplay => TELEFONE ?? "N/A";
        public string? StatusIcon => GetStatusIcon();
        public string? StatusColor => GetStatusColor();
        public int ServicosCount { get; set; }

        private string GetAvatarColor()
        {
            // Generate color based on first letter
            if (string.IsNullOrEmpty(CLINOME)) return "#2196F3";
            
            var colors = new[] 
            { 
                "#2196F3", "#F44336", "#4CAF50", "#FF9800", 
                "#9C27B0", "#00BCD4", "#FFC107", "#E91E63"
            };
            
            var code = CLINOME[0].GetHashCode() % colors.Length;
            return colors[Math.Abs(code)];
        }

        private string GetInitials()
        {
            if (string.IsNullOrEmpty(CLINOME)) return "?";
            var parts = CLINOME.Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[^1][0]}".ToUpper();
            return CLINOME[0].ToString().ToUpper();
        }

        private string GetStatusIcon()
        {
            // Retorna apenas o caractere unicode, sem &# wrapper
            // FontAwesome 7 codes: X (anulado), check (externo), circle (normal)
            return ANULADO ? "\uF057" : EXTERNO ? "\uF058" : "\uF05D";
        }

        private string GetStatusColor()
        {
            return ANULADO ? "#F44336" : EXTERNO ? "#FF9800" : "#4CAF50";
        }

        public Cliente Clone() => new()
        {
            CLINOME = CLINOME,
            CLICODIGO = CLICODIGO,
            TELEFONE = TELEFONE,
            INDICATIVO = INDICATIVO,
            EMAIL = EMAIL,
            EXTERNO = EXTERNO,
            ANULADO = ANULADO,
            VENDEDOR = VENDEDOR,
            VALORCREDITO = VALORCREDITO,
            PastasSincronizadas = PastasSincronizadas,
            ServicosCount = ServicosCount
        };
    }
}

