using Microsoft.AspNetCore.Mvc.Rendering;
using Sensore.Models;
using System.ComponentModel.DataAnnotations;

namespace Sensore.Models
{
    public class UserListViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class PatientLinkingViewModel
    {
        public string? SelectedClinicianId { get; set; }
        public SelectList? ClinicianList { get; set; }

        public List<ApplicationUser> AssignedPatients { get; set; } = new List<ApplicationUser>();
        public List<ApplicationUser> AvailablePatients { get; set; } = new List<ApplicationUser>();
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string SelectedRole { get; set; } = string.Empty;

        public SelectList? RoleList { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Current Role")]
        public string CurrentRole { get; set; } = string.Empty;

        [Required]
        [Display(Name = "New Role")]
        public string SelectedRole { get; set; } = string.Empty;

        public SelectList? RoleList { get; set; }

        [Display(Name = "New Password (leave blank to keep current)")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string? ConfirmNewPassword { get; set; }
    }
}