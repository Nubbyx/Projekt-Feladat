using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Models;
using API.Data;

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
        public IActionResult GetFolders()
        {
            //var folders = db.Notes.Select(n => n.Folder).Distinct().ToList();
            return Ok(db.Folders
                .OrderBy(f => f.FolderName)
                .Select(f => new { f.FolderId, f.FolderName, NoteCount = f.Notes.Count() })
                .ToList());
        }

        [HttpPost("{folderId}")]
        public IActionResult CreateFolder(int folderId,Folder folder)
        {
            if (folder == null || string.IsNullOrWhiteSpace(folder.FolderName))
            {
                return BadRequest("Folder name is required.");
            }
            db.Folders.Add(folder);
            db.SaveChanges();
            return Ok(new
            {
                message = "Folder created successfully.",
                folder.FolderId,
                folder.FolderName
            });
        }


        


    }
}
