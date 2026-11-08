using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Models
{
    public class Note : INotifyPropertyChanged
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NoteId { get; set; }

        [Required, StringLength(50, MinimumLength = 1)]
        public string? Title { get; set; }

        [Required(AllowEmptyStrings = true)]
        public string? Content { get; set; }

        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }
        public List<Attachment> Attachments { get; set; } = new();

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