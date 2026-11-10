using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Frontend
{
    /// <summary>
    /// Közös ős a nézetmodelleknek: megvalósítja az INotifyPropertyChanged-et, így ha egy property
    /// értéke megváltozik, a WPF-es adatkötések automatikusan frissítik a felületet.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        // A [CallerMemberName] miatt a property nevét nem kell kézzel megadni: a hívó property
        // neve automatikusan beíródik (így elgépelni sem lehet).
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Beállítja a mező értékét, és csak akkor jelez a felületnek, ha az érték tényleg megváltozott.
        /// Visszatérési értéke jelzi, történt-e változás. Használat: set => SetField(ref _mezo, value);
        /// </summary>
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false; // nincs változás, nincs értesítés
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
