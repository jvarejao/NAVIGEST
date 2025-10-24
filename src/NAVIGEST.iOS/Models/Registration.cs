namespace NAVIGEST.iOS.Models
{
    public class Registration
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Name { get; set; } = "";
        public string ContactNo { get; set; } = "";
        public string Categoria1 { get; set; } = "";
        public string Categoria2 { get; set; } = "";
        public string TipoUtilizador { get; set; } = "";
        public string Email { get; set; } = "";
        public byte[]? ProfilePicture { get; set; }
    }
}

