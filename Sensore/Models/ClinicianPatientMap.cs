using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class ClinicianPatientMap
    {
        [ForeignKey("ApplicationUser")]
        public string ClinicianUserId { get; set; }
        public ApplicationUser ClinicianUser { get; set; }

        [ForeignKey("ApplicationUser")]
        public string PatientUserId { get; set; }
        public ApplicationUser PatientUser { get; set; }
    }
}