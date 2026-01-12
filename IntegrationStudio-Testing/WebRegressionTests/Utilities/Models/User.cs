using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationStudioTests.Utilities.Models
{
    public class AdminUserData
    {
        [JsonPropertyName("AdminUser")]
        public User AdminUser { get; set; }
    }

    public class User
    {
        [JsonPropertyName("Username")]
        public string username { get; set; }

        [JsonPropertyName("Password")]
        public string password { get; set; }

        [JsonPropertyName("TenantName")]
        public string tenant { get; set; }
    }
}
