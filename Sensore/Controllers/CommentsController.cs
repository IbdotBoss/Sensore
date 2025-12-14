using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    // Provides direct access to comment threads and management.
    // Used for viewing conversation history and managing messages.
    // All actions require authentication.
    [Authorize]
    public class CommentsController : Controller
    {
private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Lists all comments with optional filtering.
        // Can filter by patient ID to see all messages for one patient,
// or by timestamp to find a specific conversation thread.
        public async Task<IActionResult> Index(string? patientId, DateTime? threadTimestamp)
     {
    var query = _context.Comments
       .Include(c => c.AuthorUser)
                .Include(c => c.PatientUser)
         .Include(c => c.ParentComment)
       .AsQueryable();

            if (!string.IsNullOrEmpty(patientId))
            {
       query = query.Where(c => c.PatientUserId == patientId);
  }

   if (threadTimestamp.HasValue)
  {
             query = query.Where(c => c.ThreadTimestamp == threadTimestamp.Value);
            }

            var comments = await query
      .OrderByDescending(c => c.CreatedAt)
              .ToListAsync();

   ViewBag.PatientId = patientId;
    ViewBag.ThreadTimestamp = threadTimestamp;

            return View(comments);
        }

   // Shows a full conversation thread for a patient.
     // Displays all messages and replies in chronological order.
        public async Task<IActionResult> Thread(string patientId, DateTime timestamp)
  {
            if (string.IsNullOrEmpty(patientId))
        {
      return NotFound();
      }

   var patient = await _context.Users.FindAsync(patientId);
            if (patient == null)
            {
return NotFound();
     }

      var comments = await _context.Comments
     .Include(c => c.AuthorUser)
                .Include(c => c.ParentComment)
          .Where(c => c.PatientUserId == patientId && c.ThreadTimestamp == timestamp)
      .OrderBy(c => c.CreatedAt)
      .ToListAsync();

            ViewBag.Patient = patient;
        ViewBag.ThreadTimestamp = timestamp;

   return View(comments);
   }

        // Shows a single comment with all its replies.
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
    return NotFound();
    }

      var comment = await _context.Comments
           .Include(c => c.AuthorUser)
      .Include(c => c.PatientUser)
              .Include(c => c.ParentComment)
        .FirstOrDefaultAsync(m => m.CommentId == id);

  if (comment == null)
        {
    return NotFound();
            }

            var replies = await _context.Comments
       .Include(r => r.AuthorUser)
     .Where(r => r.ParentCommentId == id)
        .OrderBy(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Replies = replies;

            return View(comment);
      }

        // Shows the form to create a new comment or reply.
   // Requires patient ID and thread timestamp to link the comment properly.
        public IActionResult Create(string patientId, DateTime? threadTimestamp, int? parentCommentId)
      {
            if (string.IsNullOrEmpty(patientId) || !threadTimestamp.HasValue)
        {
  return BadRequest("Patient ID and thread timestamp are required");
  }

            var comment = new Comment
  {
     PatientUserId = patientId,
          ThreadTimestamp = threadTimestamp.Value,
     ParentCommentId = parentCommentId
          };

            ViewBag.PatientId = patientId;
            ViewBag.ThreadTimestamp = threadTimestamp;
  ViewBag.ParentCommentId = parentCommentId;

  return View(comment);
        }

    // Saves a new comment to the database.
  // The current user is automatically set as the author.
    [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comment comment)
      {
       var currentUser = await _userManager.GetUserAsync(User);
if (currentUser == null)
            {
    return Unauthorized();
    }

   comment.AuthorUserId = currentUser.Id;
            comment.CreatedAt = DateTime.UtcNow;

if (ModelState.IsValid)
            {
                _context.Add(comment);
       await _context.SaveChangesAsync();
  return RedirectToAction(nameof(Thread), new { patientId = comment.PatientUserId, timestamp = comment.ThreadTimestamp });
            }

      return View(comment);
        }

        // Shows the edit form for a comment.
        // Only the original author or an admin can edit.
    public async Task<IActionResult> Edit(int? id)
        {
      if (id == null)
            {
        return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
       return NotFound();
    }

   var currentUser = await _userManager.GetUserAsync(User);
      if (currentUser == null || (comment.AuthorUserId != currentUser.Id && !User.IsInRole("Admin")))
            {
      return Forbid();
          }

            return View(comment);
        }

        // Saves changes to an existing comment.
        // Only the original author or an admin can edit.
        [HttpPost]
      [ValidateAntiForgeryToken]
 public async Task<IActionResult> Edit(int id, Comment comment)
        {
       if (id != comment.CommentId)
    {
  return NotFound();
      }

          var currentUser = await _userManager.GetUserAsync(User);
    if (currentUser == null || (comment.AuthorUserId != currentUser.Id && !User.IsInRole("Admin")))
            {
      return Forbid();
            }

        if (ModelState.IsValid)
            {
                try
         {
               _context.Update(comment);
                    await _context.SaveChangesAsync();
      }
            catch (DbUpdateConcurrencyException)
         {
         if (!CommentExists(comment.CommentId))
          {
      return NotFound();
     }
             else
     {
throw;
          }
}
                return RedirectToAction(nameof(Thread), new { patientId = comment.PatientUserId, timestamp = comment.ThreadTimestamp });
     }
    return View(comment);
   }

        // Permanently deletes a comment.
        // Only the original author or an admin can delete.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
     public async Task<IActionResult> DeleteConfirmed(int id)
     {
        var comment = await _context.Comments.FindAsync(id);
            if (comment != null)
       {
     var currentUser = await _userManager.GetUserAsync(User);
     if (currentUser == null || (comment.AuthorUserId != currentUser.Id && !User.IsInRole("Admin")))
       {
                    return Forbid();
    }

           var patientId = comment.PatientUserId;
                var timestamp = comment.ThreadTimestamp;

           _context.Comments.Remove(comment);
         await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Thread), new { patientId, timestamp });
}

      return RedirectToAction(nameof(Index));
        }

      private bool CommentExists(int id)
        {
return _context.Comments.Any(e => e.CommentId == id);
}
    }
}
