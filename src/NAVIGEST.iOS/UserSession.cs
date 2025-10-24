using System;

namespace NAVIGEST.iOS
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
        }

        public UserData User { get; set; } = new UserData();
    }
}