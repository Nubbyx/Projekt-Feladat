using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    /// <summary>
    /// A jegyzetek CRUD műveletei. Útvonal: api/notes.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly AppDbContext db;

        public NotesController(AppDbContext db)
        {
            this.db = db;
        }

        // GET api/notes
        // Az összes jegyzet a csatolmányok metaadataival (név, azonosító), de a fájlok tartalma nélkül.
        // Két lépésben dolgozunk: előbb anonim típusra vetítünk (így az EF nem húzza le a nagy longblob
        // oszlopot), aztán memóriában építjük fel a valódi Note/Attachment objektumokat, mert
        // LINQ-to-SQL lekérdezésben nem lehet közvetlenül "new Note { ... }"-t vetíteni ilyen beágyazott módon.
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

        // POST api/notes
        // Új jegyzet létrehozása. A FolderId nullable, így mappa nélküli jegyzet is lehet,
        // de ha meg van adva, annak létező mappára kell mutatnia.
        [HttpPost]
        public async Task<IActionResult> CreateNote(Note note)
        {
            if (note.FolderId != null && !await db.Folders.AnyAsync(f => f.FolderId == note.FolderId))
            {
                return BadRequest(new { message = "A megadott mappa nem létezik." });
            }

            // Csak a szükséges mezőket másoljuk át (az azonosítót az adatbázis generálja,
            // a csatolmányokat pedig külön végponton keresztül lehet hozzáadni).
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

        // PUT api/notes/{id}
        // Jegyzet teljes frissítése (cím, tartalom, mappa). A kliens ezt hívja átnevezéskor és
        // az ablak bezárásakor is. A csatolmányokat itt nem módosítjuk.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] Note updatedNote)
        {
            var existingNote = await db.Notes.FindAsync(id);

            if (existingNote == null)
            {
                return NotFound(new { message = "A jegyzet nem található." });
            }

            // Áthelyezéskor (FolderId változik) is ellenőrizzük, hogy a célmappa létezik.
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

        // DELETE api/notes/{id}
        // A jegyzet csatolmányai az adatbázisban kaszkádosan törlődnek (Attachments -> Notes: Cascade),
        // ezért itt nem kell őket külön eltávolítani.
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

        // GET api/notes/count
        // Saját függvény #1
        // Csoda, ha jövőre nem bukok meg a vizsgán #3 - Örs
        [HttpGet("count")]
        public async Task<IActionResult> CountNotes()
        {
            var count = await db.Notes.CountAsync();

            return Ok(count);
        }

        // GET api/notes/{id}/count
        // Saját függvény #2
        // Csoda, ha jövőre nem bukok meg a vizsgán #4 - Örs
        [HttpGet("{id:int}/count")]
        public async Task<IActionResult> CountNoteAttachments(int id)
        {
            if (!await db.Notes.AnyAsync(n => n.NoteId == id))
            {
                return NotFound(new { message = "A jegyzet nem található." });
            }

            var count = await db.Attachments.CountAsync(a => a.NoteId == id);

            return Ok(count);
        }
    }
}
