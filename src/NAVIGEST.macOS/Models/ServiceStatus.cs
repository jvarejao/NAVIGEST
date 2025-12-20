using System.Text.Json.Serialization;

namespace NAVIGEST.macOS.Models
{
    public class ServiceStatus
    {
        public int ID { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public string Cor { get; set; } = "#808080"; // Default gray

        [JsonIgnore]
        public Brush ColorBrush
        {
            get
            {
                var color = ResolveColor(Cor);
                return new SolidColorBrush(color);
            }
        }

        public override string ToString() => Descricao;

        private static Microsoft.Maui.Graphics.Color ResolveColor(string? raw)
        {
            // 1) hex (com ou sem #)
            if (TryParseHex(raw, out var parsed))
                return parsed;

            // 2) nome direto na coluna Cor
            var byName = NameToColor(raw);
            if (byName != null)
                return byName;

            // 3) padrão COR###
            if (TryParseCorNumber(raw, out var corNumber))
                return corNumber;

            // 4) fallback hash
            return HashToColor(raw ?? string.Empty);
        }

        private static Microsoft.Maui.Graphics.Color HashToColor(string seed)
        {
            unchecked
            {
                int hash = seed.GetHashCode();
                double hue = Math.Abs(hash % 360);
                const double saturation = 0.55;
                const double lightness = 0.50;
                return Microsoft.Maui.Graphics.Color.FromHsla(hue / 360.0, saturation, lightness);
            }
        }

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

        private static Microsoft.Maui.Graphics.Color NumberToColor(int num)
        {
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
    }
}
