// Models/DbSettings.cs
using MySqlConnector;

namespace NAVIGEST.Android.Models
{
    public sealed class DbSettings
    {
        public string Server { get; set; } = "100.81.152.95";
        public uint Port { get; set; } = 3307;
        public string Database { get; set; } = "YAHPUBLICIDADE2025";
        public string UserId { get; set; } = "YAH";
        public string? Password { get; set; } = null;

        public MySqlSslMode SslMode { get; set; } = MySqlSslMode.Preferred;
        public bool AllowPublicKeyRetrieval { get; set; } = true;
        public int ConnectionTimeout { get; set; } = 10;
        public int DefaultCommandTimeout { get; set; } = 60;
    }
}

