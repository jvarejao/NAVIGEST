namespace NAVIGEST.macOS.Models
{
    public class Tamanho
    {
        public string IdTamanho { get; set; }
        public string NomeTamanho { get; set; }

        public override string ToString() => NomeTamanho;
    }
}
