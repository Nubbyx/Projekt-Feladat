using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : Controller
    {
        public AppDbContext db;

        public NotesController()
        {
            db = new AppDbContext();
        }

        [HttpGet]
        public IActionResult GetNotes()
        {
            return Ok(db.Notes.Include(n => n.Attachments).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote(Note note)
        {
            db.Notes.Add(note);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Jegyzet sikeresen létrehozva!",
                note.NoteId,
                note.Title
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] Note updatedNote)
        {
            var existingNote = await db.Notes.FindAsync(id);

            if (existingNote == null)
            {
                return NotFound(new { message = "A jegyzet nem található." });
            }

            existingNote.Title = updatedNote.Title;
            existingNote.Content = updatedNote.Content;
            existingNote.FolderId = updatedNote.FolderId;

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Jegyzet sikeresen frissítve.",
                existingNote.NoteId,
                existingNote.Title
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var note = await db.Notes.FindAsync(id);

            if (note == null)
            {
                return NotFound(new { message = "A jegyzet nem található." });
            }

            db.Notes.Remove(note);
            await db.SaveChangesAsync();

            return Ok(new { message = "Jegyzet sikeresen törölve." });
        }
    }
}
