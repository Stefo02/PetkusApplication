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
using System.Collections.Generic;

namespace PetkusApplication.Views
{
    public partial class MagacinView : UserControl
    {
        private AppDbContext dbContext;
        private List<Item> data;
        private Item selectedItem;
        private DispatcherTimer stockCheckTimer;
        private bool isNotificationShown = false;

        private Dictionary<string, string> tableMap = new Dictionary<string, string>
        {
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
            { "kleme", "Kleme" },
            { "ormani", "Ormani" },
            { "napajanje_23", "Napajanje 23" },
            { "napajanje_230", "Napajanje 230" },
            { "prekidaci_se", "Prekidači SE" },
            { "prekidaci_si", "Prekidači SI" },
            { "sabirnicki_sistem", "Sabirnički sistem" },
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

            stockCheckTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromHours(4) // Postavi interval na 4 sata
            };
            stockCheckTimer.Tick += StockCheckTimer_Tick;
            stockCheckTimer.Start(); // Pokreni timer
            CheckForLowStock();
        }

        private void MagacinView_Loaded(object sender, RoutedEventArgs e)
        {
            // Postavi "Svi podaci" kao selektovanu opciju
            tableComboBox.SelectedItem = "Svi podaci";

            // Učitaj podatke na osnovu selektovane opcije
            LoadData();

            // Ažuriraj obaveštenja o niskim zalihama odmah nakon učitavanja podataka
            CheckForLowStock();
        }

