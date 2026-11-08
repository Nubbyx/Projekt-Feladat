using Models;

namespace Frontend
{
    public class FolderViewModel : ViewModelBase
    {
        public int FolderId { get; set; }
        public List<NoteViewModel> Notes { get; set; } = new();
        public Folder? OriginalFolder { get; set; }

        private string _folderName = string.Empty;
        public string FolderName
        {
            get => _folderName;
            set => SetField(ref _folderName, value);
        }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetField(ref _isExpanded, value);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetField(ref _isEditing, value);
        }
    }
}