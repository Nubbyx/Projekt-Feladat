using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext db;

        public NotesController(AppDbContext db)
        {
            this.db = db;
        }

        // A csatolmányok fájltartalmát nem töltjük be (csak az azonosítót és a nevet),
        // különben minden listázás az összes feltöltött fájlt áthúzná az adatbázisból.
        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            var rows = await db.Notes
                .AsNoTracking()
                .Select(n => new
                {
                    n.NoteId,
                    n.Title,
                    n.Content,
                    n.FolderId,
                    Attachments = n.Attachments
                        .Select(a => new { a.AttachmentId, a.Name, a.NoteId })
                        .ToList()
                })
                .ToListAsync();

            var notes = rows.Select(r => new Note
            {
                NoteId = r.NoteId,
                Title = r.Title,
                Content = r.Content,
                FolderId = r.FolderId,
                Attachments = r.Attachments
                    .Select(a => new Attachment { AttachmentId = a.AttachmentId, Name = a.Name, NoteId = a.NoteId })
                    .ToList()
            }).ToList();

            return Ok(notes);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote(Note note)
        {
            if (note.FolderId != null && !await db.Folders.AnyAsync(f => f.FolderId == note.FolderId))
            {
                return BadRequest(new { message = "A megadott mappa nem létezik." });
            }

            // Csak a saját mezőit vesszük át (se mappa-objektumot, se csatolmányt nem hozhat létre a kliens).
            var newNote = new Note
            {
                Title = note.Title,
                Content = note.Content,
                FolderId = note.FolderId
            };

            db.Notes.Add(newNote);
            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Jegyzet sikeresen létrehozva!",
                newNote.NoteId,
                newNote.Title
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

            if (updatedNote.FolderId != null && !await db.Folders.AnyAsync(f => f.FolderId == updatedNote.FolderId))
            {
                return BadRequest(new { message = "A megadott mappa nem létezik." });
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
