namespace NAVIGEST.iOS.Models
{
    public sealed class Vendedor
    {
        public int Id { get; init; }
        public string Nome { get; init; } = string.Empty;

        public override string ToString() => Nome;
    }
}
