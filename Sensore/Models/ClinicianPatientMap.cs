using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Represents the many-to-many relationship between Clinicians and Patients.
  // Allows a clinician to be assigned to multiple patients and vice versa.
    // Used to control which patients a clinician can view and manage.
    public class ClinicianPatientMap
  {
        // Foreign key to the clinician user.
       // Part of the composite primary key.
     [ForeignKey("ApplicationUser")]
     public string ClinicianUserId { get; set; }

      // Navigation property to the clinician user.
  public ApplicationUser ClinicianUser { get; set; }

    // Foreign key to the patient user.
        // Part of the composite primary key.
        [ForeignKey("ApplicationUser")]
      public string PatientUserId { get; set; }

  // Navigation property to the patient user.
       public ApplicationUser PatientUser { get; set; }
    }
}