using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using OfficeOpenXml;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class Racunanjeponude : Window
    {
        private MySqlConnection connection;
        private FormiranjePonudeView _formiranjePonudeView;
        private List<PonudaItem> selectedItems;

        public Racunanjeponude(FormiranjePonudeView parent, List<PonudaItem> selectedItems)
        {
            InitializeComponent();
            this.selectedItems = selectedItems;
            SelectedItemsDataGrid.ItemsSource = selectedItems;
            InitializeDatabaseConnection();
        }

        private void InitializeDatabaseConnection()
        {
            string connectionString = "server=localhost;database=myappdb;user=root;password=;";
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }

        private void UpdateAndExport_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in selectedItems)
            {
                // Pronađi tabelu koja sadrži Fabricki_kod
                string tableName = FindTableWithFabrickiKod(item.Fabricki_kod);

                if (tableName != null)
                {
                    // Ažuriraj kolonu "Kolicina" sa vrednošću iz "KolicinaZaNarucivanje"
                    using (var command = new MySqlCommand($"UPDATE {tableName} SET Kolicina = @Kolicina WHERE Fabricki_kod = @Fabricki_kod", connection))
                    {
                        command.Parameters.AddWithValue("@Kolicina", item.KolicinaZaNarucivanje);
                        command.Parameters.AddWithValue("@Fabricki_kod", item.Fabricki_kod);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    MessageBox.Show($"Fabricki_kod {item.Fabricki_kod} not found in any table.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            // Izvoz podataka u Excel
            GenerateExcelFile(selectedItems);
        }


        private string FindTableWithFabrickiKod(string fabrickiKod)
        {
            string tableName = null;
            var tableNames = new List<string>();

            // Pronađi sve tabele koje imaju kolonu 'Fabricki_kod'
            using (var command = new MySqlCommand(@"
                SELECT TABLE_NAME
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE COLUMN_NAME = 'Fabricki_kod'
                        AND TABLE_SCHEMA = 'myappdb';", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }
            }

            // Proveri svaku tabelu da vidi da li sadrži traženi fabricki kod
            foreach (var table in tableNames)
            {
                if (TableContainsFabrickiKod(table, fabrickiKod))
                {
                    return table;
                }
            }

            return null;
        }

        private bool TableContainsFabrickiKod(string tableName, string fabrickiKod)
        {
            using (var command = new MySqlCommand($"SELECT COUNT(*) FROM {tableName} WHERE Fabricki_kod = @Fabricki_kod", connection))
            {
                command.Parameters.AddWithValue("@Fabricki_kod", fabrickiKod);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void UpdateDatabase(List<PonudaItem> items, int brojKomada)
        {
            // Lista tabela koje treba ažurirati
            var tablesToUpdate = new List<string> { "zastita_d_se", "zastita_d_si", "sklopka_d_se", "sklopka_d_si" };

            foreach (var item in items)
            {
                item.Kolicina = brojKomada;
                bool updated = false;

                foreach (var table in tablesToUpdate)
                {
                    // Ako se nalazi u tabeli i ako tabela ima kolonu 'Kolicina', ažuriraj
                    if (TableContainsFabrickiKod(table, item.Fabricki_kod) && TableContainsColumn(table, "Kolicina"))
                    {
                        // Preuzmi trenutnu količinu iz baze podataka
                        int currentQuantity = GetCurrentQuantity(table, item.Fabricki_kod);

                        // Izračunaj novu količinu
                        int newQuantity = currentQuantity + brojKomada;

                        using (var command = new MySqlCommand($"UPDATE {table} SET Kolicina = @Kolicina WHERE Fabricki_kod = @Fabricki_kod", connection))
                        {
                            command.Parameters.AddWithValue("@Kolicina", newQuantity);
                            command.Parameters.AddWithValue("@Fabricki_kod", item.Fabricki_kod);
                            command.ExecuteNonQuery();
                            updated = true;
                            break; // Ako je ažurirano u tabeli, ne treba dalje pretraživati
                        }
                    }
                }

                if (!updated)
                {
                    MessageBox.Show($"Fabricki_kod {item.Fabricki_kod} not found or column 'Kolicina' missing in any table.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private int GetCurrentQuantity(string tableName, string fabrickiKod)
        {
            using (var command = new MySqlCommand($"SELECT Kolicina FROM {tableName} WHERE Fabricki_kod = @Fabricki_kod", connection))
            {
                command.Parameters.AddWithValue("@Fabricki_kod", fabrickiKod);
                var result = command.ExecuteScalar();
                // Ako nema rezultata, vrati 0
                return result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
        }


        private bool TableContainsColumn(string tableName, string columnName)
        {
            using (var command = new MySqlCommand($@"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_NAME = @TableName
                        AND COLUMN_NAME = @ColumnName
                        AND TABLE_SCHEMA = 'myappdb';", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private void GenerateExcelFile(List<PonudaItem> items)
        {
            // Postavite licencni kontekst
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Ponuda");
                worksheet.Cells[1, 1].Value = "Fabricki_kod";
                worksheet.Cells[1, 2].Value = "Opis";
                worksheet.Cells[1, 3].Value = "Puna_cena";
                worksheet.Cells[1, 4].Value = "Dimenzije";
                worksheet.Cells[1, 5].Value = "Disipacija";
                worksheet.Cells[1, 6].Value = "Tezina";
                worksheet.Cells[1, 7].Value = "Kolicina";
                worksheet.Cells[1, 8].Value = "Vrednost_rabata";
                worksheet.Cells[1, 9].Value = "Ukupna_puna";
                worksheet.Cells[1, 10].Value = "Ukupna_rabat";
                worksheet.Cells[1, 11].Value = "Ukupna_Disipacija";
                worksheet.Cells[1, 12].Value = "Ukupna_Tezina";

                int row = 2;
                foreach (var item in items)
                {
                    worksheet.Cells[row, 1].Value = item.Fabricki_kod;
                    worksheet.Cells[row, 2].Value = item.Opis;
                    worksheet.Cells[row, 3].Value = item.Puna_cena;
                    worksheet.Cells[row, 4].Value = item.Dimenzije;
                    worksheet.Cells[row, 5].Value = item.Disipacija;
                    worksheet.Cells[row, 6].Value = item.Tezina;
                    worksheet.Cells[row, 7].Value = item.Kolicina;
                    worksheet.Cells[row, 8].Value = item.Vrednost_rabata;
                    worksheet.Cells[row, 9].Value = item.Ukupna_puna;
                    worksheet.Cells[row, 10].Value = item.Ukupna_rabat;
                    worksheet.Cells[row, 11].Value = item.Ukupna_Disipacija;
                    worksheet.Cells[row, 12].Value = item.Ukupna_Tezina;

                    row++;
                }

                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Ponuda.xlsx");
                File.WriteAllBytes(filePath, package.GetAsByteArray());

                MessageBox.Show($"Excel file created at: {filePath}");
            }
        }
    }
}