        private void LowStockList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lowStockList.SelectedItem is Item selectedItem)
            {
                // Prekopiraj opis iz izabrane stavke u searchTextBox
                searchTextBox.Text = selectedItem.Opis ?? string.Empty;
            }
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
            // Preuzmi artikle sa niskim zalihama
            var lowStockItems = data.Where(item => item.Kolicina < item.MinKolicina).ToList();

            // Ažuriraj broj obaveštenja na zvoncu
            notificationCount.Text = lowStockItems.Count.ToString();

            // Ako ima artikala sa niskim zalihama, postavi ih u listu popup-a
            if (lowStockItems.Any())
            {
                lowStockList.ItemsSource = lowStockItems;
                isNotificationShown = true;
            }
            else
            {
                isNotificationShown = false;
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
                    disipacijaTextBox.Text = selectedItem.Disipacija.ToString();
                    vrednostRabataTextBox.Text = selectedItem.Vrednost_rabata.ToString();
                    minKolicinaTextBox.Text = selectedItem.MinKolicina.ToString();
                    jedinicamereComboBox.Text = selectedItem.JedinicaMere.ToString();
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
            if (tableComboBox.SelectedItem.ToString() == "Svi podaci")
            {
                MessageBox.Show("Nije moguće dodati nove podatke dok ne odaberete specifičnu tabelu.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Preuzimanje izabrane vrednosti iz ComboBox-a
            string jedinicaMere = (jedinicamereComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (string.IsNullOrEmpty(jedinicaMere))
            {
                MessageBox.Show("Molimo izaberite jedinicu mere.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validacija unosa za Kolicina (int)
            if (!int.TryParse(kolicinaTextBox.Text, out int kolicina))
            {
                MessageBox.Show("Uneta količina nije ispravna. Molimo unesite ceo broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validacija unosa za Puna cena (decimal)
            if (!decimal.TryParse(punaCenaTextBox.Text, out decimal punaCena))
            {
                MessageBox.Show("Uneta puna cena nije ispravna. Molimo unesite decimalan broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validacija unosa za Tezina (decimal)
            if (!decimal.TryParse(tezinaTextBox.Text, out decimal tezina))
            {
                MessageBox.Show("Uneta težina nije ispravna. Molimo unesite decimalan broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validacija unosa za Tezina (decimal)
            if (!decimal.TryParse(disipacijaTextBox.Text, out decimal disipacija))
            {
                MessageBox.Show("Uneta disipacija nije ispravna. Molimo unesite decimalan broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validacija unosa za Vrednost rabata (decimal)
            if (!decimal.TryParse(vrednostRabataTextBox.Text, out decimal vrednostRabata))
            {
                MessageBox.Show("Uneta vrednost rabata nije ispravna. Molimo unesite decimalan broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validacija unosa za MinKolicina (int)
            if (!int.TryParse(minKolicinaTextBox.Text, out int minKolicina))
            {
                MessageBox.Show("Uneta minimalna količina nije ispravna. Molimo unesite ceo broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Kreiranje novog Item-a
            var newItem = new Item
            {
                Opis = opisTextBox.Text,
                Proizvodjac = proizvodjacTextBox.Text,
                Fabricki_kod = fabrickiKodTextBox.Text,
                Kolicina = kolicina,
                Puna_cena = punaCena,
                Dimenzije = dimenzijeTextBox.Text,
                Tezina = tezina,
                Disipacija = disipacija,
                Vrednost_rabata = vrednostRabata,
                MinKolicina = minKolicina,


                // Ovde dodaj izabranu jedinicu mere
                JedinicaMere = jedinicaMere  // Dodavanje jedinice mere
            };

            // Dodaj novi element u bazu
            string tableName = GetTableNameFromComboBox();
            dbContext.AddItem(tableName, newItem);

            // Kreiranje audit log zapisa
            int currentUserId = GetCurrentUserId(); // Dobij ID trenutnog korisnika
            string currentUsername = GetCurrentUsername(); // Dodajte metodu koja vraća trenutnog korisnika
            var auditLog = new AuditLog
            {
                UserId = currentUserId,
                Action = "Dodato",
                TableAffected = tableName,
                RecordId = newItem.Id, // Pretpostavljamo da newItem ima svoj ID
                Timestamp = DateTime.Now,
                OldValue = string.Empty, // Nema stare vrednosti, jer je nova stavka
                NewValue = newItem.ToString(), // Ovdje bi trebalo konvertovati u string, zavisno od tvoje implementacije
                Username = currentUsername
            };

            // Sačuvaj audit log
            dbContext.SaveAuditLog(auditLog);

            // Očisti polja nakon dodavanja
            ClearTextBoxes();
            LoadData();

            // Resetuj ComboBox nakon dodavanja
            jedinicamereComboBox.SelectedIndex = -1;

            // Provera zaliha nakon dodavanja stavke
            CheckForLowStock();
        }

        private string GetCurrentUsername()
        {
            // Pretpostavljamo da imate neku globalnu promenljivu ili metodu koja vraća trenutnog korisnika
            return App.CurrentUser?.Username ?? "Nepoznat korisnik"; // Vraća "Nepoznat korisnik" ako nema trenutnog korisnika
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem != null)
            {
                // Preuzmite staru verziju podataka pre ažuriranja
                var oldData = dbContext.GetItemsFromTable(selectedItem.OriginalTable)
                    .FirstOrDefault(i => i.Id == selectedItem.Id);

                selectedItem.Opis = opisTextBox.Text;
                selectedItem.Proizvodjac = proizvodjacTextBox.Text;
                selectedItem.Fabricki_kod = fabrickiKodTextBox.Text;

                // Validacija unosa za Kolicina (int)
                if (int.TryParse(kolicinaTextBox.Text, out int kolicina))
                {
                    selectedItem.Kolicina = kolicina;
                }
                else
                {
                    MessageBox.Show("Uneta količina nije ispravna. Molimo unesite ceo broj.");
                    return;
                }

                // Validacija unosa za Puna cena (decimal)
                if (decimal.TryParse(punaCenaTextBox.Text, out decimal punaCena))
                {
                    selectedItem.Puna_cena = punaCena;
                }
                else
                {
                    MessageBox.Show("Uneta puna cena nije ispravna. Molimo unesite decimalan broj.");
                    return;
                }

                selectedItem.Dimenzije = dimenzijeTextBox.Text;

                // Validacija unosa za Tezina (decimal)
                if (decimal.TryParse(tezinaTextBox.Text, out decimal tezina))
                {
                    selectedItem.Tezina = tezina;
                }
                else
                {
                    MessageBox.Show("Uneta težina nije ispravna. Molimo unesite decimalan broj.");
                    return;
                }

                // Validacija unosa za Disipacija (decimal)
                if (decimal.TryParse(disipacijaTextBox.Text, out decimal disipacija))
                {
                    selectedItem.Disipacija = disipacija;
                }
                else
                {
                    MessageBox.Show("Uneta disipacija nije ispravna. Molimo unesite decimalan broj.");
                    return;
                }


                // Validacija unosa za Vrednost rabata (decimal)
                if (decimal.TryParse(vrednostRabataTextBox.Text, out decimal vrednostRabata))
                {
                    selectedItem.Vrednost_rabata = vrednostRabata;
                }
                else
                {
                    MessageBox.Show("Uneta vrednost rabata nije ispravna. Molimo unesite decimalan broj.");
                    return;
                }

                // Validacija unosa za MinKolicina (int)
                if (int.TryParse(minKolicinaTextBox.Text, out int minKolicina))
                {
                    selectedItem.MinKolicina = minKolicina;
                }
                else
                {
                    MessageBox.Show("Uneta minimalna količina nije ispravna. Molimo unesite ceo broj.");
                    return;
                }

                // Preuzimanje vrednosti iz ComboBox-a
                string jedinicaMere = (jedinicamereComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (!string.IsNullOrEmpty(jedinicaMere))
                {
                    selectedItem.JedinicaMere = jedinicaMere;  // Ažuriranje vrednosti JedinicaMere
                }

                string tableName = !string.IsNullOrEmpty(selectedItem.OriginalTable)
                    ? selectedItem.OriginalTable
                    : GetTableNameFromComboBox();

                if (!string.IsNullOrEmpty(tableName))
                {
                    dbContext.UpdateItemAcrossTables(selectedItem);

                    // Preuzmite ID trenutno prijavljenog korisnika
                    int currentUserId = GetCurrentUserId();

                    // Dohvati korisnika kako bi dobio username
                    string username = dbContext.Users
                        .Where(u => u.Id == currentUserId)
                        .Select(u => u.Username)
                        .FirstOrDefault();

                    // Kreirajte audit log zapis
                    var oldValues = GetOldValues(oldData, selectedItem);  // Samo stare vrednosti
                    var newValues = GetNewValues(oldData, selectedItem);  // Samo nove vrednosti

                    var auditLog = new AuditLog
                    {
                        UserId = currentUserId,  // Ovaj parametar je korisnički ID
                        Action = "Ažurirano",
                        TableAffected = tableName,
                        RecordId = selectedItem.Id,
                        Timestamp = DateTime.Now,
                        OldValue = oldValues,  // Konvertujte staru vrednost u string
                        NewValue = newValues,  // Konvertujte novu vrednost u string
                        Username = username
                    };

                    dbContext.SaveAuditLog(auditLog);

                    ClearTextBoxes();
                    LoadData();
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

        private int GetCurrentUserId()
        {
            if (App.CurrentUser != null)
            {
                return App.CurrentUser.Id;
            }
            return 0;
        }

        public string GetOldValues(Item oldItem, Item newItem)
        {
            var oldValues = new Dictionary<string, object>();

            if (oldItem.Opis != newItem.Opis)
                oldValues.Add(nameof(oldItem.Opis), oldItem.Opis);

            if (oldItem.Proizvodjac != newItem.Proizvodjac)
                oldValues.Add(nameof(oldItem.Proizvodjac), oldItem.Proizvodjac);

            if (oldItem.Fabricki_kod != newItem.Fabricki_kod)
                oldValues.Add(nameof(oldItem.Fabricki_kod), oldItem.Fabricki_kod);

            if (oldItem.Kolicina != newItem.Kolicina)
                oldValues.Add(nameof(oldItem.Kolicina), oldItem.Kolicina);

            if (oldItem.Puna_cena != newItem.Puna_cena)
                oldValues.Add(nameof(oldItem.Puna_cena), oldItem.Puna_cena);

            if (oldItem.Dimenzije != newItem.Dimenzije)
                oldValues.Add(nameof(oldItem.Dimenzije), oldItem.Dimenzije);

            if (oldItem.Tezina != newItem.Tezina)
                oldValues.Add(nameof(oldItem.Tezina), oldItem.Tezina);

            if (oldItem.Disipacija != newItem.Disipacija)
                oldValues.Add(nameof(oldItem.Disipacija), oldItem.Disipacija);

            if (oldItem.Vrednost_rabata != newItem.Vrednost_rabata)
                oldValues.Add(nameof(oldItem.Vrednost_rabata), oldItem.Vrednost_rabata);

            if (oldItem.MinKolicina != newItem.MinKolicina)
                oldValues.Add(nameof(oldItem.MinKolicina), oldItem.MinKolicina);

            if (oldItem.JedinicaMere != newItem.JedinicaMere)
                oldValues.Add(nameof(oldItem.JedinicaMere), oldItem.JedinicaMere);

            // Konvertujte stare vrednosti u JSON
            return JsonSerializer.Serialize(oldValues);
        }

        public string GetNewValues(Item oldItem, Item newItem)
        {
            var newValues = new Dictionary<string, object>();

            if (oldItem.Opis != newItem.Opis)
                newValues.Add(nameof(newItem.Opis), newItem.Opis);

            if (oldItem.Proizvodjac != newItem.Proizvodjac)
                newValues.Add(nameof(newItem.Proizvodjac), newItem.Proizvodjac);

            if (oldItem.Fabricki_kod != newItem.Fabricki_kod)
                newValues.Add(nameof(newItem.Fabricki_kod), newItem.Fabricki_kod);

            if (oldItem.Kolicina != newItem.Kolicina)
                newValues.Add(nameof(newItem.Kolicina), newItem.Kolicina);

            if (oldItem.Puna_cena != newItem.Puna_cena)
                newValues.Add(nameof(newItem.Puna_cena), newItem.Puna_cena);

            if (oldItem.Dimenzije != newItem.Dimenzije)
                newValues.Add(nameof(newItem.Dimenzije), newItem.Dimenzije);

            if (oldItem.Tezina != newItem.Tezina)
                newValues.Add(nameof(newItem.Tezina), newItem.Tezina);

            if (oldItem.Disipacija != newItem.Disipacija)
                newValues.Add(nameof(newItem.Disipacija), newItem.Disipacija);

            if (oldItem.Vrednost_rabata != newItem.Vrednost_rabata)
                newValues.Add(nameof(newItem.Vrednost_rabata), newItem.Vrednost_rabata);

            if (oldItem.MinKolicina != newItem.MinKolicina)
                newValues.Add(nameof(newItem.MinKolicina), newItem.MinKolicina);

            if (oldItem.JedinicaMere != newItem.JedinicaMere)
                newValues.Add(nameof(newItem.JedinicaMere), newItem.JedinicaMere);

            // Konvertujte nove vrednosti u JSON
            return JsonSerializer.Serialize(newValues);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedItem != null)
            {
                string tableName = GetTableNameFromComboBox();

                // Uzmite trenutnog korisnika za audit log
                int currentUserId = GetCurrentUserId();
                string currentUsername = GetCurrentUsername();

                // Kreirajte audit log pre brisanja
                var auditLog = new AuditLog
                {
                    UserId = currentUserId,
                    Action = "Obrisano",
                    TableAffected = tableName,
                    RecordId = selectedItem.Id,
                    Timestamp = DateTime.Now,
                    OldValue = selectedItem.ToString(), // Ovde konvertujte u string ako je potrebno
                    NewValue = string.Empty, // Nema nove vrednosti, jer se stavka briše
                    Username = currentUsername
                };

                // Sačuvajte audit log pre nego što obrišete stavku
                dbContext.SaveAuditLog(auditLog);

                // Obrisi stavku
                dbContext.DeleteItem(tableName, selectedItem.Id);

                ClearTextBoxes();
                LoadData();

                // Proverite zalihe nakon brisanja stavke
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
            disipacijaTextBox.Text = string.Empty;
            vrednostRabataTextBox.Text = string.Empty;
            minKolicinaTextBox.Text = string.Empty;
            jedinicamereComboBox.Text = string.Empty;
        }

        private void LoadData()
        {
            string tableName = GetTableNameFromComboBox();

            if (string.IsNullOrEmpty(tableName) || tableName == "Svi podaci")
            {
                // Preuzmi sve podatke iz svih tabela
                data = dbContext.GetItemsFromAllTables();
            }
            else
            {
                // Preuzmi podatke iz specifične tabele (ali NE koristi UpdateItemAcrossTables)
                data = dbContext.GetItemsFromTable(tableName); // Ova metoda vraća podatke iz odabrane tabele
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
                Title = "Sačuvajte izabrane redove kao Excel fajl."
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
                    worksheet.Cell(1, 8).Value = "Disipacija";
                    worksheet.Cell(1, 9).Value = "Vrednost rabata";
                    worksheet.Cell(1, 10).Value = "Min Kolicina";
                    worksheet.Cell(1, 11).Value = "Kolicina za narucivanje";

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
                        worksheet.Cell(i + 2, 8).Value = item.Disipacija;
                        worksheet.Cell(i + 2, 9).Value = item.Vrednost_rabata;
                        worksheet.Cell(i + 2, 10).Value = item.MinKolicina;
                    }

                    // Save the workbook
                    workbook.SaveAs(filePath);
                }

                MessageBox.Show("Excel fajl je uspešno sačuvan.");
            }
        }
    }
}