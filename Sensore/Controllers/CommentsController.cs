using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sensore.Data;
using Sensore.Models;

namespace Sensore.Controllers
{
    // Controller for managing comments and threaded discussions.
    // Provides CRUD operations for comments and thread viewing.
    // Requires authentication for all actions.
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

        // ========================================================================
        // LIST AND VIEW COMMENTS
        // ========================================================================

        // Lists comments with optional filtering by patient and timestamp.
        // param: patientId - Filter by patient (optional)
        // param: threadTimestamp - Filter by thread timestamp (optional)
        public async Task<IActionResult> Index(string? patientId, DateTime? threadTimestamp)
        {
            var query = _context.Comments
                .Include(c => c.AuthorUser)
                .Include(c => c.PatientUser)
                .Include(c => c.ParentComment)
                .AsQueryable();

            // Apply patient filter if specified
            if (!string.IsNullOrEmpty(patientId))
            {
                query = query.Where(c => c.PatientUserId == patientId);
            }

            // Apply timestamp filter if specified
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

        // Displays a threaded conversation for a specific patient and timestamp.
     // Shows all comments and replies in chronological order.
        // param: patientId - The patient the thread belongs to
        // param: timestamp - The thread's timestamp
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

            // Get all comments in this thread (top-level and replies)
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

        // Displays details for a single comment including its replies.
        // param: id - The comment ID
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

          // Get replies to this comment
        var replies = await _context.Comments
    .Include(r => r.AuthorUser)
   .Where(r => r.ParentCommentId == id)
            .OrderBy(r => r.CreatedAt)
         .ToListAsync();

            ViewBag.Replies = replies;

      return View(comment);
        }

        // ========================================================================
        // CREATE COMMENT
   // ========================================================================

        // Displays the form for creating a new comment.
    // param: patientId - The patient to comment about
        // param: threadTimestamp - The thread timestamp
        // param: parentCommentId - Optional parent for replies
        public IActionResult Create(string patientId, DateTime? threadTimestamp, int? parentCommentId)
      {
    if (string.IsNullOrEmpty(patientId) || !threadTimestamp.HasValue)
    {
         return BadRequest("Patient ID and thread timestamp are required");
 }

     // Pre-populate comment with context
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

        // Creates a new comment or reply.
        // Automatically sets the author to the current user.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comment comment)
      {
            // Set the current user as the author
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

        // ========================================================================
        // EDIT COMMENT
        // ========================================================================

   // Displays the form for editing a comment.
     // Only the author or an Admin can edit.
        // param: id - The comment ID to edit
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

     // Authorization: only author or admin can edit
    var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null || (comment.AuthorUserId != currentUser.Id && !User.IsInRole("Admin")))
          {
     return Forbid();
    }

            return View(comment);
        }

        // Updates an existing comment.
        // Only the author or an Admin can edit.
   [HttpPost]
        [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Comment comment)
        {
        if (id != comment.CommentId)
    {
       return NotFound();
     }

// Authorization: only author or admin can edit
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

        // ========================================================================
        // DELETE COMMENT
        // ========================================================================

        // Deletes a comment from the system.
        // Only the author or an Admin can delete.
        // param: id - The comment ID to delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
         var comment = await _context.Comments.FindAsync(id);
 if (comment != null)
            {
     // Authorization: only author or admin can delete
     var currentUser = await _userManager.GetUserAsync(User);
     if (currentUser == null || (comment.AuthorUserId != currentUser.Id && !User.IsInRole("Admin")))
   {
      return Forbid();
     }

    // Store values for redirect before deletion
  var patientId = comment.PatientUserId;
 var timestamp = comment.ThreadTimestamp;

  _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Thread), new { patientId, timestamp });
    }

 return RedirectToAction(nameof(Index));
        }

    // Checks if a comment exists in the database.
     // param: id - The comment ID to check
        private bool CommentExists(int id)
 {
            return _context.Comments.Any(e => e.CommentId == id);
    }
    }
}
