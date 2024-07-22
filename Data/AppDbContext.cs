using Microsoft.EntityFrameworkCore;
using MySqlConnector; // Use this for MySqlCommand and MySqlConnection
using PetkusApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PetkusApplication.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        // Add DbSets for other models if necessary

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("server=localhost;database=myappdb;user=root;password=;", new MySqlServerVersion(new Version(10, 4, 32)));
            }
        }

        // Method to get items from all tables
        public List<Item> GetItemsFromAllTables()
        {
            var items = new List<Item>();
            var tableNames = GetTablesWithColumns();

            foreach (var tableName in tableNames)
            {
                items.AddRange(GetItemsFromTable(tableName));
            }

            return items;
        }

        // Method to get table names with specific columns
        public List<string> GetTablesWithColumns()
        {
            var tableNames = new List<string>();

            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();
                string query = @"
                    SELECT DISTINCT TABLE_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = 'myappdb'
                    AND COLUMN_NAME IN ('Id', 'Opis', 'Proizvodjac', 'Fabricki_kod', 'Kolicina', 'Puna_cena', 'Dimenzije', 'Tezina', 'Vrednost_rabata')
                    GROUP BY TABLE_NAME
                    HAVING COUNT(DISTINCT COLUMN_NAME) = 9";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tableNames.Add(reader.GetString("TABLE_NAME"));
                        }
                    }
                }
            }

            return tableNames;
        }

        // Method to get items from a specific table
        public List<Item> GetItemsFromTable(string tableName)
        {
            var items = new List<Item>();

            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();
                string query = $@"
                    SELECT Id, Opis, Proizvodjac, Fabricki_kod, Kolicina, Puna_cena, Dimenzije, Tezina, Vrednost_rabata
                    FROM `{tableName}`";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new Item
                            {
                                Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? 0 : reader.GetInt32("Id"),
                                Opis = reader.IsDBNull(reader.GetOrdinal("Opis")) ? string.Empty : reader.GetString("Opis"),
                                Proizvodjac = reader.IsDBNull(reader.GetOrdinal("Proizvodjac")) ? string.Empty : reader.GetString("Proizvodjac"),
                                Fabricki_kod = reader.IsDBNull(reader.GetOrdinal("Fabricki_kod")) ? string.Empty : reader.GetString("Fabricki_kod"),
                                Kolicina = reader.IsDBNull(reader.GetOrdinal("Kolicina")) ? 0 : reader.GetInt32("Kolicina"),
                                Puna_cena = reader.IsDBNull(reader.GetOrdinal("Puna_cena")) ? 0 : reader.GetDecimal("Puna_cena"),
                                Dimenzije = reader.IsDBNull(reader.GetOrdinal("Dimenzije")) ? string.Empty : reader.GetString("Dimenzije"),
                                Tezina = reader.IsDBNull(reader.GetOrdinal("Tezina")) ? 0 : reader.GetDecimal("Tezina"),
                                Vrednost_rabata = reader.IsDBNull(reader.GetOrdinal("Vrednost_rabata")) ? 0 : reader.GetDecimal("Vrednost_rabata")
                            });
                        }
                    }
                }
            }

            return items;
        }

        // Method to get items with low quantity
        public List<Item> GetItemsWithLowQuantity(int threshold)
        {
            var lowQuantityItems = new List<Item>();
            var tableNames = GetTablesWithColumns();

            foreach (var tableName in tableNames)
            {
                lowQuantityItems.AddRange(GetItemsFromTableWithLowQuantity(tableName, threshold));
            }

            return lowQuantityItems;
        }

        // Method to get items from a table with low quantity
        public List<Item> GetItemsFromTableWithLowQuantity(string tableName, int threshold)
        {
            var items = new List<Item>();

            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();
                string query = $@"
                    SELECT Id, Opis, Proizvodjac, Fabricki_kod, Kolicina, Puna_cena, Dimenzije, Tezina, Vrednost_rabata
                    FROM `{tableName}`
                    WHERE Kolicina < @Threshold";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Threshold", threshold);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new Item
                            {
                                Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? 0 : reader.GetInt32("Id"),
                                Opis = reader.IsDBNull(reader.GetOrdinal("Opis")) ? string.Empty : reader.GetString("Opis"),
                                Proizvodjac = reader.IsDBNull(reader.GetOrdinal("Proizvodjac")) ? string.Empty : reader.GetString("Proizvodjac"),
                                Fabricki_kod = reader.IsDBNull(reader.GetOrdinal("Fabricki_kod")) ? string.Empty : reader.GetString("Fabricki_kod"),
                                Kolicina = reader.IsDBNull(reader.GetOrdinal("Kolicina")) ? 0 : reader.GetInt32("Kolicina"),
                                Puna_cena = reader.IsDBNull(reader.GetOrdinal("Puna_cena")) ? 0 : reader.GetDecimal("Puna_cena"),
                                Dimenzije = reader.IsDBNull(reader.GetOrdinal("Dimenzije")) ? string.Empty : reader.GetString("Dimenzije"),
                                Tezina = reader.IsDBNull(reader.GetOrdinal("Tezina")) ? 0 : reader.GetDecimal("Tezina"),
                                Vrednost_rabata = reader.IsDBNull(reader.GetOrdinal("Vrednost_rabata")) ? 0 : reader.GetDecimal("Vrednost_rabata")
                            });
                        }
                    }
                }
            }

            return items;
        }

        // Method to add an item to a table
        public void AddItem(string tableName, Item item)
        {
            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();
                string query = $@"
                    INSERT INTO {tableName} (Opis, Proizvodjac, Fabricki_kod, Kolicina, Puna_cena, Dimenzije, Tezina, Vrednost_rabata)
                    VALUES (@Opis, @Proizvodjac, @Fabricki_kod, @Kolicina, @Puna_cena, @Dimenzije, @Tezina, @Vrednost_rabata)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Opis", item.Opis);
                    command.Parameters.AddWithValue("@Proizvodjac", item.Proizvodjac);
                    command.Parameters.AddWithValue("@Fabricki_kod", item.Fabricki_kod);
                    command.Parameters.AddWithValue("@Kolicina", item.Kolicina);
                    command.Parameters.AddWithValue("@Puna_cena", item.Puna_cena);
                    command.Parameters.AddWithValue("@Dimenzije", item.Dimenzije);
                    command.Parameters.AddWithValue("@Tezina", item.Tezina);
                    command.Parameters.AddWithValue("@Vrednost_rabata", item.Vrednost_rabata);

                    command.ExecuteNonQuery();
                }
            }
        }

        // Method to update an item in a table
        public void UpdateItem(string tableName, Item item)
        {
            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();

                string query = $@"
                    UPDATE {tableName} SET 
                    Opis = @Opis, 
                    Proizvodjac = @Proizvodjac, 
                    Fabricki_kod = @Fabricki_kod, 
                    Kolicina = @Kolicina, 
                    Puna_cena = @Puna_cena, 
                    Dimenzije = @Dimenzije, 
                    Tezina = @Tezina, 
                    Vrednost_rabata = @Vrednost_rabata 
                    WHERE Id = @Id";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Opis", item.Opis);
                    cmd.Parameters.AddWithValue("@Proizvodjac", item.Proizvodjac);
                    cmd.Parameters.AddWithValue("@Fabricki_kod", item.Fabricki_kod);
                    cmd.Parameters.AddWithValue("@Kolicina", item.Kolicina);
                    cmd.Parameters.AddWithValue("@Puna_cena", item.Puna_cena);
                    cmd.Parameters.AddWithValue("@Dimenzije", item.Dimenzije);
                    cmd.Parameters.AddWithValue("@Tezina", item.Tezina);
                    cmd.Parameters.AddWithValue("@Vrednost_rabata", item.Vrednost_rabata);
                    cmd.Parameters.AddWithValue("@Id", item.Id);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Method to delete an item from a table
        public void DeleteItem(string tableName, int id)
        {
            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();
                string query = $@"
                    DELETE FROM {tableName}
                    WHERE Id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
