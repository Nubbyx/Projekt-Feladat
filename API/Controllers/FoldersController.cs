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
        public AppDbContext db;

        public FoldersController()
        {
            db = new AppDbContext();
        }

        [HttpGet]
        public IActionResult GetAllFolders()
        {
            var folders = db.Folders.Include(f => f.Notes).ToList();
            return Ok(folders);
        }

        [HttpPost]
        public IActionResult CreateFolders(Folder folder)
        {
            db.Folders.Add(folder);
            db.SaveChanges();
            return Ok(new
            {
                message = "Mappa sikeresen létrehozva!",
                folder.FolderId,
                folder.FolderName
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
        public IActionResult DeleteFolder(int FolderId)
        {
            var folder = db.Folders.Include(f => f.Notes).FirstOrDefault(f => f.FolderId == FolderId);
            if (folder == null)
            {
                return NotFound("A mappa nem található.");
            }

            db.Notes.RemoveRange(folder.Notes);
            db.Folders.Remove(folder);
            db.SaveChanges();
            return Ok(new
            {
                message = "Mappa sikeresen törölve!",
                folder.FolderName
            });
        }
    }
}