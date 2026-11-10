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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("https://localhost:7226") };

        // A megnyitott jegyzetek memóriában tartott példányai (mentetlen szerkesztésekkel együtt).
        // Kilépéskor mindegyiket elmentjük.
        private readonly Dictionary<int, Note> _openedNotes = new();

        private Note GetLiveNote(NoteViewModel nvm) =>
            _openedNotes.TryGetValue(nvm.NoteId, out var n) ? n : nvm.OriginalNote!;

        private Note? currentNote;
        public Note? CurrentNote
        {
            get => currentNote;
            set
            {
                currentNote = value;
                OnPropertyChanged();
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
            DataContext = this;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var folders = await _http.GetFromJsonAsync<List<Folder>>("api/folders") ?? new();
                var notes = await _http.GetFromJsonAsync<List<Note>>("api/notes") ?? new();

                var folderViewModels = new List<FolderViewModel>();

                foreach (var f in folders)
                {
                    var fvm = new FolderViewModel
                    {
                        FolderId = f.FolderId,
                        FolderName = f.FolderName ?? "Névtelen mappa",
                        OriginalFolder = f,
                        IsExpanded = true
                    };

                    var folderNotes = notes.Where(n => n.FolderId == f.FolderId).ToList();
                    foreach (var n in folderNotes)
                    {
                        fvm.Notes.Add(new NoteViewModel
                        {
                            NoteId = n.NoteId,
                            Title = n.Title ?? "Névtelen jegyzet",
                            Content = n.Content ?? "",
                            FolderId = n.FolderId,
                            OriginalNote = n
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

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var newFolder = new Folder { FolderName = "Új mappa" };
            var response = await _http.PostAsJsonAsync("api/folders", newFolder);
            if (response.IsSuccessStatusCode) await LoadDataAsync();
        }

        private async void AddNoteToFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is FolderViewModel fvm)
            {
                var newNote = new Note { Title = "Új jegyzet", Content = "", FolderId = fvm.FolderId };
                var response = await _http.PostAsJsonAsync("api/notes", newNote);
                if (response.IsSuccessStatusCode) await LoadDataAsync();
            }
        }

        private void NoteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is NoteViewModel nvm && nvm.OriginalNote != null)
            {
                var live = GetLiveNote(nvm);
                _openedNotes[live.NoteId] = live;
                CurrentNote = live;
            }
        }

        private async void DeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is FolderViewModel fvm)
            {
                var res = MessageBox.Show($"Biztosan törli a(z) {fvm.FolderName} mappát és a benne lévő jegyzeteket?", "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    foreach (var n in fvm.Notes) _openedNotes.Remove(n.NoteId);
                    await _http.DeleteAsync($"api/folders/{fvm.FolderId}");
                    await LoadDataAsync();
                    CurrentNote = null;
                }
            }
        }

        private async void DeleteNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is NoteViewModel nvm)
            {
                var res = MessageBox.Show($"Biztosan törli a(z) {nvm.Title} jegyzetet?", "Törlés", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    _openedNotes.Remove(nvm.NoteId);
                    await _http.DeleteAsync($"api/notes/{nvm.NoteId}");
                    await LoadDataAsync();
                    if (CurrentNote?.NoteId == nvm.NoteId) CurrentNote = null;
                }
            }
        }

        // ---------- Átnevezés ----------

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

        // Amikor a szerkesztő TextBox láthatóvá válik, fókuszt kap és kijelöli a szöveget.
        // (A Loaded itt nem jó: az akkor is lefut, amikor a TextBox még Collapsed.)
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

        // ----- Jegyzete -----

        private void EnableNoteRename_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is NoteViewModel nvm)
            {
                ExitAllEditing();
                nvm.IsEditing = true;
            }
        }

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
                nvm.IsEditing = false;
            }
        }

        private async void NoteRenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is NoteViewModel nvm)
                await CommitNoteRenameAsync(nvm);
        }

        private async Task CommitNoteRenameAsync(NoteViewModel nvm)
        {
            if (!nvm.IsEditing || nvm.OriginalNote == null) return;
            nvm.IsEditing = false;

            var oldTitle = nvm.OriginalNote.Title ?? "";
            var newTitle = nvm.Title.Trim();

            if (newTitle.Length is < 1 or > 50)
            {
                nvm.Title = oldTitle;
                MessageBox.Show("A jegyzetnev 1 és 50 karakter közötti lehet.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            nvm.Title = newTitle;
            if (newTitle == oldTitle) return;

            var live = GetLiveNote(nvm);
            nvm.OriginalNote.Title = newTitle;
            live.Title = newTitle;
            try
            {
                var response = await _http.PutAsJsonAsync($"api/notes/{nvm.NoteId}", live);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }

            nvm.OriginalNote.Title = oldTitle;
            live.Title = oldTitle;
            nvm.Title = oldTitle;
            MessageBox.Show("Nem sikerült elmenteni a jegyzet új nevét.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // ----- Mappa -----

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

        private async Task CommitFolderRenameAsync(FolderViewModel fvm)
        {
            if (!fvm.IsEditing || fvm.OriginalFolder == null) return;
            fvm.IsEditing = false;

            var oldName = fvm.OriginalFolder.FolderName ?? "";
            var newName = fvm.FolderName.Trim();

            if (newName.Length is < 1 or > 50)
            {
                fvm.FolderName = oldName;
                MessageBox.Show("A folder neve 1 és 50 karakter közötti lehet.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            fvm.FolderName = newName;
            if (newName == oldName) return;

            fvm.OriginalFolder.FolderName = newName;
            try
            {
                // Csak az azonosítót és az új nevet küldjük, nem a teljes mappát a jegyzeteivel.
                var payload = new Folder { FolderId = fvm.FolderId, FolderName = newName };
                var response = await _http.PutAsJsonAsync($"api/folders/{fvm.FolderId}", payload);
                if (response.IsSuccessStatusCode) return;
            }
            catch (HttpRequestException) { }

            fvm.OriginalFolder.FolderName = oldName;
            fvm.FolderName = oldName;
            MessageBox.Show("Nem sikerült menteni a mappa új nevét.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        // ----- Csatolmányok -----

        private async void AddAttachment_Click(object sender, RoutedEventArgs e)
        {
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
            if (dialog.ShowDialog() != true) return;

            var fileName = Path.GetFileName(dialog.FileName);
            if (fileName.Length > 50)
            {
                MessageBox.Show("A fájlnév legfeljebb 50 karakter lehet. Nevezze át a fájlt, majd próbáld újra.",
                    "Túl hosszú fájlnév", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using var form = new MultipartFormDataContent();
                using var stream = System.IO.File.OpenRead(dialog.FileName);
                using var fileContent = new StreamContent(stream);
                form.Add(fileContent, "file", fileName);

                var response = await _http.PostAsync($"api/attachments/{CurrentNote.NoteId}", form);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"A feltöltés nem sikerült.\n{error}", "Hiba",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                await RefreshCurrentNoteAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A fájl feltöltése nem sikerült: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

                var bytes = await response.Content.ReadAsByteArrayAsync();
                await System.IO.File.WriteAllBytesAsync(dialog.FileName, bytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A letöltés nem sikerült: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private async Task RefreshCurrentNoteAsync()
        {
            if (CurrentNote == null) return;

            var noteId = CurrentNote.NoteId;
            var notes = await _http.GetFromJsonAsync<List<Note>>("api/notes") ?? new();
            var refreshed = notes.FirstOrDefault(n => n.NoteId == noteId);
            if (refreshed == null) return;

            // A szerkesztés alatt álló tartalmat megőrizzük, csak a csatolmánylistát frissítjük.
            refreshed.Title = CurrentNote.Title;
            refreshed.Content = CurrentNote.Content;
            refreshed.FolderId = CurrentNote.FolderId;

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
                    break;
                }
            }
        }

        // Mentés csak kilépéskor: az összes megnyitott jegyzet.
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var errors = new List<string>();

            foreach (var note in _openedNotes.Values)
            {
                try
                {
                    var response = _http.PutAsJsonAsync($"api/notes/{note.NoteId}", note).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        if (body.Length > 300) body = body[..300] + "…";
                        errors.Add($"„{note.Title}”: {(int)response.StatusCode} {body}");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"„{note.Title}”: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                var res = MessageBox.Show(
                    "Nem sikerült minden jegyzetet elmenteni:\n\n" + string.Join("\n", errors) + "\n\nKilép mentés nélkül?",
                    "Mentési hiba", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.No) e.Cancel = true;
            }
        }
    }
}