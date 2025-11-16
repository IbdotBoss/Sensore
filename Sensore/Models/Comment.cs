using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    /// <summary>
    /// Represents a single comment in a threaded discussion,
    /// linked to a patient and a specific data timestamp.
    /// </summary>
    public class Comment
    {
        public Comment()
        {
            Replies = new HashSet<Comment>();
        }

        [Key]
        public long CommentId { get; set; }

        /// <summary>
        /// The timestamp of the pressure data this comment refers to.
        /// </summary>
        [Required]
        public DateTime ThreadTimestamp { get; set; }

        [Required]
        public string CommentText { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Foreign key for the user who *wrote* the comment
        public string AuthorUserId { get; set; }
        [ForeignKey("AuthorUserId")]
        public virtual ApplicationUser AuthorUser { get; set; }

        // Foreign key for the patient this comment is *about*
        public string PatientUserId { get; set; }
        [ForeignKey("PatientUserId")]
        public virtual ApplicationUser PatientUser { get; set; }

        // Self-referencing key for threaded replies
        public long? ParentCommentId { get; set; } // Nullable for top-level comments
        [ForeignKey("ParentCommentId")]
        public virtual Comment ParentComment { get; set; }

        // Navigation property for replies
        public virtual ICollection<Comment> Replies { get; set; }
    }
}