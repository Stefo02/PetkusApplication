using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using PetkusApplication.Data;
using PetkusApplication.Models;
using ClosedXML.Excel;
using System.IO;
using System.Text.Json;

namespace PetkusApplication.Views
{
    public partial class MagacinView : UserControl
    {
        private AppDbContext dbContext;
        private List<Item> data;
        private Item selectedItem;
        private DispatcherTimer stockCheckTimer;
        private bool isNotificationShown = false;
        private const string AppDataFolder = "PetkusApplication";
        private const string DataFileName = "ComboBoxSelections.json";


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
            { "fc_se_frekventni_regulator", "Frekventni regulatori FC SE" },
            { "fc_se_zastita", "Zaštita FC SE" },
            { "fc_si_dodatna_oprema", "Dodatna oprema FC SI" },
            { "fc_si_frekventni_regulator", "Frekventni regulatori FC SI" },
            { "fc_si_zastita", "Zaštita FC SI" },
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
        

        public MagacinView()
        {
            InitializeComponent();
            dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql("server=localhost;database=myappdb;user=root;password=;", new MySqlServerVersion(new Version(10, 4, 32)))
                .Options); // Initialize AppDbContext with options

            data = new List<Item>();  // Inicijalizacija liste pre upotrebe

            dataGrid.ItemsSource = data;
            LoadTableComboBox();
            ApplyRowStyle();
            LoadComboBoxSelections();

            Application.Current.Exit += OnApplicationExit;

            stockCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(4) // Postavi interval na 4 sata
            };
            stockCheckTimer.Tick += StockCheckTimer_Tick;
            stockCheckTimer.Start(); // Pokreni timer
            CheckForLowStock();
        }

        private void OnApplicationExit(object sender, ExitEventArgs e)
        {
            SaveComboBoxSelections(); // Sačuvaj selekcije pre zatvaranja aplikacije
        }

        private void StockCheckTimer_Tick(object sender, EventArgs e)
        {
            CheckForLowStock(); // Pozivanje metode za proveru niske količine
        }

        private void LoadTableComboBox()
        {
            var tableNames = dbContext.GetTablesWithColumns();
            var allDataOption = "Svi podaci"; // Option for all data

            // Add the 'All Data' option at the beginning of the list
            var tableOptions = new List<string> { allDataOption };
            tableOptions.AddRange(tableNames.Select(t => tableMap.ContainsKey(t) ? tableMap[t] : t)); // Add the actual table names

            tableComboBox.ItemsSource = tableOptions;

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
                data = dbContext.GetItemsFromAllTables(); // Preuzmi podatke iz svih tabela
            }
            else
            {
                string tableName = GetTableNameFromComboBox();
                data = dbContext.GetItemsFromTable(tableName); // Preuzmi podatke iz izabrane tabele
            }

            dataGrid.ItemsSource = data;
            dataGrid.Items.Refresh();

            // Pozovi CheckForLowStock da bi se broj obaveštenja odmah ažurirao
            CheckForLowStock();
        }



        private DateTime lastWarningTime = DateTime.MinValue;

        private void CheckForLowStock()
        {
            var lowStockItems = data.Where(item => item.Kolicina < item.MinKolicina).ToList();

            // Ažuriraj broj obaveštenja
            notificationCount.Text = lowStockItems.Count.ToString();

            // Ako ima artikala sa niskim zalihama, popup može ostati zatvoren dok se ne klikne
            if (lowStockItems.Any())
            {
                lowStockList.ItemsSource = lowStockItems; // Postavi listu artikala u popup
                isNotificationShown = true; // Postavi zastavicu da je obaveštenje aktivno
            }
            else
            {
                isNotificationShown = false; // Resetuj zastavicu kada nema zaliha
            }
        }



        private void NotificationBell_Click(object sender, RoutedEventArgs e)
        {
            // Prikupi stavke sa niskim zalihama
            var lowStockItems = data.Where(item => item.Kolicina < item.MinKolicina).ToList();

            // Ažuriraj listu u popup prozoru
            lowStockList.ItemsSource = lowStockItems;

            // Ažuriraj broj obaveštenja pored zvonca
            notificationCount.Text = lowStockItems.Count.ToString();

            // Prikaži popup iznad zvonca
            notificationPopup.IsOpen = true;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
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

        private bool isUpdatingSelection = false;

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isUpdatingSelection) return; // Spreči rekurzivne pozive

            if (dataGrid.SelectedItem != null)
            {
                isUpdatingSelection = true;
                selectedItem = dataGrid.SelectedItem as Item;

                if (selectedItem != null)
                {
                    // Postavljanje vrednosti u TextBox-ove
                    opisTextBox.Text = selectedItem.Opis ?? string.Empty;
                    proizvodjacTextBox.Text = selectedItem.Proizvodjac ?? string.Empty;
                    fabrickiKodTextBox.Text = selectedItem.Fabricki_kod ?? string.Empty;
                    kolicinaTextBox.Text = selectedItem.Kolicina.ToString();
                    punaCenaTextBox.Text = selectedItem.Puna_cena.ToString();
                    dimenzijeTextBox.Text = selectedItem.Dimenzije ?? string.Empty;
                    tezinaTextBox.Text = selectedItem.Tezina.ToString();
                    vrednostRabataTextBox.Text = selectedItem.Vrednost_rabata.ToString();
                    minKolicinaTextBox.Text = selectedItem.MinKolicina.ToString();

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
            // Provera da li je selektovana tabela "Svi podaci"
            if (tableComboBox.SelectedItem.ToString() == "Svi podaci")
            {
                MessageBox.Show("Nije moguće dodati nove podatke dok ne odaberete specifičnu tabelu.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kreiranje novog Item objekta
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

            // Preuzimanje imena selektovane tabele
            string tableName = GetTableNameFromComboBox();
            dbContext.AddItem(tableName, newItem);

            ClearTextBoxes();
            LoadData();

            // Pozivamo funkciju za proveru zaliha nakon dodavanja nove stavke
            CheckForLowStock();
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

                string tableName = !string.IsNullOrEmpty(selectedItem.OriginalTable)
                    ? selectedItem.OriginalTable
                    : GetTableNameFromComboBox();

                if (!string.IsNullOrEmpty(tableName))
                {
                    dbContext.UpdateItem(tableName, selectedItem);
                    ClearTextBoxes();
                    LoadData();

                    // Pozivamo funkciju za proveru zaliha nakon ažuriranja
                    CheckForLowStock();
                }
                else
                {
                    MessageBox.Show("Nije moguće odrediti ime tabele za izabranu stavku.");
                }
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

                // Pozivamo funkciju za proveru zaliha nakon brisanja stavke
                CheckForLowStock();
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
        }
       
        private string GetTableNameFromComboBox()
        {
            var selectedOption = tableComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedOption) || selectedOption == "Svi podaci")
                return null;

            return tableMap.FirstOrDefault(x => x.Value == selectedOption).Key;
        }

        private void SaveToExcelButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSelectedRowsToExcel();
        }

        private void SaveSelectedRowsToExcel()
        {
            // Get selected rows
            var selectedItems = dataGrid.SelectedItems.Cast<Item>().ToList();

            if (selectedItems.Count == 0)
            {
                MessageBox.Show("Nema izabranih redova.");
                return;
            }

            // Open the SaveFileDialog only if there are selected rows
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = "Save Selected Rows as Excel File"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("SelectedRows");

                    // Add headers
                    worksheet.Cell(1, 1).Value = "Opis";
                    worksheet.Cell(1, 2).Value = "Proizvodjac";
                    worksheet.Cell(1, 3).Value = "Fabricki kod";
                    worksheet.Cell(1, 4).Value = "Kolicina";
                    worksheet.Cell(1, 5).Value = "Puna cena";
                    worksheet.Cell(1, 6).Value = "Dimenzije";
                    worksheet.Cell(1, 7).Value = "Tezina";
                    worksheet.Cell(1, 8).Value = "Vrednost rabata";
                    worksheet.Cell(1, 9).Value = "Min Kolicina";
                    worksheet.Cell(1, 10).Value = "Kolicina za narucivanje";

                    // Add data rows
                    for (int i = 0; i < selectedItems.Count; i++)
                    {
                        var item = selectedItems[i];
                        worksheet.Cell(i + 2, 1).Value = item.Opis;
                        worksheet.Cell(i + 2, 2).Value = item.Proizvodjac;
                        worksheet.Cell(i + 2, 3).Value = item.Fabricki_kod;
                        worksheet.Cell(i + 2, 4).Value = item.Kolicina;
                        worksheet.Cell(i + 2, 5).Value = item.Puna_cena;
                        worksheet.Cell(i + 2, 6).Value = item.Dimenzije;
                        worksheet.Cell(i + 2, 7).Value = item.Tezina;
                        worksheet.Cell(i + 2, 8).Value = item.Vrednost_rabata;
                        worksheet.Cell(i + 2, 9).Value = item.MinKolicina;
                    }

                    // Save the workbook
                    workbook.SaveAs(filePath);
                }

                MessageBox.Show("Excel fajl je uspesno sacuvan.");
            }
        }

        private string GetAppDataPath()
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDataFolder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(folderPath, DataFileName);
        }

        private void SaveComboBoxSelections()
        {
            // Grupisanje po 'Id' i uzimanje prvog elementa iz svake grupe kako bi se izbegli duplikati
            var selections = dataGrid.ItemsSource.Cast<Item>()
                .GroupBy(item => item.Id)
                .ToDictionary(group => group.Key, group => group.First().JedinicaMere);

            string json = JsonSerializer.Serialize(selections);
            File.WriteAllText(GetAppDataPath(), json);
        }


        private void LoadComboBoxSelections()
        {
            if (File.Exists(GetAppDataPath()))
            {
                string json = File.ReadAllText(GetAppDataPath());
                var selections = JsonSerializer.Deserialize<Dictionary<int, string>>(json);

                foreach (var item in dataGrid.ItemsSource.Cast<Item>())
                {
                    if (selections.ContainsKey(item.Id))
                    {
                        item.JedinicaMere = selections[item.Id];
                    }
                }
            }
        }

    }
}
