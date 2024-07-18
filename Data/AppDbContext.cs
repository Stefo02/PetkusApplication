using Microsoft.EntityFrameworkCore;
using PetkusApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PetkusApplication.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<User> Users { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure concurrency token for Item entity
            modelBuilder.Entity<Item>()
                .Property(p => p.Timestamp)
                .IsRowVersion(); // Configures Timestamp as RowVersion for concurrency

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Replace "YourConnectionStringHere" with your actual connection string
            optionsBuilder.UseMySql("server=localhost;database=myappdb;user=root;password=;", new MySqlServerVersion(new Version(10, 4, 32)));
        }

    }
}
