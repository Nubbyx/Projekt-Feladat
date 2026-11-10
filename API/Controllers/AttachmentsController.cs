using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    /// <summary>
    /// A jegyzetekhez tartozó csatolmányok kezelése (listázás, letöltés, feltöltés, törlés).
    /// Útvonal: api/attachments (az [controller] a osztály nevéből, a "Controller" végződés nélkül képződik).
    /// </summary>
    [ApiController] // automatikus modellvalidáció (400-as válasz érvénytelen kérésnél) és paraméterkötés
    [Route("api/[controller]")]
    public class AttachmentsController : ControllerBase
    {
        // Az AppDbContext-et a DI konténer adja át (Program.cs-ben van regisztrálva).
        private readonly AppDbContext db;

        public AttachmentsController(AppDbContext db)
        {
            this.db = db;
        }

        // GET api/attachments
        // Az összes csatolmány listája, DE a fájl tartalma nélkül. A Select anonim típusra vetít,
        // így az EF a longblob oszlopot le sem kéri az adatbázisból (nagy fájloknál ez sok memóriát és időt spórol).
        [HttpGet]
        public async Task<IActionResult> GetAttachments()
        {
            var attachments = await db.Attachments
                .AsNoTracking() // csak olvasunk, nincs szükség változáskövetésre
                .Select(a => new { a.AttachmentId, a.Name, a.NoteId })
                .ToListAsync();

            return Ok(attachments);
        }

        // GET api/attachments/{id}
        // Egy csatolmány letöltése. Itt kérjük le a teljes rekordot, mert itt kell a fájl tartalma.
        // A {id:int} megszorítás miatt csak számot tartalmazó útvonal illeszkedik.
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

            // Általános "application/octet-stream" típus: a kliens a fájlnév alapján dönti el, mit kezd vele.
            return File(attachment.File, "application/octet-stream", attachment.Name);
        }

        // POST api/attachments/{noteId}
        // Fájl feltöltése egy jegyzethez multipart/form-data kéréssel.
        // A paraméter neve ("file") egyezzen a kliens által a form-ba tett mezőnévvel.
        [HttpPost("{noteId:int}")]
        public async Task<IActionResult> UploadAttachment(int noteId, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nincs file csatolva.");
            }

            // Path.GetFileName levágja az esetleges útvonalat a fájlnév elől (pl. "C:\..\x.txt" -> "x.txt"),
            // így nem kerülhet útvonal-részlet az adatbázisba. A hossz az adatbázis varchar(50) oszlopa miatt korlátozott.
            var fileName = Path.GetFileName(file.FileName);
            if (fileName.Length is < 1 or > 50)
            {
                return BadRequest("A fájlnév 1 és 50 karakter közötti lehet.");
            }

            // Nem létező jegyzethez nem lehet csatolmányt adni (idegen kulcs hiba helyett érthető 404-et adunk).
            if (!await db.Notes.AnyAsync(n => n.NoteId == noteId))
            {
                return NotFound("A jegyzet nem található.");
            }

            // A feltöltött stream tartalmát bájttömbbé másoljuk, mert így tároljuk az adatbázisban (longblob).
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

        // DELETE api/attachments/{id}
        // FindAsync az elsődleges kulcs alapján keres, és előbb a változáskövető cache-ben néz körül, ezért gyors.
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
