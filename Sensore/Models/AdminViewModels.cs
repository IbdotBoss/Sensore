using Microsoft.AspNetCore.Mvc.Rendering;
using Sensore.Models;
using System.ComponentModel.DataAnnotations;

namespace Sensore.Models
{
    // View model for displaying a user in the admin user list.
  // Shows basic user information and assigned role.
    public class UserListViewModel
    {
        // The unique identifier of the user.
        public string Id { get; set; } = string.Empty;

      // The user's email address.
        public string? Email { get; set; }

  // The user's full name for display.
      public string FullName { get; set; } = string.Empty;

        // The user's assigned role (Admin, Clinician, or Patient).
        public string Role { get; set; } = string.Empty;
    }

    // View model for the admin patient linking page.
    // Allows admins to assign/unassign patients to clinicians.
    public class PatientLinkingViewModel
    {
     // The selected clinician's ID to manage assignments for.
        public string? SelectedClinicianId { get; set; }

    // Dropdown list of all available clinicians.
        public SelectList? ClinicianList { get; set; }

 // Patients currently assigned to the selected clinician.
  public List<ApplicationUser> AssignedPatients { get; set; } = new List<ApplicationUser>();

        // Patients not yet assigned to the selected clinician.
       // Available for assignment.
        public List<ApplicationUser> AvailablePatients { get; set; } = new List<ApplicationUser>();
    }

    // View model for creating a new user account.
    // Used by admins to add new users to the system.
    public class CreateUserViewModel
  {
      // The new user's email address. Also used as username.
     [Required]
   [EmailAddress]
        [Display(Name = "Email")]
     public string Email { get; set; } = string.Empty;

       // The new user's full name.
        [Required]
       [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

  // Password for the new account.
      // Must meet complexity requirements.
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

     // Password confirmation to prevent typos.
  [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // The role to assign to the new user.
        [Required]
        [Display(Name = "Role")]
    public string SelectedRole { get; set; } = string.Empty;

   // Dropdown list of available roles.
      public SelectList? RoleList { get; set; }
    }

    // View model for editing an existing user account.
    // Allows admins to update user details, role, and password.
  public class EditUserViewModel
    {
        // The user's unique identifier.
        public string Id { get; set; } = string.Empty;

// The user's email address.
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

      // The user's full name.
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

  // The user's current role for display.
        [Display(Name = "Current Role")]
    public string CurrentRole { get; set; } = string.Empty;

    // The new role to assign (can be same as current).
     [Required]
      [Display(Name = "New Role")]
     public string SelectedRole { get; set; } = string.Empty;

      // Dropdown list of available roles.
      public SelectList? RoleList { get; set; }

   // Optional new password. Leave blank to keep current password.
 [Display(Name = "New Password (leave blank to keep current)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        // Confirmation for the new password.
   [DataType(DataType.Password)]
     [Display(Name = "Confirm new password")]
[Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }
    }
}