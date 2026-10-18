using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentsController : Controller
    {
        public AppDbContext db;

        public AttachmentsController()
        {
            db = new AppDbContext();
        }

        [HttpGet]
        public IActionResult GetAttachments()
        {
            return Ok(db.Attachments);
        }

        [HttpPost("{noteId}")]
        public async Task<IActionResult> UploadAttachment(int noteId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var note = await db.Notes.Include(n => n.Attachments).FirstOrDefaultAsync(n => n.NoteId == noteId);

            if (note == null)
            {
                return NotFound("Note not found.");
            }

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            var attachment = new Attachment
            {
                Name = file.FileName,
                File = fileBytes,
                NoteId = note.NoteId
            };

            note.Attachments.Add(attachment);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Attachment uploaded successfully.",
                attachment.AttachmentId,
                attachment.Name
            });
        }
    }
}