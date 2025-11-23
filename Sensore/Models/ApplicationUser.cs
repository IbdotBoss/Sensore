using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    /// <summary>
    /// Extends the built-in IdentityUser with navigation properties
    /// for our custom application tables.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // 1-to-1 relationship with PatientProfile
        public virtual PatientProfile PatientProfile { get; set; }

        // 1-to-many relationship with PressureFrame (a patient has many frames)
        public virtual ICollection<PressureFrame> PressureFrames { get; set; }

        // 1-to-many relationship with Comment (a user can author many comments)
        [InverseProperty("AuthorUser")]
        public virtual ICollection<Comment> AuthoredComments { get; set; }

        // 1-to-many relationship with Comment (a patient can be the subject of many comments)
        [InverseProperty("PatientUser")]
        public virtual ICollection<Comment> SubjectComments { get; set; }

        // Many-to-many: Links to clinicians (if this user is a patient)
        [InverseProperty("PatientUser")]
        public virtual ICollection<ClinicianPatientMap> CliniciansAssigned { get; set; }

        // Many-to-many: Links to patients (if this user is a clinician)
        [InverseProperty("ClinicianUser")]
        public virtual ICollection<ClinicianPatientMap> PatientsAssigned { get; set; }
    }
}
