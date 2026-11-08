using Models;

namespace Frontend
{
    public class NoteViewModel : ViewModelBase
    {
        public int NoteId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int? FolderId { get; set; }
        public Note? OriginalNote { get; set; }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetField(ref _isEditing, value);
        }
    }
}