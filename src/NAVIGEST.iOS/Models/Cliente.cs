namespace NAVIGEST.iOS.Models
{
    public class Cliente
    {
        public string? CLINOME { get; set; }
        public string? CLICODIGO { get; set; }
        public string? TELEFONE { get; set; }
        public string? EMAIL { get; set; }
        public bool EXTERNO { get; set; }
        public bool ANULADO { get; set; }
        public string? VENDEDOR { get; set; }
        public string? VALORCREDITO { get; set; }
        public bool PastasSincronizadas { get; set; }

        // Propriedades computadas para UI estilo iOS Contacts
        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(CLINOME))
                    return "?";

                var parts = CLINOME.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                    return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();

                return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
            }
        }

        public string AvatarColor
        {
            get
            {
                // Cores variadas baseadas na primeira letra do nome
                var colors = new[]
                {
                    "#FF6B6B", "#4ECDC4", "#45B7D1", "#FFA07A", "#98D8C8",
                    "#F7B731", "#5F27CD", "#00D2D3", "#FF9FF3", "#54A0FF",
                    "#48DBFB", "#1DD1A1", "#FF6348", "#FF4757", "#2E86DE"
                };

                if (string.IsNullOrWhiteSpace(CLINOME))
                    return colors[0];

                var index = Math.Abs(CLINOME.ToUpper()[0] - 'A') % colors.Length;
                return colors[index];
            }
        }

        public string StatusIcon
        {
            get
            {
                if (ANULADO) return "\uf057"; // circle-xmark (anulado)
                if (EXTERNO) return "\uf362"; // arrow-up-right-from-square (externo)
                if (PastasSincronizadas) return "\uf00c"; // check (sincronizado)
                return "\uf054"; // chevron-right (normal)
            }
        }

        public string StatusColor
        {
            get
            {
                if (ANULADO) return "#FF3B30"; // vermelho
                if (EXTERNO) return "#FF9500"; // laranja
                if (PastasSincronizadas) return "#34C759"; // verde
                return "#C7C7CC"; // cinza claro
            }
        }

        public Cliente Clone() => new()
        {
            CLINOME = CLINOME,
            CLICODIGO = CLICODIGO,
            TELEFONE = TELEFONE,
            EMAIL = EMAIL,
            EXTERNO = EXTERNO,
            ANULADO = ANULADO,
            VENDEDOR = VENDEDOR,
            VALORCREDITO = VALORCREDITO,
            PastasSincronizadas = PastasSincronizadas
        };
    }
}


