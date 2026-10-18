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
                message = "Attachment created successfully.",
                note.NoteId,
                note.Title
            });
        }
    }
}
