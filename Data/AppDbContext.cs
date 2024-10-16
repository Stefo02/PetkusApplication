using DocumentFormat.OpenXml.Office.Word;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using PetkusApplication.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PetkusApplication.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public List<AuditLog> GetAuditLogs()
        {
            return AuditLogs.ToList(); // Vraća sve podatke iz tabele AuditLogs
        }


        public void UpdateItemAcrossTables(Item item)
        {
            // Pronađi sve tabele koje sadrže potrebne kolone
            var tableNames = GetTablesWithColumns();

            foreach (var tableName in tableNames)
            {
                // Pronađi duplikate u svakoj tabeli na osnovu Fabricki_kod
                var duplicates = GetItemsFromTable(tableName)
                    .Where(i => i.Fabricki_kod == item.Fabricki_kod && i.Id != item.Id)
                    .ToList();

                foreach (var duplicate in duplicates)
                {
                    // Ažuriraj podatke duplikata
                    duplicate.Opis = item.Opis;
                    duplicate.Proizvodjac = item.Proizvodjac;
                    duplicate.Kolicina = item.Kolicina;
                    duplicate.Puna_cena = item.Puna_cena;
                    duplicate.Dimenzije = item.Dimenzije;
                    duplicate.Tezina = item.Tezina;
                    duplicate.Vrednost_rabata = item.Vrednost_rabata;
                    duplicate.MinKolicina = item.MinKolicina;
                    duplicate.JedinicaMere = item.JedinicaMere;

                    // Ažuriraj duplikat u bazi
                    UpdateItem(tableName, duplicate);
                }
            }

            // Konačno ažuriraj originalni red u njegovoj tabeli
            UpdateItem(item.OriginalTable, item);
        }

        public void UpdateItemWithDuplicateHandling(string tableName, Item item)
        {
            UpdateItem(tableName, item);

            // Pronađi sve duplikate koji imaju isti Fabricki_kod, bez obzira na ostale vrednosti
            var duplicates = GetItemsFromTable(tableName)
                .Where(i => i.Fabricki_kod == item.Fabricki_kod && i.Id != item.Id)  // Ova linija osigurava da svi redovi sa istim Fabricki_kod budu obuhvaćeni, osim originalnog reda
                .ToList();

            foreach (var duplicate in duplicates)
            {
                // Ažuriraj podatke u duplikatima samo na osnovu originalnog reda
                duplicate.Opis = item.Opis;
                duplicate.Proizvodjac = item.Proizvodjac;
                duplicate.Kolicina = item.Kolicina;
                duplicate.Puna_cena = item.Puna_cena;
                duplicate.Dimenzije = item.Dimenzije;
                duplicate.Tezina = item.Tezina;
                duplicate.Vrednost_rabata = item.Vrednost_rabata;
                duplicate.MinKolicina = item.MinKolicina;
                duplicate.JedinicaMere = item.JedinicaMere;

                // Ažuriraj duplikat u bazi
                UpdateItem(tableName, duplicate);
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySql("server=localhost;database=myappdb;user=root;password=;", new MySqlServerVersion(new Version(10, 4, 32)));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSession>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(us => us.UserId);
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
            AND COLUMN_NAME IN ('Id', 'Opis', 'Proizvodjac', 'Fabricki_kod', 'Kolicina', 'Puna_cena', 'Dimenzije', 'Tezina', 'Vrednost_rabata', 'Min_Kolicina', 'JedinicaMere')
            GROUP BY TABLE_NAME
            HAVING COUNT(DISTINCT COLUMN_NAME) = 11";  // Promeni na 11 zbog dodatne kolone

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

        public void SaveAuditLog(AuditLog auditLog)
        {
            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();

                string query = @"INSERT INTO AuditLogs (UserId, Action, TableAffected, RecordId, Timestamp, OldValue, NewValue, Username)
                         VALUES (@UserId, @Action, @TableAffected, @RecordId, @Timestamp, @OldValue, @NewValue, @Username)";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserId", auditLog.UserId);
                    cmd.Parameters.AddWithValue("@Action", auditLog.Action);
                    cmd.Parameters.AddWithValue("@TableAffected", auditLog.TableAffected);
                    cmd.Parameters.AddWithValue("@RecordId", auditLog.RecordId);
                    cmd.Parameters.AddWithValue("@Timestamp", auditLog.Timestamp);
                    cmd.Parameters.AddWithValue("@OldValue", auditLog.OldValue);
                    cmd.Parameters.AddWithValue("@NewValue", auditLog.NewValue);
                    cmd.Parameters.AddWithValue("@Username", auditLog.Username);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Method to get items from a specific table
        public List<Item> GetItemsFromTable(string tableName)
        {
            var items = new List<Item>();

            using (var connection = new MySqlConnection("server=localhost;database=myappdb;user=root;password="))
            {
                connection.Open();
                string query = $@"
    SELECT Id, Opis, Proizvodjac, Fabricki_kod, Kolicina, Puna_cena, Dimenzije, Tezina, Vrednost_rabata, Min_Kolicina, JedinicaMere, Disipacija
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
                                Disipacija = reader.IsDBNull(reader.GetOrdinal("Disipacija")) ? 0 : reader.GetDecimal("Disipacija"),
                                Vrednost_rabata = reader.IsDBNull(reader.GetOrdinal("Vrednost_rabata")) ? 0 : reader.GetDecimal("Vrednost_rabata"),
                                MinKolicina = reader.IsDBNull(reader.GetOrdinal("Min_Kolicina")) ? 0 : reader.GetInt32("Min_Kolicina"),
                                JedinicaMere = reader.IsDBNull(reader.GetOrdinal("JedinicaMere")) ? string.Empty : reader.GetString("JedinicaMere"),  // Dodaj čitanje JedinicaMere
                                OriginalTable = tableName
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
    SELECT Id, Opis, Proizvodjac, Fabricki_kod, Kolicina, Puna_cena, Dimenzije, Tezina, Vrednost_rabata, Min_Kolicina, JedinicaMere
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
                                Vrednost_rabata = reader.IsDBNull(reader.GetOrdinal("Vrednost_rabata")) ? 0 : reader.GetDecimal("Vrednost_rabata"),
                                MinKolicina = reader.IsDBNull(reader.GetOrdinal("Min_Kolicina")) ? 0 : reader.GetInt32("Min_Kolicina"),
                                JedinicaMere = reader.IsDBNull(reader.GetOrdinal("JedinicaMere")) ? string.Empty : reader.GetString("JedinicaMere"),  // Dodaj JedinicaMere
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
        INSERT INTO `{tableName}` (Opis, Proizvodjac, Fabricki_kod, Kolicina, Puna_cena, Dimenzije, Tezina, Vrednost_rabata, Min_Kolicina, JedinicaMere, Disipacija)  
        VALUES (@Opis, @Proizvodjac, @Fabricki_kod, @Kolicina, @Puna_cena, @Dimenzije, @Tezina, @Vrednost_rabata, @Min_Kolicina, @JedinicaMere, @Disipacija)";
        
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
                    cmd.Parameters.AddWithValue("@Min_Kolicina", item.MinKolicina);
                    cmd.Parameters.AddWithValue("@JedinicaMere", item.JedinicaMere);
                    cmd.Parameters.AddWithValue("@Disipacija", item.Disipacija);  // Ensure this is included

                    cmd.ExecuteNonQuery();
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
        UPDATE `{tableName}` SET 
        Opis = @Opis, 
        Proizvodjac = @Proizvodjac, 
        Fabricki_kod = @Fabricki_kod, 
        Kolicina = @Kolicina, 
        Puna_cena = @Puna_cena, 
        Dimenzije = @Dimenzije, 
        Tezina = @Tezina, 
        Vrednost_rabata = @Vrednost_rabata,
        Min_Kolicina = @Min_Kolicina,
        JedinicaMere = @JedinicaMere,  -- Add the missing comma here
        Disipacija = @Disipacija
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
                    cmd.Parameters.AddWithValue("@Min_Kolicina", item.MinKolicina);
                    cmd.Parameters.AddWithValue("@JedinicaMere", item.JedinicaMere);
                    cmd.Parameters.AddWithValue("@Disipacija", item.Disipacija);
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

        public void EndUserSession(int userId)
        {
            var session = UserSessions.FirstOrDefault(s => s.UserId == userId);
            if (session != null)
            {
                UserSessions.Remove(session);
                SaveChanges();
            }
        }
    }
}
