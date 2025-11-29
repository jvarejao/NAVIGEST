namespace NAVIGEST.macOS.Models
{
    public class AbsenceType
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "\uf073"; // Default calendar icon

        public override string ToString() => Description;
    }
}
