using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Models
{
    /// <summary>
    /// Egy mappa, amelyben jegyzetek vannak. Egyszerre adatbázis-entitás (EF Core) és
    /// a kliens által is használt modell; az INotifyPropertyChanged a WPF-es adatkötéshez kell.
    /// </summary>
    public class Folder : INotifyPropertyChanged
    {
        // Elsődleges kulcs; az adatbázis generálja.
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int FolderId { get; set; }

        // A mappa neve: kötelező, 1–50 karakter (varchar(50)). Az API a modellvalidáció révén
        // ezt automatikusan ellenőrzi; a kliens külön is ellenőrzi átnevezés előtt.
        [Required, StringLength(50, MinimumLength = 1)]
        public string? FolderName { get; set; }

        // Navigációs tulajdonság: a mappa jegyzetei (1-N kapcsolat). Az idegen kulcs a Note.FolderId.
        public List<Note> Notes { get; set; } = new List<Note>();

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

        public override string ToString() => FolderName!;
    }
}
