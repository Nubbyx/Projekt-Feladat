using Models;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Frontend
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("https://localhost:7226") };

        private List<Note> _allNotes = new List<Note>();

        private Note currentNote;

        public Note CurrentNote
        {
            get => currentNote;
            set
            {
                currentNote = value;
                OnPropertyChanged(); // Itt jelezzük a WPF-nek, hogy változott az objektum
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this; // << EZ HIÁNYZOTT: Összeköti a XAML-t a code-behinddal
        }

        private async Task LoadNotesAsync()
        {
            try
            {
                var notes = await _http.GetFromJsonAsync<List<Note>>("api/notes");
                _allNotes = notes ?? new List<Note>();

                sp_Files.ItemsSource = notes;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadNotesAsync();
        }

        private void NoteSelect(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is Note selectedNote)
            {
                CurrentNote = selectedNote;
            }
        }
    }
}