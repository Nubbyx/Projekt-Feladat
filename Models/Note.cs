using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Note
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NoteId { get; set; } // PRIMARY KEY  AUTO_INCREMENT

        [Required, StringLength(50, MinimumLength = 1)]
        public string? Title { get; set; } // VARCHAR(50)

        [Required]
        public string? Content { get; set; } // MEDIUMTEXT

        public int? FolderId { get; set; } // Nullable: egy jegyzet nem kötelezően van mappában
        public Folder? Folder { get; set; }

        public List<Attachment> Attachments { get; set; } = new();


        public override string ToString() => Title!;
    }
}