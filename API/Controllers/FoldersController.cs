using API.Data;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("{FolderId}")]
        public IActionResult GetFolders(int FolderId)
        {
            var folder = db.Folders.FirstOrDefault(f => f.FolderId == FolderId);
            if (folder == null)
            {
                return NotFound("Folder not found.");
            }
            return Ok(folder);
        }

        [HttpPost]
        public IActionResult CreateFolders(Folder folder)
        {
            db.Folders.Add(folder);
            db.SaveChanges();
            return Ok(new
            {
                message = "Folder created successfully.",
                folder.FolderName
            });
        }

        [HttpDelete("{FolderId}")]
        public IActionResult DeleteFolder(int FolderId)
        {
            var folder = db.Folders.FirstOrDefault(f => f.FolderId == FolderId);
            if (folder == null)
            {
                return NotFound("Folder not found.");
            }
            db.Folders.Remove(folder);
            db.SaveChanges();
            return Ok(new
            {
                message = "Folder deleted successfully.",
                folder.FolderName
            });
        }

        [HttpGet("{FolderId}/notes")]
        public IActionResult GetNotesFromFolder(int FolderId)
        {
            var folder = db.Folders.FirstOrDefault(f => f.FolderId == FolderId);
            if (folder == null)
            {
                return NotFound("Folder not found.");
            }
            var notes = db.Notes.Where(n => n.FolderId == FolderId).ToList();
            return Ok(notes);
        }







    }
}
