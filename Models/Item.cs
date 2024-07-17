using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PetkusApplication.Models
{
    public class Item : INotifyPropertyChanged
    {
        private string _name;
        private string _description;
        private byte[] _timestamp;

        public int Id { get; set; }

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        public byte[] Timestamp
        {
            get { return _timestamp; }
            set { _timestamp = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
