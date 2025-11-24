namespace NAVIGEST.iOS.Models
{
    public class AbsenceType
    {
        public int Id { get; set; }
        public string Descricao { get; set; } = string.Empty;

        public override string ToString() => Descricao;
    }
}
