using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Represents a comment in the patient-clinician communication system.
    // Supports threaded discussions with replies.
    // Used for feedback between patients and their care team.
    public class Comment
    {
        // Unique identifier for the comment.
        [Key]
        public int CommentId { get; set; }

        // The user ID of the person who wrote this comment.
        // Can be a Patient or Clinician.
        [ForeignKey("ApplicationUser")]
        public string AuthorUserId { get; set; }

        // Navigation property to the comment author.
        public virtual ApplicationUser AuthorUser { get; set; }

        // The patient this comment is associated with.
        // All comments in a thread belong to a specific patient's record.
        [ForeignKey("ApplicationUser")]
        public string PatientUserId { get; set; }

        // Navigation property to the patient.
        public virtual ApplicationUser PatientUser { get; set; }

        // The timestamp of the sensor data this comment refers to.
        // Links comments to specific pressure readings for context.
        public DateTime ThreadTimestamp { get; set; }

        // The actual text content of the comment.
        public string CommentText { get; set; }

        // When the comment was created.
        public DateTime CreatedAt { get; set; }

        // ========================================================================
        // THREADING SUPPORT
        // Self-referencing relationship for nested replies
        // ========================================================================

        // The ID of the parent comment if this is a reply.
        // Null for top-level comments.
        public int? ParentCommentId { get; set; }

        // Navigation property to the parent comment.
        [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

        // Collection of replies to this comment.
        // Enables threaded discussion view.
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}