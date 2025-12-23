namespace NAVIGEST.macOS.Models
{
    public class Tamanho
    {
        public string IdTamanho { get; set; } = string.Empty;
        public string NomeTamanho { get; set; } = string.Empty;

        public override string ToString() => NomeTamanho;
    }
}
