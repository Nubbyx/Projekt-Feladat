using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Attachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttachmentId { get; set; } // PRIMARY KEY  AUTO_INCREMENT

        [Required, StringLength(50, MinimumLength = 1)]
        public string? Name { get; set; } // VARCHAR(50)

        [Required]
        public byte[] File { get; set; } = Array.Empty<byte>(); // LONGBLOB

        [Required]
        public int NoteId { get; set; }


        public override string ToString() => Name!;
    }
}