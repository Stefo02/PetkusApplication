using PetkusApplication.Data;
using PetkusApplication.Models;
using System.Collections.Generic;
using System.Linq;

namespace PetkusApplication.Service
{
    public class ItemCRUD
    {
        private readonly AppDbContext _context;

        public ItemCRUD(AppDbContext context)
        {
            _context = context;
        }

        // Create
        public void CreateItem(Item item)
        {
            _context.Items.Add(item);
            _context.SaveChanges();
        }

        // Read
        public List<Item> GetAllItems()
        {
            return _context.Items.ToList();
        }

        public Item GetItemById(int id)
        {
            return _context.Items.Find(id);
        }

        // Update
        public void UpdateItem(Item item)
        {
            var existingItem = _context.Items.Find(item.Id);
            if (existingItem != null)
            {
                _context.Entry(existingItem).CurrentValues.SetValues(item);
                _context.SaveChanges();
            }
        }


        // Delete
        public void DeleteItem(int id)
        {
            var item = _context.Items.Find(id);
            if (item != null)
            {
                _context.Items.Remove(item);
                _context.SaveChanges();
            }
        }
    }
}
