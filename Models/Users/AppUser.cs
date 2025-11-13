using Microsoft.AspNetCore.Identity;

namespace Conquest.Models.AppUsers
{
    public class AppUser : IdentityUser
    {
        public string? DisplayName { get; set; }
    }
}
