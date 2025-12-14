using Microsoft.AspNetCore.Identity;

namespace Sensore.Models
{
    // Custom role class for Admin, Clinician, and Patient roles.
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() : base() { }
        public ApplicationRole(string roleName) : base(roleName) { }
    }
}