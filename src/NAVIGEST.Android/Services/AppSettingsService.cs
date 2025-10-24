// Services/AppSettingsService.cs
using NAVIGEST.Android.Models;
using Microsoft.Maui.Storage;

namespace NAVIGEST.Android.Services
{
    public class AppSettingsService
    {
        private const string KeyServer = "db.server";
        private const string KeyPort = "db.port";
        private const string KeyDatabase = "db.database";
        private const string KeyUserId = "db.userid";
        private const string KeyPassword = "db.password";

        public DbSettings Load()
        {
            return new DbSettings
            {
                Server = Preferences.Default.Get(KeyServer, "100.81.152.95"),
                Port = (uint)Preferences.Default.Get(KeyPort, 3308),
                Database = Preferences.Default.Get(KeyDatabase, "YAHPUBLICIDADE2025"),
                UserId = Preferences.Default.Get(KeyUserId, "YAH"),
                Password = Preferences.Default.Get(KeyPassword, "#JONy2244&")
            };
        }

        public void Save(DbSettings settings)
        {
            Preferences.Default.Set(KeyServer, settings.Server);
            Preferences.Default.Set(KeyPort, (int)settings.Port);
            Preferences.Default.Set(KeyDatabase, settings.Database);
            Preferences.Default.Set(KeyUserId, settings.UserId);
            Preferences.Default.Set(KeyPassword, settings.Password ?? string.Empty);
        }
    }
}

