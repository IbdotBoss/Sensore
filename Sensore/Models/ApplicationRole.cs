using Microsoft.AspNetCore.Identity;

namespace Sensore.Models
{
    // Represents a role in the Sensore system.
    // Extends IdentityRole to support custom role properties.
    // Available roles: Admin, Clinician, Patient.
    public class ApplicationRole : IdentityRole
    {
        // Default constructor required for Entity Framework and seeding.
        public ApplicationRole() : base() { }

        // Constructor that sets the role name.
        // param: roleName - The name of the role (Admin, Clinician, or Patient)
        public ApplicationRole(string roleName) : base(roleName) { }

        // Custom properties can be added here in the future
        // Example: public string Description { get; set; }
    }
}