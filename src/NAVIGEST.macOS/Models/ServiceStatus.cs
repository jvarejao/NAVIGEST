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
                try
                {
                    return new SolidColorBrush(Microsoft.Maui.Graphics.Color.FromArgb(Cor));
                }
                catch
                {
                    return new SolidColorBrush(Microsoft.Maui.Graphics.Colors.Gray);
                }
            }
        }

        public override string ToString() => Descricao;
    }
}
