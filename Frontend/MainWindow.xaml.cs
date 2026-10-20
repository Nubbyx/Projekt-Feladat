using Models;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace Frontend
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HttpClient _http = new HttpClient { BaseAddress = new Uri("https://localhost:7226") };

        private List<Note> _allNotes = new List<Note>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async Task LoadNotesAsync()
        {
            try
            {
                var notes = await _http.GetFromJsonAsync<List<Note>>("api/ideas");
                _allNotes = notes ?? new List<Note>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}