using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Models
{
    /// <summary>
    /// Egy jegyzethez csatolt fájl. A fájl tartalma közvetlenül az adatbázisban van tárolva (longblob).
    /// A Models projekt az API-ban és a kliensben is használva van, így a két oldal ugyanazt az osztályt látja.
    /// </summary>
    public class Attachment
    {
        // Elsődleges kulcs; az adatbázis generálja (AUTO_INCREMENT), ezért nem kell megadni létrehozáskor.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AttachmentId { get; set; } // PRIMARY KEY  AUTO_INCREMENT

        // A fájlnév (kiterjesztéssel). Az 50 karakteres korlát a varchar(50) oszlopból és a
        // kliens/API oldali ellenőrzésből jön, a három helyen egyeznie kell.
        [Required, StringLength(50, MinimumLength = 1)]
        public string? Name { get; set; } // VARCHAR(50)

        // A bináris tartalmat sosem küldjük JSON-ban (listázáskor és jegyzetmentéskor is felesleges
        // lenne); letölteni a GET api/attachments/{id} végponttal lehet.
        [JsonIgnore]
        [Required]
        public byte[] File { get; set; } = Array.Empty<byte>(); // LONGBLOB

        // Idegen kulcs: melyik jegyzethez tartozik. A jegyzet törlésekor a csatolmány kaszkádosan törlődik.
        [Required]
        public int NoteId { get; set; }


        // A ListBox/ComboBox a ToString()-et használja megjelenítéshez, ha nincs külön sablon.
        public override string ToString() => Name!;
    }
}
