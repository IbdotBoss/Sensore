using System.ComponentModel.DataAnnotations.Schema;
namespace Sensore.Models
{
    /// <summary>
    /// Join table for the many-to-many relationship between
    /// clinicians and patients.
    /// </summary>
    /// <remarks>
    /// A composite primary key (ClinicianUserId, PatientUserId)
    /// must be configured in the DbContext (OnModelCreating).
    /// </remarks>
    public class ClinicianPatientMap
    {
        // Composite Key Part 1 & Foreign Key to ApplicationUser
        public string ClinicianUserId { get; set; }
        [ForeignKey("ClinicianUserId")]
        public virtual ApplicationUser ClinicianUser { get; set; }

        // Composite Key Part 2 & Foreign Key to ApplicationUser
        public string PatientUserId { get; set; }
        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser PatientUser { get; set; }
    }
}