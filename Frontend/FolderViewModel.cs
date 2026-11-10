using Models;

namespace Frontend
{
    /// <summary>
    /// Egy mappa megjelenítési modellje a bal oldali fában. Az API-tól kapott Folder-t
    /// csomagolja be kiegészítő, csak a felületet érintő állapotokkal (lenyitott-e, éppen átnevezik-e).
    /// </summary>
    public class FolderViewModel : ViewModelBase
    {
        public int FolderId { get; set; }

        // A mappában lévő jegyzetek nézetmodelljei. Sima List, mert a tartalmát a LoadDataAsync
        // egyszerre tölti fel, és a fa minden betöltéskor teljesen újraépül (nincs szükség
        // ObservableCollection-re).
        public List<NoteViewModel> Notes { get; set; } = new();

        // Az eredeti modell. Átnevezésnél ebből olvassuk a régi nevet (Escape / sikertelen mentés
        // esetén ide állunk vissza), és ennek az értékét frissítjük sikeres mentés előtt.
        public Folder? OriginalFolder { get; set; }

        // A név property-ként van megírva, hogy módosításkor a felület azonnal frissüljön.
        private string _folderName = string.Empty;
        public string FolderName
        {
            get => _folderName;
            set => SetField(ref _folderName, value);
        }

        // Le van-e nyitva a mappa a fában. Alapból nyitott; a LoadDataAsync megőrzi a korábbi állapotot.
        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetField(ref _isExpanded, value);
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
