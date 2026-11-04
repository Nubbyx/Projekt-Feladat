using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Folder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FolderId { get; set; }

        [Required, StringLength(50, MinimumLength =1)]
        public string? FolderName { get; set; }

        public List<Note> Notes { get; set; } = new List<Note>();

    }
}
