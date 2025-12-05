using Microsoft.AspNetCore.Identity;

namespace Sensore.Models
{
    public class ApplicationRole : IdentityRole
    {
        // Constructor needed for seeding
        public ApplicationRole() : base() { }

        public ApplicationRole(string roleName) : base(roleName) { }

        // You can add custom properties here in the future
        // e.g. public string Description { get; set; }
    }
}