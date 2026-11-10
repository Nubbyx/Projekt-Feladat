using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Models
{
    public class Attachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttachmentId { get; set; } // PRIMARY KEY  AUTO_INCREMENT

        [Required, StringLength(50, MinimumLength = 1)]
        public string? Name { get; set; } // VARCHAR(50)

        // A bináris tartalmat sosem küldjük JSON-ban (listázáskor és jegyzetmentéskor is felesleges
        // lenne); letölteni a GET api/attachments/{id} végponttal lehet.
        [JsonIgnore]
        [Required]
        public byte[] File { get; set; } = Array.Empty<byte>(); // LONGBLOB

        [Required]
        public int NoteId { get; set; }


        public override string ToString() => Name!;
    }
}