using Microsoft.AspNetCore.Identity;

namespace MakrisPortfolio.Server.Data
{
    public class AppUser : IdentityUser
    {
        public string? DisplayName { get; set; }
    }
}
