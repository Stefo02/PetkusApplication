using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetkusApplication.Models
{
    public class GroupedItem : INotifyPropertyChanged, IDataErrorInfo
    {
        private int _quantity;

        public string Opis { get; set; }
        public string GroupName { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_quantity != value)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                }
            }
        }

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(Quantity))
                {
                    if (Quantity <= 0)
                    {
                        return "Unesite broj veći od 0.";
                    }
                }
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
