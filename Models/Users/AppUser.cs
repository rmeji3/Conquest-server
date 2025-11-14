using Microsoft.AspNetCore.Identity;

namespace Conquest.Models.AppUsers
{
    public class AppUser : IdentityUser
    {
        //public required string UserName { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
    }
}
