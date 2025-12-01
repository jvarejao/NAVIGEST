using System;

namespace NAVIGEST.macOS
{
    public class UserSession
    {
        public static UserSession Current { get; } = new UserSession();

        public class UserData
        {
            public string Name { get; set; } = "";
            public string Role { get; set; } = "";
            public byte[]? Photo { get; set; }
            public string CompanyName { get; set; } = "";
            public byte[]? CompanyLogo { get; set; }

            public bool IsAdmin => string.Equals(Role, "ADMIN", StringComparison.OrdinalIgnoreCase);
            public bool IsFinancial => IsAdmin || string.Equals(Role, "FINANCEIRA", StringComparison.OrdinalIgnoreCase);
            public bool IsGeneralSupervisor => IsAdmin || string.Equals(Role, "ENC. GERAL", StringComparison.OrdinalIgnoreCase);
        }

        public UserData User { get; set; } = new UserData();
    }
}