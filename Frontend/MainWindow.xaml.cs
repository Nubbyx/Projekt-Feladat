using Models;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Frontend
{
    /// <summary>
    /// A jegyzetkezelő fő ablaka. Bal oldalon a mappák/jegyzetek fája (sp_Files),
    /// jobb oldalon a kiválasztott jegyzet szerkesztője (CurrentNote-ra kötve).
    /// Minden adatműveletet a háttérben futó REST API-n keresztül végez.
    /// Az INotifyPropertyChanged azért kell, hogy a XAML-ben a CurrentNote-ra
    /// kötött elemek frissüljenek, ha az értéke változik.
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Egyetlen HttpClient az egész ablaknak (újrahasználható, nem kell minden híváshoz újat létrehozni).
        // A port az API launchSettings-ében beállított HTTPS porttal egyezzen meg.
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("https://localhost:7226") };

        // Gyorsítótár azokról a jegyzetekről, amelyeket a felhasználó megnyitott a munkamenet alatt.
        // Fontos: a szerkesztő ezeket az objektumokat módosítja, ezért itt vannak a legfrissebb
        // (még el nem mentett) tartalmak. Bezáráskor ebből mentünk vissza mindent az API-ba.
        // Kulcs: NoteId.
        private readonly Dictionary<int, Note> _openedNotes = new();

        // Visszaadja a jegyzet "élő" példányát: ha már megnyitották, a gyorsítótárból (benne a
        // nem mentett módosításokkal), különben a listabetöltéskor kapott eredeti példányt.
        private Note GetLiveNote(NoteViewModel nvm) =>
            _openedNotes.TryGetValue(nvm.NoteId, out var n) ? n : nvm.OriginalNote!;

        // A jelenleg szerkesztett jegyzet. A property-n keresztül állítjuk, hogy a UI értesítést kapjon.
        private Note? currentNote;
        public Note? CurrentNote
        {
            get => currentNote;
            set
            {
                currentNote = value;
                OnPropertyChanged(); // a [CallerMemberName] miatt automatikusan "CurrentNote" a név
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null!)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // a XAML-beli kötések (pl. CurrentNote.Content) erre az ablakra mutatnak
        }

        // Az ablak betöltése után kérjük le az adatokat (async void, mert eseménykezelő).
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        /// <summary>
        /// Lekéri az összes mappát és jegyzetet az API-tól, majd felépíti belőlük a fa nézetmodelljét.
        /// Minden módosítás (létrehozás, törlés) után ezt hívjuk újra, így a UI mindig a szerver állapotát mutatja.
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                var folders = await _http.GetFromJsonAsync<List<Folder>>("api/folders") ?? new();
                var notes = await _http.GetFromJsonAsync<List<Note>>("api/notes") ?? new();

                // Újratöltés előtt megjegyezzük, melyik mappa volt lenyitva, hogy a felhasználó
                // ne veszítse el a fa állapotát minden frissítésnél.
                var expandedStates = (sp_Files.ItemsSource as IEnumerable<FolderViewModel>)?
                    .ToDictionary(x => x.FolderId, x => x.IsExpanded) ?? new Dictionary<int, bool>();

                var folderViewModels = new List<FolderViewModel>();

                foreach (var f in folders)
                {
                    var fvm = new FolderViewModel
                    {
                        FolderId = f.FolderId,
                        FolderName = f.FolderName ?? "Névtelen mappa",
                        OriginalFolder = f,
                        // Új mappa (nincs mentett állapot) alapból nyitva van; a már ismert mappa megtartja az előző állapotát.
                        IsExpanded = !expandedStates.TryGetValue(f.FolderId, out var wasExpanded) || wasExpanded
                    };

                    // Az adott mappához tartozó jegyzetek szűrése (egy lekérdezés az összes jegyzetre, kliensoldali csoportosítással).
                    var folderNotes = notes.Where(n => n.FolderId == f.FolderId).ToList();
                    foreach (var n in folderNotes)
                    {
                        fvm.Notes.Add(new NoteViewModel
                        {
                            NoteId = n.NoteId,
                            Title = n.Title ?? "Névtelen jegyzet",
                            Content = n.Content ?? "",
                            FolderId = n.FolderId,
                            OriginalNote = n // az eredeti modell, amit mentéskor/átnevezéskor használunk
                        });
                    }
                    folderViewModels.Add(fvm);
                }

                sp_Files.ItemsSource = folderViewModels;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------- Létrehozás ----------

        // Új, "Új mappa" nevű mappa létrehozása; siker esetén újratöltjük a fát.
        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var newFolder = new Folder { FolderName = "Új mappa" };
                var response = await _http.PostAsJsonAsync("api/folders", newFolder);
                if (response.IsSuccessStatusCode) await LoadDataAsync();
                else ShowError("A mappa létrehozása nem sikerült.");
            }
            catch (HttpRequestException ex)
            {
                // Hálózati hiba (pl. nem fut az API)
                ShowError($"A mappa létrehozása nem sikerült: {ex.Message}");
            }
        }

        // Új jegyzet létrehozása a jobb gombbal kiválasztott mappában.
        // A mappát a menüelem DataContext-je adja (a ContextMenu a FolderViewModel-hez tartozik).
        private async void AddNoteToFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is FolderViewModel fvm)
            {
                try
                {
                    var newNote = new Note { Title = "Új jegyzet", Content = "", FolderId = fvm.FolderId };
                    var response = await _http.PostAsJsonAsync("api/notes", newNote);
                    if (response.IsSuccessStatusCode) await LoadDataAsync();
                    else ShowError("A jegyzet létrehozása nem sikerült.");
                }
                catch (HttpRequestException ex)
                {
                    ShowError($"A jegyzet létrehozása nem sikerült: {ex.Message}");
                }
            }
        }

        // Jegyzet megnyitása a szerkesztőben a fában rákattintáskor.
        private void NoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is NoteViewModel nvm && nvm.OriginalNote != null)
            {
                // Az élő példányt használjuk, így a korábban szerkesztett, még nem mentett
                // tartalom nem vész el, ha a felhasználó másik jegyzetre vált és visszatér.
                var live = GetLiveNote(nvm);
                _openedNotes[live.NoteId] = live; // innentől bezáráskor ez is mentésre kerül
                CurrentNote = live;
            }
        }

        // Egységes hibaüzenet-ablak, hogy ne kelljen mindenhol megismételni a paramétereket.
        private static void ShowError(string message) =>
            MessageBox.Show(message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);

        // ---------- Törlés ----------

        // Mappa törlése megerősítéssel. A mappával együtt a benne lévő jegyzetek is törlődnek.
        private async void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is FolderViewModel fvm)
            {
                var res = MessageBox.Show($"Biztosan törli a(z) {fvm.FolderName} mappát és a benne lévő jegyzeteket?", "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _http.DeleteAsync($"api/folders/{fvm.FolderId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            ShowError("A mappa törlése nem sikerült.");
                            return; // a szerveren nem törlődött, a helyi állapothoz nem nyúlunk
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        ShowError($"A mappa törlése nem sikerült: {ex.Message}");
                        return;
                    }

                    // Csak sikeres szerveroldali törlés után takarítjuk a helyi állapotot:
                    // a törölt jegyzeteket ki kell venni a gyorsítótárból, különben bezáráskor
                    // megpróbálnánk elmenteni őket (404-es hibát okozva).
                    var deletedIds = fvm.Notes.Select(n => n.NoteId).ToHashSet();
                    foreach (var id in deletedIds) _openedNotes.Remove(id);
                    // Ha éppen a törölt mappa egyik jegyzete van megnyitva, ürítjük a szerkesztőt.
                    if (CurrentNote != null && deletedIds.Contains(CurrentNote.NoteId)) CurrentNote = null;

                    await LoadDataAsync();
                }
            }
        }

        // Egyetlen jegyzet törlése megerősítéssel.
        private async void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is NoteViewModel nvm)
            {
                var res = MessageBox.Show($"Biztosan törli a(z) {nvm.Title} jegyzetet?", "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _http.DeleteAsync($"api/notes/{nvm.NoteId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            ShowError("A jegyzet törlése nem sikerült.");
                            return;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        ShowError($"A jegyzet törlése nem sikerült: {ex.Message}");
                        return;
                    }

                    // Gyorsítótár és szerkesztő takarítása (lásd DeleteFolder_Click).
                    _openedNotes.Remove(nvm.NoteId);
                    if (CurrentNote?.NoteId == nvm.NoteId) CurrentNote = null;
                    await LoadDataAsync();
                }
            }
        }

        // ---------- Átnevezés ----------
        // Az átnevezés "inline": a fában a címke helyett egy TextBox jelenik meg (IsEditing = true).
        // Enter vagy fókuszvesztés = mentés, Escape = elvetés.
        // A mentés optimista: előbb átírjuk a helyi értéket, és csak sikertelen API-hívásnál állítjuk vissza.

        // Kilép minden mappa és jegyzet szerkesztő módjából, hogy egyszerre csak egy TextBox legyen aktív.
        private void ExitAllEditing()
        {
            foreach (var item in sp_Files.Items)
            {
                if (item is FolderViewModel f)
                {
                    f.IsEditing = false;
                    foreach (var n in f.Notes) n.IsEditing = false;
                }
            }
        }

        // Amikor az átnevező TextBox láthatóvá válik, fókuszt kap és kijelöli a teljes szöveget,
        // így a felhasználó azonnal gépelhet. A BeginInvoke azért kell, mert a vezérlő
        // megjelenésekor a fókuszállítás még nem működne megbízhatóan; az Input prioritás
        // biztosítja, hogy a megjelenítés lefutása utánra kerüljön.
        private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is TextBox tb && tb.IsVisible)
            {
                tb.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    tb.Focus();
                    tb.SelectAll();
                }));
            }
        }

        // ----- Jegyzet -----

        // Átnevezés indítása a helyi menüből.
        private void EnableNoteRename_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is NoteViewModel nvm)
            {
                ExitAllEditing();
                nvm.IsEditing = true;
            }
        }

        // Enter = mentés, Escape = az eredeti cím visszaállítása.
        // e.Handled = true, hogy a billentyű ne jusson tovább más vezérlőkhöz.
        private async void NoteRenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not NoteViewModel nvm) return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await CommitNoteRenameAsync(nvm);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                nvm.Title = nvm.OriginalNote?.Title ?? nvm.Title;
                nvm.IsEditing = false; // a CommitNoteRenameAsync így már nem fut le érdemben (IsEditing == false)
            }
        }

        // Ha a felhasználó máshová kattint, ugyanúgy mentjük a változást, mint Enter-nél.
        private async void NoteRenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is NoteViewModel nvm)
                await CommitNoteRenameAsync(nvm);
        }

        // A jegyzet átnevezésének tényleges végrehajtása: validáció, helyi frissítés, mentés az API-ba,
        // hiba esetén visszaállítás.
        private async Task CommitNoteRenameAsync(NoteViewModel nvm)
        {
            // Védelem a dupla futás ellen: Enter után a TextBox eltűnik és LostFocus is elsülhet.
            // Az IsEditing flag biztosítja, hogy a mentés csak egyszer menjen végbe.
            if (!nvm.IsEditing || nvm.OriginalNote == null) return;
            nvm.IsEditing = false;

            var oldTitle = nvm.OriginalNote.Title ?? "";
            var newTitle = nvm.Title.Trim();

            // Az API/adatbázis megszorításával egyező hosszellenőrzés (1–50 karakter).
            if (newTitle.Length is < 1 or > 50)
            {
                nvm.Title = oldTitle;
                MessageBox.Show("A név 1 és 50 karakter közötti lehet.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            nvm.Title = newTitle;
            if (newTitle == oldTitle) return; // nem változott semmi, felesleges az API-hívás

            // A fában lévő példányt és az élő (esetleg szerkesztőben megnyitott) példányt is frissítjük,
            // hogy a két nézet konzisztens maradjon. A PUT az élő példányt küldi, így a
            // még nem mentett tartalom is elmentődik az átnevezéssel együtt.
            var live = GetLiveNote(nvm);
            nvm.OriginalNote.Title = newTitle;
            live.Title = newTitle;
            try
            {
                var response = await _http.PutAsJsonAsync($"api/notes/{nvm.NoteId}", live);
                if (response.IsSuccessStatusCode) return; // siker, nincs mit visszaállítani
            }
            catch (HttpRequestException) { } // hálózati hiba: továbbesünk a visszaállításhoz

            // Ide csak sikertelen mentés esetén jutunk: mindent visszaállítunk a régi névre.
            nvm.OriginalNote.Title = oldTitle;
            live.Title = oldTitle;
            nvm.Title = oldTitle;
            MessageBox.Show("Nem sikerült elmenteni a jegyzet új nevét.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ----- Mappa -----
        // A mappa átnevezése a jegyzetével megegyező logikájú, csak itt nincs "élő" gyorsítótár-példány.

        private void EnableFolderRename_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is FolderViewModel fvm)
            {
                ExitAllEditing();
                fvm.IsEditing = true;
            }
        }

        private async void FolderRenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not FolderViewModel fvm) return;

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await CommitFolderRenameAsync(fvm);
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                fvm.FolderName = fvm.OriginalFolder?.FolderName ?? fvm.FolderName;
                fvm.IsEditing = false;
            }
        }

        private async void FolderRenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is FolderViewModel fvm)
                await CommitFolderRenameAsync(fvm);
        }

        // Mappa átnevezése: validáció, optimista helyi frissítés, mentés, hiba esetén visszaállítás.
        private async Task CommitFolderRenameAsync(FolderViewModel fvm)
        {
            if (!fvm.IsEditing || fvm.OriginalFolder == null) return; // dupla futás elleni védelem
            fvm.IsEditing = false;

            var oldName = fvm.OriginalFolder.FolderName ?? "";
            var newName = fvm.FolderName.Trim();

            if (newName.Length is < 1 or > 50)
            {
                fvm.FolderName = oldName;
                MessageBox.Show("A név 1 és 50 karakter közötti lehet.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            fvm.FolderName = newName;
            if (newName == oldName) return;

            fvm.OriginalFolder.FolderName = newName;
            try
            {
                // Csak az azonosítót és az új nevet küldjük, a mappa jegyzeteit nem kell újra elküldeni.
                var payload = new Folder { FolderId = fvm.FolderId, FolderName = newName };
                var response = await _http.PutAsJsonAsync($"api/folders/{fvm.FolderId}", payload);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }

            // Sikertelen mentés: visszaállítás a régi névre.
            fvm.OriginalFolder.FolderName = oldName;
            fvm.FolderName = oldName;
            MessageBox.Show("Nem sikerült menteni a mappa új nevét.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        // ----- Csatolmányok -----

        // Fájl feltöltése a jelenleg megnyitott jegyzethez (multipart/form-data kérésként).
        private async void AddAttachment_Click(object sender, RoutedEventArgs e)
        {
            // Csatolmányt csak megnyitott jegyzethez lehet adni.
            if (CurrentNote == null)
            {
                MessageBox.Show("Először válasszon ki egy jegyzetet.", "Csatolmány",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Csatolmány kiválasztása",
                CheckFileExists = true,
                Multiselect = false
            };
            if (dialog.ShowDialog() != true) return; // a felhasználó megszakította

            // A fájlnév hossza az adatbázis oszlopmérete miatt korlátozott; jobb itt jelezni,
            // mint az API-tól visszakapni egy érthetetlen hibát.
            var fileName = Path.GetFileName(dialog.FileName);
            if (fileName.Length > 50)
            {
                MessageBox.Show("A fájlnév legfeljebb 50 karakter lehet. Nevezze át a fájlt, majd próbáld újra.",
                    "Túl hosszú fájlnév", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // A using-ok gondoskodnak róla, hogy a fájl és a stream felszabaduljon a feltöltés után.
                // A "file" mezőnév egyezzen az API-oldali IFormFile paraméter nevével.
                using var form = new MultipartFormDataContent();
                using var stream = File.OpenRead(dialog.FileName);
                using var fileContent = new StreamContent(stream);
                form.Add(fileContent, "file", fileName);

                var response = await _http.PostAsync($"api/attachments/{CurrentNote.NoteId}", form);
                if (!response.IsSuccessStatusCode)
                {
                    // Az API hibaüzenetét is megmutatjuk, így könnyebb a hibakeresés.
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"A feltöltés nem sikerült.\n{error}", "Hiba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Újratöltjük a jegyzetet, hogy az új csatolmány megjelenjen a listában.
                await RefreshCurrentNoteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A fájl feltöltése nem sikerült: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Csatolmány letöltése: a felhasználó által választott helyre mentjük a szerverről kapott bájtokat.
        // A csatolmányt a gomb DataContext-je adja (a lista elemeihez kötött sablonból).
        private async void DownloadAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not Attachment attachment)
                return;

            var dialog = new SaveFileDialog
            {
                FileName = attachment.Name ?? "csatolmany",
                Title = "Csatolmány mentése"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                var response = await _http.GetAsync($"api/attachments/{attachment.AttachmentId}");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("A csatolmány letöltése nem sikerült.", "Hiba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Memóriába olvassuk a teljes fájlt; kisebb csatolmányoknál ez rendben van,
                // nagy fájloknál érdemes lenne streamelni.
                var bytes = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A letöltés nem sikerült: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Csatolmány törlése megerősítés után, majd a jegyzet frissítése.
        private async void DeleteAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.DataContext is not Attachment attachment)
                return;

            var result = MessageBox.Show(
                $"Biztosan törli a(z) \"{attachment.Name}\" csatolmányt?",
                "Csatolmány törlése", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var response = await _http.DeleteAsync($"api/attachments/{attachment.AttachmentId}");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show("A csatolmány törlése sikertelen.", "Hiba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                await RefreshCurrentNoteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A törlés nem sikerült: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Újratölti a megnyitott jegyzetet a szerverről (a csatolmányok listájának frissítéséhez),
        /// de a cím/tartalom/mappa mezőket a helyi, még nem mentett értékekből tartja meg.
        /// Így a csatolmány-művelet nem írja felül a szerkesztő félbehagyott módosításait.
        /// Teljes LoadDataAsync helyett azért célszerű ez, mert az újraépítené a fát és a szerkesztőt is.
        /// </summary>
        private async Task RefreshCurrentNoteAsync()
        {
            if (CurrentNote == null) return;

            var noteId = CurrentNote.NoteId;
            var notes = await _http.GetFromJsonAsync<List<Note>>("api/notes") ?? new();
            var refreshed = notes.FirstOrDefault(n => n.NoteId == noteId);
            if (refreshed == null) return; // közben törölték a szerveren

            // A szerver még a régi címet/tartalmat tudja, ezért a helyi értékeket visszamásoljuk.
            refreshed.Title = CurrentNote.Title;
            refreshed.Content = CurrentNote.Content;
            refreshed.FolderId = CurrentNote.FolderId;

            // Az új példányt mindenhol be kell állítani: gyorsítótárban, szerkesztőben és a fa nézetmodelljében.
            _openedNotes[noteId] = refreshed;
            CurrentNote = refreshed;

            foreach (var folder in sp_Files.Items.OfType<FolderViewModel>())
            {
                var noteVm = folder.Notes.FirstOrDefault(n => n.NoteId == noteId);
                if (noteVm != null)
                {
                    noteVm.OriginalNote = refreshed;
                    noteVm.Title = refreshed.Title ?? "Névtelen jegyzet";
                    noteVm.Content = refreshed.Content ?? "";
                    break; // egy jegyzet csak egy mappában lehet
                }
            }
        }

        // ---------- Bezárás ----------

        private bool _closeConfirmed;   // true, ha a mentés lefutott és az ablak ténylegesen bezárható
        private bool _isSavingOnClose;  // védelem a többszöri X-kattintás ellen mentés közben

        /// <summary>
        /// Bezáráskor az összes megnyitott jegyzetet elmentjük az API-ba.
        /// Mivel a mentés aszinkron, de a Closing esemény szinkron várja a döntést, először mindig
        /// megszakítjuk a bezárást (e.Cancel = true), elvégezzük a mentést, majd sikeres mentés
        /// (vagy a felhasználó jóváhagyása) után újra meghívjuk a Close()-t, ezúttal engedve a bezárást.
        /// </summary>
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_closeConfirmed) return; // a mentés kész, az ablak bezárulhat

            e.Cancel = true;                 // egyelőre nem zárjuk be
            if (_isSavingOnClose) return;    // már folyik a mentés, a további X-kattintásokat figyelmen kívül hagyjuk
            _isSavingOnClose = true;

            // Visszaadjuk a vezérlést a UI szálnak, hogy a Cancel biztosan érvényesüljön, mielőtt a mentés elindul.
            await Task.Yield();

            try
            {
                var errors = new List<string>();

                // ToList(): másolaton iterálunk, így a ciklus közbeni módosítás nem okoz kivételt.
                foreach (var note in _openedNotes.Values.ToList())
                {
                    try
                    {
                        var response = await _http.PutAsJsonAsync($"api/notes/{note.NoteId}", note);
                        if (!response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            if (body.Length > 300) body = body[..300] + "…"; // a hosszú hibaüzenetet levágjuk
                            errors.Add($"„{note.Title}”: {(int)response.StatusCode} {body}");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Egy jegyzet hibája ne akadályozza a többi mentését; összegyűjtjük a hibákat.
                        errors.Add($"„{note.Title}”: {ex.Message}");
                    }
                }

                // Ha bármi nem mentődött el, a felhasználó dönt: marad szerkeszteni vagy mentés nélkül kilép.
                if (errors.Count > 0)
                {
                    var res = MessageBox.Show(
                        "Nem sikerült minden jegyzetet elmenteni:\n\n" + string.Join("\n", errors) + "\n\nKilép mentés nélkül?",
                        "Mentési hiba", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.No) return; // az ablak nyitva marad
                }

                _closeConfirmed = true;
                Close(); // újra bezárási kísérlet, most már átengedi a korai return
            }
            finally
            {
                // Akkor is feloldjuk a zárat, ha a felhasználó a bezárás megszakítását választotta.
                _isSavingOnClose = false;
            }
        }
    }
}