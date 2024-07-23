using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class MagacinView : UserControl
    {
        private AppDbContext dbContext;
        private List<Item> data;
        private Item selectedItem;
        private Dictionary<string, string> tableMap = new Dictionary<string, string>
        {
            { "compact_nsxm_nsx_2021", "Compact NSXM NSX 2021" },
            { "dodatna_oprema_compact_nsxm_nsx_2021", "Dodatna oprema Compact NSXM NSX 2021" },
            { "dodatna_oprema_d_se", "Dodatna oprema D SE" },
            { "dodatna_oprema_d_si", "Dodatna oprema D SI" },
            { "dodatna_oprema_easypact_cvs_100_630", "Dodatna oprema Easypact CSV 100 630" },
            { "dodatna_oprema_ns", "Dodatna oprema NS" },
            { "dodatna_oprema_siemens", "Dodatna oprema Siemens" },
            { "easypact_cvs_prekidac", "Easypact CSV prekidac" },
            { "fc_d_zastita", "FC D zastita" },
            { "fc_se_zastita", "FC SE zaštita" },
            { "fc_si_zastita", "FC SI zaštita" },
            { "ins_100_630", "INS 100 630" },
            { "kompak_prekidaci_400_1600a_55ka", "Compact prekidači 400 1600a 55ka" },
            { "mtz2_dodatna_oprema", "MTZ2 dodatna oprema" },
            { "mtz2_prekidaci", "MTZ2 prekidači" },
            { "ns_prekidaci", "NS prekidači" },
            { "siemens", "Siemens" },
            { "siemens_pojedinacna_oprema", "Siemens pojedinačna oprema" },
            { "sklopka_d_se", "Sklopka D SE" },
            { "sklopka_d_si", "Sklopka D SI" },
            { "soft_bimetali", "Soft bimetali" },
            { "soft_dodatna_oprema", "Soft dodatna oprema" },
            { "soft_sklopke", "Soft sklopke" },
            { "soft_starteri", "Soft starteri" },
            { "soft_zastita", "Soft zaštita" },
            { "tesys_control", "Tesys control" },
            { "yd_se", "YD SE" },
            { "yd_si", "YD SI" },
            { "zastita_d_se", "Zaštita D SE" },
            { "zastita_d_si", "Zaštita D SI" }
        };
        private DispatcherTimer timer;

        public MagacinView()
        {
            InitializeComponent();
            dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql("server=localhost;database=myappdb;user=root;password=;", new MySqlServerVersion(new Version(10, 4, 32)))
                .Options); // Initialize AppDbContext with options

            data = new List<Item>();
            dataGrid.ItemsSource = data;
            LoadTableComboBox();
        }

        private void LoadTableComboBox()
        {
            var tableNames = dbContext.GetTablesWithColumns();
            var allDataOption = "Svi podaci"; // Option for all data

            // Add the 'All Data' option at the beginning of the list
            var tableOptions = new List<string> { allDataOption };
            tableOptions.AddRange(tableNames.Select(t => tableMap.ContainsKey(t) ? tableMap[t] : t)); // Add the actual table names

            tableComboBox.ItemsSource = tableOptions;
            tableComboBox.SelectedIndex = 0; // Set default selection to 'Svi podaci'
        }

        private void tableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedOption = tableComboBox.SelectedItem as string;

            if (selectedOption == null) return;

            if (selectedOption == "Svi podaci")
            {
                data = dbContext.GetItemsFromAllTables(); // Fetch data from all tables
            }
            else
            {
                string tableName = GetTableNameFromComboBox();
                data = dbContext.GetItemsFromTable(tableName); // Fetch data from the selected table
            }

            dataGrid.ItemsSource = data;
            dataGrid.Items.Refresh();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();

            if (data == null || !data.Any())
            {
                MessageBox.Show("Nema podataka za pretragu.");
                return;
            }

            var filteredData = data.Where(item =>
                (item.Opis != null && item.Opis.ToLower().Contains(searchText)) ||
                (item.Fabricki_kod != null && item.Fabricki_kod.ToLower().Contains(searchText))
            ).ToList();

            dataGrid.ItemsSource = filteredData;
            dataGrid.Items.Refresh();
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the DataGrid has a selected item
            if (dataGrid.SelectedItem != null)
            {
                selectedItem = dataGrid.SelectedItem as Item;

                if (selectedItem != null)
                {
                    // Populate the TextBoxes with the selected item's data
                    opisTextBox.Text = selectedItem.Opis ?? string.Empty;
                    proizvodjacTextBox.Text = selectedItem.Proizvodjac ?? string.Empty;
                    fabrickiKodTextBox.Text = selectedItem.Fabricki_kod ?? string.Empty;
                    kolicinaTextBox.Text = selectedItem.Kolicina.ToString(); // Assuming Kolicina is int
                    punaCenaTextBox.Text = selectedItem.Puna_cena.ToString(); // Assuming Puna_cena is decimal
                    dimenzijeTextBox.Text = selectedItem.Dimenzije ?? string.Empty;
                    tezinaTextBox.Text = selectedItem.Tezina.ToString(); // Assuming Tezina is int
                    vrednostRabataTextBox.Text = selectedItem.Vrednost_rabata.ToString(); // Assuming Vrednost_rabata is decimal
                }
            }
            else
            {
                // Clear the TextBoxes if no item is selected
                ClearTextBoxes();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Clear the DataGrid selection when clicking anywhere on the window
            dataGrid.SelectedItem = null;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new Item
            {
                Opis = opisTextBox.Text,
                Proizvodjac = proizvodjacTextBox.Text,
                Fabricki_kod = fabrickiKodTextBox.Text,
                Kolicina = int.Parse(kolicinaTextBox.Text),
                Puna_cena = decimal.Parse(punaCenaTextBox.Text),
                Dimenzije = dimenzijeTextBox.Text,
                Tezina = decimal.Parse(tezinaTextBox.Text),
                Vrednost_rabata = decimal.Parse(vrednostRabataTextBox.Text)
            };

            string tableName = GetTableNameFromComboBox();
            dbContext.AddItem(tableName, newItem);

            // Clear TextBoxes
            ClearTextBoxes();

            // Reload data
            LoadData();
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem != null)
            {
                selectedItem.Opis = opisTextBox.Text;
                selectedItem.Proizvodjac = proizvodjacTextBox.Text;
                selectedItem.Fabricki_kod = fabrickiKodTextBox.Text;
                selectedItem.Kolicina = int.Parse(kolicinaTextBox.Text);
                selectedItem.Puna_cena = decimal.Parse(punaCenaTextBox.Text);
                selectedItem.Dimenzije = dimenzijeTextBox.Text;
                selectedItem.Tezina = decimal.Parse(tezinaTextBox.Text);
                selectedItem.Vrednost_rabata = decimal.Parse(vrednostRabataTextBox.Text);

                string tableName = GetTableNameFromComboBox();
                dbContext.UpdateItem(tableName, selectedItem);

                // Clear TextBoxes
                ClearTextBoxes();

                // Reload data
                LoadData();
            }
            else
            {
                MessageBox.Show("Odaberite stavku za ažuriranje.");
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem != null)
            {
                string tableName = GetTableNameFromComboBox();
                dbContext.DeleteItem(tableName, selectedItem.Id);

                // Clear TextBoxes
                ClearTextBoxes();

                // Reload data
                LoadData();
            }
            else
            {
                MessageBox.Show("Odaberite stavku za brisanje.");
            }
        }


        private void ClearTextBoxes()
        {
            opisTextBox.Text = string.Empty;
            proizvodjacTextBox.Text = string.Empty;
            fabrickiKodTextBox.Text = string.Empty;
            kolicinaTextBox.Text = string.Empty;
            punaCenaTextBox.Text = string.Empty;
            dimenzijeTextBox.Text = string.Empty;
            tezinaTextBox.Text = string.Empty;
            vrednostRabataTextBox.Text = string.Empty;
        }

       
        private void LoadData()
        {
            string tableName = GetTableNameFromComboBox();
            if (string.IsNullOrEmpty(tableName) || tableName == "Svi podaci")
            {
                data = dbContext.GetItemsFromAllTables();
            }
            else
            {
                data = dbContext.GetItemsFromTable(tableName);
            }
            dataGrid.ItemsSource = data;
            dataGrid.Items.Refresh();
        }

        private string GetTableNameFromComboBox()
        {
            var selectedOption = tableComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedOption) || selectedOption == "Svi podaci")
                return null;

            return tableMap.FirstOrDefault(x => x.Value == selectedOption).Key;
        }
    }
}
