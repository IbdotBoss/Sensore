using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }

        [ForeignKey("ApplicationUser")]
        public string AuthorUserId { get; set; }
        public virtual ApplicationUser AuthorUser { get; set; }

        [ForeignKey("ApplicationUser")]
        public string PatientUserId { get; set; }
        public virtual ApplicationUser PatientUser { get; set; }

        // The specific moment in the sensor data this comment refers to
        public DateTime ThreadTimestamp { get; set; }

        public string CommentText { get; set; }
        public DateTime CreatedAt { get; set; }

        // Self-referencing for threaded replies
        public int? ParentCommentId { get; set; }
        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        // Navigation property for replies
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}