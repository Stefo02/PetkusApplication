using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;
using PetkusApplication.Service;

namespace PetkusApplication.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ItemCRUD _itemCrud;
        private ObservableCollection<Item> _items;
        private Item _selectedItem;

        public MainViewModel()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32)); // Adjust version as per your MySQL server version
            optionsBuilder.UseMySql("Server=localhost;Database=myappdb;Uid=root;Pwd=;", serverVersion);

            _itemCrud = new ItemCRUD(new AppDbContext(optionsBuilder.Options));
            Items = new ObservableCollection<Item>(_itemCrud.GetAllItems());
        }

        public ObservableCollection<Item> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                RaisePropertyChanged(nameof(Items));
            }
        }

        public Item SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                RaisePropertyChanged(nameof(SelectedItem));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddItem(Item newItem)
        {
            _itemCrud.CreateItem(newItem);
            Items.Add(newItem);
        }

        public void UpdateItem(Item updatedItem)
        {
            _itemCrud.UpdateItem(updatedItem);
            // Optionally update the ObservableCollection if needed
            // Items = new ObservableCollection<Item>(_itemCrud.GetAllItems());
        }

        public void DeleteItem(Item itemToDelete)
        {
            _itemCrud.DeleteItem(itemToDelete.Id);
            Items.Remove(itemToDelete);
        }
    }
}
