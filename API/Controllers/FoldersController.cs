using API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly AppDbContext db;

        public FoldersController(AppDbContext db)
        {
            this.db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFolders()
        {
            var folders = await db.Folders.AsNoTracking().Include(f => f.Notes).ToListAsync();
            return Ok(folders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolders(Folder folder)
        {
            // Csak a nevet vesszük át: a kliens ne tudjon jegyzeteket "becsempészni" a mappával.
            var newFolder = new Folder { FolderName = folder.FolderName };

            db.Folders.Add(newFolder);
            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mappa sikeresen létrehozva!",
                newFolder.FolderId,
                newFolder.FolderName
            });
        }

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

        [HttpDelete("{FolderId}")]
        public async Task<IActionResult> DeleteFolder(int FolderId)
        {
            var folder = await db.Folders.Include(f => f.Notes).FirstOrDefaultAsync(f => f.FolderId == FolderId);
            if (folder == null)
            {
                return NotFound("A mappa nem található.");
            }

            // A jegyzetek csatolmányait az adatbázis törli (cascade).
            db.Notes.RemoveRange(folder.Notes);
            db.Folders.Remove(folder);
            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mappa sikeresen törölve!",
                folder.FolderName
            });
        }
    }
}
