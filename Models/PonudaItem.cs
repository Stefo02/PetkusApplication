using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetkusApplication.Models
{
    public class PonudaItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Fabricki_kod { get; set; }
        public string Opis { get; set; }
        public decimal Puna_cena { get; set; }
        public string Dimenzije { get; set; }
        public decimal Disipacija { get; set; }
        public decimal Tezina { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public List<string> RelatedFabricki_kod { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
