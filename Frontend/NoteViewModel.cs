using Models;

namespace Frontend
{
    /// <summary>
    /// Egy jegyzet megjelenítési modellje a bal oldali fában. Az API-tól kapott Note adatait
    /// és a felület állapotát (pl. átnevezés folyamatban) tartja egyben.
    /// A szerkesztő nem ezt, hanem az OriginalNote-ot (az "élő" Note-ot) szerkeszti.
    /// </summary>
    public class NoteViewModel : ViewModelBase
    {
        public int NoteId { get; set; }

        // Csak a betöltéskori tartalom másolata; a valódi, szerkesztett tartalom a Note objektumban van.
        // Nem property-értesítős, mert a fa nem jeleníti meg.
        public string Content { get; set; } = string.Empty;

        // Nullable: a jegyzet mappa nélkül is létezhet az adatmodell szerint.
        public int? FolderId { get; set; }

        // Az eredeti modellpéldány. Átnevezéskor ebből olvassuk a régi címet (visszaállításhoz),
        // és a csatolmányműveletek után erre állítjuk át a frissen betöltött példányt.
        public Note? OriginalNote { get; set; }

        // A cím property-ként van megírva, hogy a fában átnevezéskor a felület azonnal frissüljön.
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        // Átnevezés közben true: a címke helyett szerkesztő TextBox jelenik meg a XAML-ben.
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetField(ref _isEditing, value);
        }
    }
}
