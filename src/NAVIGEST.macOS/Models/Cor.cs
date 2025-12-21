namespace NAVIGEST.macOS.Models
{
    public class Cor
    {
        public string IdCor { get; set; }
        public string NomeCor { get; set; }
        public string? CodigoHex { get; set; }
        public string? Referencia { get; set; }

        public Microsoft.Maui.Graphics.Color MauiColor
        {
            get
            {
                // 1) código hex explícito (novo campo)
                if (TryParseHex(CodigoHex, out var parsedFromCode))
                    return parsedFromCode;

                // 2) hex no Id (mantém compatibilidade)
                if (TryParseHex(IdCor, out var parsed))
                    return parsed;

                // 3) nome da cor (prioritário para alinhar círculo com o nome mostrado)
                var byName = NameToColor(NomeCor) ?? NameToColor(IdCor);
                if (byName != null)
                    return byName;

                // 4) padrão COR###
                if (TryParseCorNumber(IdCor, out var corNumber))
                    return corNumber;

                // 5) fallback determinístico por hash
                return HashToColor(IdCor ?? string.Empty);
            }
        }

        public override string ToString() => NomeCor;

        private static bool TryParseHex(string? value, out Microsoft.Maui.Graphics.Color color)
        {
            color = Microsoft.Maui.Graphics.Colors.Transparent;
            if (string.IsNullOrWhiteSpace(value)) return false;

            var trimmed = value.Trim();
            if (!trimmed.StartsWith("#") && IsHex(trimmed))
                trimmed = "#" + trimmed;

            try
            {
                color = Microsoft.Maui.Graphics.Color.FromArgb(trimmed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseCorNumber(string? value, out Microsoft.Maui.Graphics.Color color)
        {
            color = Microsoft.Maui.Graphics.Colors.Transparent;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var trimmed = value.Trim();
            if (!trimmed.StartsWith("COR", StringComparison.OrdinalIgnoreCase)) return false;
            if (!int.TryParse(trimmed.AsSpan(3), out var num)) return false;
            color = NumberToColor(num);
            return true;
        }

        private static Microsoft.Maui.Graphics.Color HashToColor(string seed)
        {
            // Gera um tom fixo para cada string, garantindo contraste razoável
            unchecked
            {
                int hash = seed.GetHashCode();
                // Hue 0..360
                double hue = Math.Abs(hash % 360);
                const double saturation = 0.55;
                const double lightness = 0.50;
                return Microsoft.Maui.Graphics.Color.FromHsla(hue / 360.0, saturation, lightness);
            }
        }

        private static Microsoft.Maui.Graphics.Color NumberToColor(int num)
        {
            // Usa o número para gerar hue estável (ex.: COR363)
            double hue = Math.Abs(num % 360);
            const double saturation = 0.65;
            const double lightness = 0.55;
            return Microsoft.Maui.Graphics.Color.FromHsla(hue / 360.0, saturation, lightness);
        }

        private static bool IsHex(string s)
        {
            if (s.Length is not (6 or 8)) return false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool isHex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!isHex) return false;
            }
            return true;
        }

        private static Microsoft.Maui.Graphics.Color? NameToColor(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var n = name.Trim().ToLowerInvariant();
            return n switch
            {
                "branco" => Microsoft.Maui.Graphics.Colors.White,
                "preto" => Microsoft.Maui.Graphics.Colors.Black,
                "castanho" => new Microsoft.Maui.Graphics.Color(0.55f, 0.27f, 0.07f),
                "marrom" => new Microsoft.Maui.Graphics.Color(0.55f, 0.27f, 0.07f),
                "vermelho" => Microsoft.Maui.Graphics.Colors.Red,
                "verde" => Microsoft.Maui.Graphics.Colors.Green,
                "azul" => Microsoft.Maui.Graphics.Colors.Blue,
                "laranja" => new Microsoft.Maui.Graphics.Color(1f, 0.55f, 0f),
                "amarelo" => Microsoft.Maui.Graphics.Colors.Yellow,
                "cinzento" => Microsoft.Maui.Graphics.Colors.Gray,
                "cinza" => Microsoft.Maui.Graphics.Colors.Gray,
                "roxo" => Microsoft.Maui.Graphics.Colors.Purple,
                "rosa" => new Microsoft.Maui.Graphics.Color(1f, 0.41f, 0.71f),
                "transparente" => new Microsoft.Maui.Graphics.Color(1f,1f,1f,0.2f),
                _ => (Microsoft.Maui.Graphics.Color?)null
            };
        }
    }
}
