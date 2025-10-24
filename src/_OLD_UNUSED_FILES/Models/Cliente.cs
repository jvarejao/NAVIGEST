namespace AppLoginMaui.Models
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

