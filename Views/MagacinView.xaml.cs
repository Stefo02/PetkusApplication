﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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
            // Table mapping remains the same
            { "d_se_bimetali", "Bimetali D SE " },
            { "d_se_dodatna_oprema", "Dodatna oprema D SE" },
            { "d_se_osiguraci", "Osigurači D SE" },
            { "d_se_sklopke", "Sklopke D SE" },
            { "d_se_tesys_control", "Tesys Control D SE" },
            { "d_se_zastita", "Zaštita D SE" },
            { "d_si_dodatna_oprema", "Dodatna oprema D SI" },
            { "d_si_sklopke", "Sklopke D SI" },
            { "d_si_zastita", "Zaštita D SI" },
            { "fc_d_dodatna_oprema", "Dodatna oprema FC D" },
            { "fc_d_frekventni_regulatori", "Frekventni regulatori FC D" },
            { "fc_d_zastita", "Zaštita FC D" },
            { "fc_se_dodatna_oprema", "Dodatna oprema FC SE" },
            { "fc_se_frekventni_regulatori", "Frekventni regulatori FC SE" },
            { "fc_se_zastita", "Zaštita FC SE" },
            { "fc_si_dodatna_oprema", "Dodatna oprema FC SI" },
            { "fc_si_frekventni_regulatori", "Frekventni regulatori FC SI" },
            { "fc_si_zastita", "Zaštita FC SI" },
            { "sklopka_d_se", "Sklopka D SE" },
            { "sklopka_d_si", "Sklopka D SI" },
            { "soft_bimetali", "Bimetali Soft" },
            { "soft_dodatna_oprema", "Dodatna oprema Soft" },
            { "soft_sklopke", "Sklopke Soft" },
            { "soft_osiguraci", "Osigurači Soft" },
            { "soft_starteri", "Starteri Soft" },
            { "soft_zastita", "Zaštita Soft" },
            { "yd_se_bimetali", "Bimetali YD SE" },
            { "yd_se_dodatna_oprema", "Dodatna oprema YD SE" },
            { "yd_se_osiguraci", "Osigurači YD SE" },
            { "yd_se_sklopke", "Sklopke YD SE" },
            { "yd_se_zastita", "Zaštita YD SE" },
            { "yd_si_dodatna_oprema", "Dodatna oprema YD SI" },
            { "yd_si_sklopke", "Sklopke YD SI" },
            { "yd_si_yd_kombinacija", "Kombinacija YD SI" },
            { "yd_si_zastita", "Zaštita YD SI" },
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
            ApplyRowStyle();
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

        private void ApplyRowStyle()
        {
            var style = new Style(typeof(DataGridRow));
            var trigger = new DataTrigger
            {
                Binding = new Binding("Kolicina")
                {
                    RelativeSource = new RelativeSource(RelativeSourceMode.Self)
                },
                Value = 0
            };

            trigger.Setters.Add(new Setter
            {
                Property = DataGridRow.BackgroundProperty,
                Value = Brushes.LightCoral
            });

            style.Triggers.Add(trigger);
            dataGrid.RowStyle = style;
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

            CheckForLowStock();
        }

        private void CheckForLowStock()
        {
            var lowStockItems = data.Where(item => item.Kolicina < item.MinKolicina).ToList();

            if (lowStockItems.Any())
            {
                foreach (var item in lowStockItems)
                {
                    var index = dataGrid.Items.IndexOf(item);
                    var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
                    if (row != null)
                    {
                        row.Background = Brushes.LightCoral;
                    }
                }

                MessageBox.Show("Neki redovi imaju količinu ispod minimalne količine. Molimo proverite!", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
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

            CheckForLowStock();
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(sender, e);
            }
        }

        private bool isUpdatingSelection = false;

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSelection) return; // Prevent recursive calls

            if (dataGrid.SelectedItem != null)
            {
                isUpdatingSelection = true;
                selectedItem = dataGrid.SelectedItem as Item;

                if (selectedItem != null)
                {
                    opisTextBox.Text = selectedItem.Opis ?? string.Empty;
                    proizvodjacTextBox.Text = selectedItem.Proizvodjac ?? string.Empty;
                    fabrickiKodTextBox.Text = selectedItem.Fabricki_kod ?? string.Empty;
                    kolicinaTextBox.Text = selectedItem.Kolicina.ToString();
                    punaCenaTextBox.Text = selectedItem.Puna_cena.ToString();
                    dimenzijeTextBox.Text = selectedItem.Dimenzije ?? string.Empty;
                    tezinaTextBox.Text = selectedItem.Tezina.ToString();
                    vrednostRabataTextBox.Text = selectedItem.Vrednost_rabata.ToString();
                    minKolicinaTextBox.Text = selectedItem.MinKolicina.ToString();

                    // Apply visual indicator if Kolicina is less than MinKolicina
                    if (selectedItem.Kolicina < selectedItem.MinKolicina)
                    {
                        dataGrid.SelectedItem = null; // Deselect the current item
                        dataGrid.Focus(); // Refocus on the DataGrid
                        dataGrid.SelectedItem = selectedItem; // Re-select the item
                        MessageBox.Show("Količina u ovom redu je ispod minimalne količine.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            else
            {
                ClearTextBoxes();
            }

            isUpdatingSelection = false;
        }


        private void kolicinaTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(kolicinaTextBox.Text, out int kolicina) && int.TryParse(minKolicinaTextBox.Text, out int minKolicina))
            {
                var row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(dataGrid.SelectedIndex);
                if (row != null && kolicina < minKolicina)
                {
                    row.Background = Brushes.LightCoral;
                }
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
                Vrednost_rabata = decimal.Parse(vrednostRabataTextBox.Text),
                MinKolicina = int.Parse(minKolicinaTextBox.Text)
            };

            string tableName = GetTableNameFromComboBox();
            dbContext.AddItem(tableName, newItem);

            ClearTextBoxes();
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
                selectedItem.MinKolicina = int.Parse(minKolicinaTextBox.Text);

                string tableName = GetTableNameFromComboBox();
                dbContext.UpdateItem(tableName, selectedItem);

                ClearTextBoxes();
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

                ClearTextBoxes();
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
            minKolicinaTextBox.Text = string.Empty; // Added clearing
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
            CheckForLowStock();
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
