using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentsController : ControllerBase
    {
        private readonly AppDbContext db;

        public AttachmentsController(AppDbContext db)
        {
            this.db = db;
        }

        // A fájl tartalma nélkül listáz (azt a letöltő végpont adja vissza).
        [HttpGet]
        public async Task<IActionResult> GetAttachments()
        {
            var attachments = await db.Attachments
                .AsNoTracking()
                .Select(a => new { a.AttachmentId, a.Name, a.NoteId })
                .ToListAsync();

            return Ok(attachments);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> DownloadAttachment(int id)
        {
            var attachment = await db.Attachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AttachmentId == id);

            if (attachment == null)
            {
                return NotFound("A csatolmány nem található.");
            }

            return File(attachment.File, "application/octet-stream", attachment.Name);
        }

        [HttpPost("{noteId:int}")]
        public async Task<IActionResult> UploadAttachment(int noteId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nincs file csatolva.");
            }

            var fileName = Path.GetFileName(file.FileName);
            if (fileName.Length is < 1 or > 50)
            {
                return BadRequest("A fájlnév 1 és 50 karakter közötti lehet.");
            }

            if (!await db.Notes.AnyAsync(n => n.NoteId == noteId))
            {
                return NotFound("A jegyzet nem található.");
            }

            byte[] fileBytes;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            var attachment = new Attachment
            {
                Name = fileName,
                File = fileBytes,
                NoteId = noteId
            };

            db.Attachments.Add(attachment);
            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Csatolmány sikeresen létrehozva!",
                attachment.AttachmentId,
                attachment.Name
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteAttachment(int id)
        {
            var attachment = await db.Attachments.FindAsync(id);

            if (attachment == null)
            {
                return NotFound(new { message = "A csatolmány nem található." });
            }

            db.Attachments.Remove(attachment);
            await db.SaveChangesAsync();

            return Ok(new { message = "Csatolmány sikeresen törölve." });
        }
    }
}
