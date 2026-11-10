using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Models
{
    /// <summary>
    /// Egy jegyzet. Egyszerre adatbázis-entitás (EF Core) és a kliens által is használt modell;
    /// az INotifyPropertyChanged azért van, hogy a szerkesztőben a módosítások azonnal
    /// látszódjanak a kötött felületi elemeken.
    /// </summary>
    public class Note : INotifyPropertyChanged
    {
        // Elsődleges kulcs; az adatbázis generálja.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NoteId { get; set; }

        // A jegyzet címe: kötelező, 1–50 karakter (varchar(50)).
        [Required, StringLength(50, MinimumLength = 1)]
        public string? Title { get; set; }

        // A jegyzet szövege (longtext). Az AllowEmptyStrings = true miatt az üres szöveg is érvényes,
        // csak a null nem; így egy frissen létrehozott, még üres jegyzet is elmenthető.
        [Required(AllowEmptyStrings = true)]
        public string? Content { get; set; }

        // Idegen kulcs a mappához. Nullable, vagyis a jegyzet mappa nélkül is létezhet.
        // A Notes -> Folders kapcsolat nem kaszkádos, ezért mappatörléskor a jegyzeteket
        // a kontrollernek kell előbb kitörölnie.
        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }

        // A jegyzet csatolmányai (1-N). A jegyzet törlésekor ezek kaszkádosan törlődnek.
        public List<Attachment> Attachments { get; set; } = new();

        // Csak a felületnek szükséges állapot (átnevezés folyamatban), ezért [NotMapped]:
        // az EF nem hozza létre hozzá adatbázis-oszlopot.
        private bool _isEditing;
        [NotMapped]
        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString() => Title!;
    }
}
