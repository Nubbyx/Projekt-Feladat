using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    /// <summary>
    /// A mappák CRUD műveletei. Útvonal: api/folders.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly AppDbContext db;

        public FoldersController(AppDbContext db)
        {
            this.db = db;
        }

        // GET api/folders
        // Az összes mappa a benne lévő jegyzetekkel együtt (Include = JOIN a Notes táblával).
        // Megjegyzés: a Note.Folder visszahivatkozás miatt körkörös objektumgráf keletkezik; ezt a
        // Program.cs-ben beállított ReferenceHandler.IgnoreCycles kezeli a JSON szerializálásnál.
        [HttpGet]
        public async Task<IActionResult> GetAllFolders()
        {
            var folders = await db.Folders.AsNoTracking().Include(f => f.Notes).ToListAsync();
            return Ok(folders);
        }

        // POST api/folders
        // Új mappa létrehozása. Szándékosan új Folder objektumot építünk, és csak a nevet másoljuk át,
        // így a kliens nem tudja előre megadni az azonosítót vagy a jegyzetek listáját (túlküldés elleni védelem).
        [HttpPost]
        public async Task<IActionResult> CreateFolders(Folder folder)
        {
            var newFolder = new Folder { FolderName = folder.FolderName };

            db.Folders.Add(newFolder);
            await db.SaveChangesAsync(); // mentés után az adatbázis által generált FolderId már ki van töltve

            return Ok(new
            {
                message = "Mappa sikeresen létrehozva!",
                newFolder.FolderId,
                newFolder.FolderName
            });
        }

        // PUT api/folders/{id}
        // Mappa átnevezése. Csak a nevet írjuk felül, a mappa jegyzeteihez nem nyúlunk.
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFolder(int id, [FromBody] Folder updatedFolder)
        {
            var existingFolder = await db.Folders.FindAsync(id);

            if (existingFolder == null)
            {
                return NotFound(new { message = "A mappa nem található." });
            }

            existingFolder.FolderName = updatedFolder.FolderName;
            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mappa sikeresen frissítve.",
                existingFolder.FolderId,
                existingFolder.FolderName
            });
        }

        // DELETE api/folders/{FolderId}
        // Mappa törlése a benne lévő jegyzetekkel együtt.
        // A jegyzeteket kézzel kell törölni, mert a Notes -> Folders idegen kulcs nem kaszkádos
        // (a migráció szerint nincs onDelete: Cascade), így a mappa törlése önmagában hibát adna.
        // A jegyzetek csatolmányai viszont az adatbázisban kaszkádosan törlődnek (Attachments -> Notes: Cascade).
        [HttpDelete("{FolderId}")]
        public async Task<IActionResult> DeleteFolder(int FolderId)
        {
            var folder = await db.Folders.Include(f => f.Notes).FirstOrDefaultAsync(f => f.FolderId == FolderId);
            if (folder == null)
            {
                return NotFound("A mappa nem található.");
            }

            db.Notes.RemoveRange(folder.Notes);
            db.Folders.Remove(folder);
            await db.SaveChangesAsync(); // egyetlen tranzakcióban törlődik a jegyzet és a mappa is

            return Ok(new
            {
                message = "Mappa sikeresen törölve!",
                folder.FolderName
            });
        }

        // GET api/folders/count
        // Saját függvény
        // Csoda, ha jövőre nem bukok meg a vizsgán #2 - Örs
        [HttpGet("count")]
        public async Task<IActionResult> CountFolders()
        {
            var count = await db.Folders.CountAsync();

            return Ok(count);
        }
    }
}
