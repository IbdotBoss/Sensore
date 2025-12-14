using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sensore.Models
{
    // Patient-clinician communication with threaded replies.
  public class Comment
    {
        [Key]
   public int CommentId { get; set; }

        // Who wrote this comment
 [Required]
        [ForeignKey("AuthorUser")]
   public string AuthorUserId { get; set; } = string.Empty;

        public virtual ApplicationUser? AuthorUser { get; set; }

   // Which patient's record this is on
[Required]
        [ForeignKey("PatientUser")]
        public string PatientUserId { get; set; } = string.Empty;

   public virtual ApplicationUser? PatientUser { get; set; }

      // Links comment to specific pressure reading
      public DateTime ThreadTimestamp { get; set; }

        // The comment text
       [Required]
        [StringLength(2000)]
        public string CommentText { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

     // Self-reference for threading (null = top-level comment)
        public int? ParentCommentId { get; set; }

     [ForeignKey("ParentCommentId")]
        public virtual Comment? ParentComment { get; set; }

    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    }
}