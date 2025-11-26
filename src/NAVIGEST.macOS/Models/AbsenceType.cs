namespace NAVIGEST.macOS.Models
{
    public class AbsenceType
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;

        public override string ToString() => Description;
    }
}
