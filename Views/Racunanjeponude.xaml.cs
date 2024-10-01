using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using OfficeOpenXml;
using Microsoft.Win32; 
using PetkusApplication.Models;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace PetkusApplication.Views
{
    public partial class Racunanjeponude : Window
    {
        private MySqlConnection connection;
        private FormiranjePonudeView _formiranjePonudeView;
        private List<PonudaItem> selectedItems;
        public ObservableCollection<PonudaItem> PonudaItems { get; set; }

        public Racunanjeponude(FormiranjePonudeView parent, List<PonudaItem> selectedItems)
        {
            InitializeComponent();
            this.selectedItems = selectedItems;
            SelectedItemsDataGrid.ItemsSource = selectedItems;
            this._formiranjePonudeView = parent;
            InitializeDatabaseConnection();

            // Proveri da li je ponudaItems null, ako jeste, inicijalizuj praznu kolekciju
            PonudaItems = PonudaItems ?? new ObservableCollection<PonudaItem>();

            this.DataContext = PonudaItems;

            // Učitaj količine iz baze odmah nakon otvaranja prozora
            LoadQuantitiesFromDatabase();
        }

        private void InitializeDatabaseConnection()
        {
            string connectionString = "server=localhost;database=myappdb;user=root;password=;";
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }

        private void LoadQuantitiesFromDatabase()
        {
            foreach (var item in selectedItems)
            {
                string tableName = FindTableWithFabrickiKod(item.Fabricki_kod);

                if (tableName != null)
                {
                    // Dohvati trenutnu količinu iz baze
                    int currentQuantity = GetCurrentQuantity(tableName, item.Fabricki_kod);

                    // Postavi trenutnu količinu u stavku
                    item.Kolicina = currentQuantity;

                    // Osvježi DataGrid
                    SelectedItemsDataGrid.Items.Refresh();
                }
            }
        }

        // Metoda za dodavanje novih stavki u već otvoren prozor
        public void DodajNoveStavke(List<PonudaItem> noveStavke)
        {
            foreach (var stavka in noveStavke)
            {
                // Proveri da li je stavka već dodana kako bi se izbeglo dupliranje
                if (!PonudaItems.Any(p => p.Fabricki_kod == stavka.Fabricki_kod))
                {
                    PonudaItems.Add(stavka); // Dodaj novu stavku u kolekciju samo ako ne postoji
                }
            }

            // Osvježi prikaz u DataGrid-u (ako postoji)
            SelectedItemsDataGrid.Items.Refresh();
        }

        private void UpdateAndExport_Click(object sender, RoutedEventArgs e)
        {
            // Prvo proverite sve stavke pre nego što obrišete redove
            var selectedNacinPokretanja = _formiranjePonudeView.comboBox1.SelectedItem as ComboBoxItem;
            var selectedProizvodac = _formiranjePonudeView.comboBox2.SelectedItem as ComboBoxItem;
            var selectedBrojSmerova = _formiranjePonudeView.comboBox3.SelectedItem as ComboBoxItem;
            var selectedSnaga = _formiranjePonudeView.comboBox4.SelectedItem as ComboBoxItem;

            if (selectedNacinPokretanja != null && selectedProizvodac != null && selectedBrojSmerova != null && selectedSnaga != null)
            {
                _formiranjePonudeView.UpdateProcedure();

                bool allQuantitiesValid = true;

                foreach (var item in selectedItems)
                {
                    string tableName = FindTableWithFabrickiKod(item.Fabricki_kod);

                    if (tableName != null)
                    {
                        int currentQuantity = GetCurrentQuantity(tableName, item.Fabricki_kod);
                        item.Kolicina = currentQuantity;
                        SelectedItemsDataGrid.Items.Refresh();

                        int newQuantity = currentQuantity - item.KolicinaZaNarucivanje;
                        if (newQuantity < 0)
                        {
                            // Ako količina pređe ispod 0, prikaži poruku i postavi flag
                            MessageBox.Show($"Nema dovoljno zaliha za {item.Opis}.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                            allQuantitiesValid = false;
                            break;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Fabrički kod {item.Fabricki_kod} nije pronađen.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                        allQuantitiesValid = false;
                        break;
                    }
                }

                // Ako su sve količine validne, brišemo redove i nastavljamo sa operacijom
                if (allQuantitiesValid)
                {
                    // Ažuriraj bazu
                    foreach (var item in selectedItems)
                    {
                        string tableName = FindTableWithFabrickiKod(item.Fabricki_kod);
                        if (tableName != null)
                        {
                            int newQuantity = GetCurrentQuantity(tableName, item.Fabricki_kod) - item.KolicinaZaNarucivanje;
                            using (var command = new MySqlCommand($"UPDATE {tableName} SET Kolicina = @Kolicina WHERE Fabricki_kod = @Fabricki_kod", connection))
                            {
                                command.Parameters.AddWithValue("@Kolicina", newQuantity);
                                command.Parameters.AddWithValue("@Fabricki_kod", item.Fabricki_kod);
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    // Export podataka u Excel
                    GenerateExcelFile(selectedItems);

                    // Osveži podatke u FormiranjePonudeView
                    _formiranjePonudeView.RefreshData();

                    // Brišemo stavke iz gridova samo ako je sve validno
                    if (_formiranjePonudeView.GroupedItems != null)
                    {
                        _formiranjePonudeView.GroupedItems.Clear();
                    }

                    if (SelectedItemsDataGrid.ItemsSource is List<PonudaItem> selectedList)
                    {
                        selectedList.Clear();
                        SelectedItemsDataGrid.Items.Refresh();
                    }
                }
            }
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
            // Retrieve all table names from the database
            var tablesToUpdate = GetAllTableNames();

            foreach (var item in items)
            {
                item.Kolicina = brojKomada;
                bool updated = false;

                foreach (var table in tablesToUpdate)
                {
                    // Check if the table contains the 'Fabricki_kod' column and 'Kolicina' column
                    if (TableContainsFabrickiKod(table, item.Fabricki_kod) && TableContainsColumn(table, "Kolicina"))
                    {
                        // Retrieve the current quantity
                        int currentQuantity = GetCurrentQuantity(table, item.Fabricki_kod);

                        // Calculate the new quantity
                        int newQuantity = currentQuantity + brojKomada;

                        using (var command = new MySqlCommand($"UPDATE {table} SET Kolicina = @Kolicina WHERE Fabricki_kod = @Fabricki_kod", connection))
                        {
                            command.Parameters.AddWithValue("@Kolicina", newQuantity);
                            command.Parameters.AddWithValue("@Fabricki_kod", item.Fabricki_kod);
                            command.ExecuteNonQuery();
                            updated = true;
                            break; // If updated, break out of the loop
                        }
                    }
                }

                if (!updated)
                {
                    MessageBox.Show($"Fabrički kod {item.Fabricki_kod} nije pronađen ili kolona 'Količina' nedostaje u nekoj od tabela.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        // Helper method to get all table names from the database
        private List<string> GetAllTableNames()
        {
            var tableNames = new List<string>();

            using (var command = new MySqlCommand("SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'myappdb';", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tableNames.Add(reader.GetString(0));
                    }
                }
            }

            return tableNames;
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

        private void GenerateExcel_Click(object sender, RoutedEventArgs e)
        {
            GenerateExcelFile(selectedItems);
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
                worksheet.Cells[1, 13].Value = "KolicinaZaNarucivanje";

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
                    worksheet.Cells[row, 13].Value = item.KolicinaZaNarucivanje;

                    row++;
                }

                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files (*.xlsx)|*.xlsx",
                    Title = "Sačuvajte Excel fajl",
                    FileName = "Ponuda.xlsx" // Default file name
                };

                // Show the dialog and wait for user input
                bool? result = saveFileDialog.ShowDialog();

                if (result == true)
                {
                    var filePath = saveFileDialog.FileName;
                    File.WriteAllBytes(filePath, package.GetAsByteArray());

                    MessageBox.Show($"Excel fajl je kreiran u: {filePath}");
                }
            }
        }
    }
}