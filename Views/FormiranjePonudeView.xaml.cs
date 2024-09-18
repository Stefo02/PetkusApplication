using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MySql.Data.MySqlClient;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class FormiranjePonudeView : UserControl
    {
        private MySqlConnection connection;
        private Dictionary<string, Action> comboBoxSetups;
        private Dictionary<string, Dictionary<string, Action>> subComboBoxSetups;


        private Dictionary<int, ObservableCollection<PonudaItem>> groupedPonudaItems = new Dictionary<int, ObservableCollection<PonudaItem>>();
        private int currentGroupId = 0;

        public ObservableCollection<PonudaItem> PonudaItems { get; set; }
        public ObservableCollection<GroupedItem> GroupedItems { get; set; }
        private List<PonudaItem> allGroupedItems = new List<PonudaItem>();

        private bool itemsGrouped = false;

        public FormiranjePonudeView()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            InitializeComboBoxes();
            PonudaItems = new ObservableCollection<PonudaItem>();
            GroupedItems = new ObservableCollection<GroupedItem>();
            DataContext = this; // Set DataContext for data binding
            GroupedDataGrid.ItemsSource = GroupedItems;
        }

        public void RefreshData()
        {
            // Ponovo učitaj podatke iz baze, slično kao u LoadDataFromProcedure
            string procedureName = GetProcedureNameForSelectedOptions();
            if (procedureName != null)
            {
                LoadDataFromProcedure(procedureName);
            }
        }


        private void InitializeDatabaseConnection()
        {
            string connectionString = "server=localhost;database=myappdb;user=root;password=;";
            connection = new MySqlConnection(connectionString);
            connection.Open();
        }

        private void InitializeComboBoxes()
        {
            comboBoxSetups = new Dictionary<string, Action>
        {
            {"Direktno", SetupForDirektno},
            {"Zvezda-Trougao", SetupForZvezdaTrougao},
            {"Soft", SetupForSoft},
            {"Frekventno", SetupForFrekventno}
        };

            subComboBoxSetups = new Dictionary<string, Dictionary<string, Action>>
            {
                {"Direktno", new Dictionary<string, Action>
                    {
                        {"comboBox3", () => SetupComboBox(comboBox3, new[] {"Direktno", "Reverzibilni"})},
                        {"comboBox4", () => SetupComboBox(comboBox4, new[] {"0,09kW", "0,12kW", "0,37kW", "110kW"})}
                    }
                },
                {"Zvezda-Trougao", new Dictionary<string, Action>
                    {
                        {"comboBox3", () => SetupComboBox(comboBox3, new[] {"Option1", "Option2"})},
                        {"comboBox4", () => SetupComboBox(comboBox4, new[] {"Option3", "Option4"})}
                    }
                },
                {"Soft", new Dictionary<string, Action>
                    {
                        {"comboBox3", () => SetupComboBox(comboBox3, new[] {"Soft Option 1", "Soft Option 2"})},
                        {"comboBox4", () => SetupComboBox(comboBox4, new[] {"Soft Power 1", "Soft Power 2"})}
                    }
                },
                {"Frekventno", new Dictionary<string, Action>
                    {
                        {"comboBox3", () => SetupComboBox(comboBox3, new[] {"Frekventno Option 1", "Frekventno Option 2"})},
                        {"comboBox4", () => SetupComboBox(comboBox4, new[] {"Frekventno Power 1", "Frekventno Power 2"})}
                    }
                }
            };

            comboBox1.SelectionChanged += ComboBox_SelectionChanged;
            comboBox2.SelectionChanged += ComboBox_SelectionChanged;
            comboBox3.SelectionChanged += ComboBox3_SelectionChanged;
            comboBox4.SelectionChanged += ComboBox_SelectionChanged;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Logika za comboBox1 (nacin pokretanja)
            if (sender == comboBox1)
            {
                string selectedNacinPokretanja = (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(selectedNacinPokretanja) && comboBoxSetups.ContainsKey(selectedNacinPokretanja))
                {
                    ResetComboBoxes(comboBox2, comboBox3, comboBox4);  // Resetovanje drugih ComboBox-ova
                    comboBoxSetups[selectedNacinPokretanja].Invoke();  // Postavljanje vrednosti u zavisnosti od izbora
                    UpdateComboBoxVisibility(selectedNacinPokretanja); // Ažuriranje vidljivosti ComboBox-ova
                }
            }

            // Logika za comboBox4 (poslednji ComboBox sa automatskom selekcijom redova)
            if (sender == comboBox4)
            {
                UpdateProcedure();      // Ažuriraj podatke iz baze na osnovu izbora
                AutoSelectAllRows();    // Automatski selektuj sve redove u DataGrid-u
            }
            else
            {
                UpdateProcedure();      // Ako je bilo koji drugi ComboBox aktiviran, samo ažuriraj podatke
            }
        }

        private void ComboBox3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedOption = (comboBox3.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (selectedProizvodac == "Siemens")
            {
                if (selectedOption == "Direktno")
                {
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,37kW", "0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW", "5,5kW", "5,5kW_class20", "7,5kW", "7,5kW_class20",
                "9,2kW", "9,2kW_class20", "11kW", "11kW_class20", "15kW", "15kW_class20", "18,5kW","18,5kW_class20", "22kW","22kW_class20",
                "30kW","30kW_class20s", "37kW", "45kW", "55kW_(S3)", "55kW_(P)", "75kW", "90kW", "110kW" });
                }
                else if (selectedOption == "Reverzibilni")
                {
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,18kW", "0,25kW", "0,37kW", "0,55kW", "0,75kW" });
                }
            }
            else if (selectedProizvodac == "Schneider")
            {
                if (selectedOption == "Direktno")
                {
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,18kW", "0,25kW", "0,37kW", "0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW",
                        "5,5kW", "5,5kW_2", "7,5kW", "7,5kW_2", "9,2kW", "9,2kW_2", "11kW","11kW_2", "15kW", "15kW_2","15kW_3", "18,5kW", "18,5kW_2", "22kW", "22kW_2",
                        "30kW","30kW_2", "37kW","37kW_2", "45kW","45kW_2", "55kW", "55kW_2", "75kW","75kW_2", "90kW","90kW_2", "110kW", "110kW_2"});
                }
                else if (selectedOption == "Reverzibilni")
                {
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,18kW", "0,25kW", "0,37kW", "0,55kW", "0,75kW" });
                }
            }
        }

        private void UpdateComboBoxVisibility(string selectedNacinPokretanja)
        {
            if (selectedNacinPokretanja == "Soft")
            {
                comboBox2.Visibility = ProizvodacTextBlock.Visibility = Visibility.Collapsed;
                comboBox3.Visibility = comboBox4.Visibility = Visibility.Visible;
            }
            else
            {
                comboBox2.Visibility = ProizvodacTextBlock.Visibility =
                comboBox3.Visibility = comboBox4.Visibility = Visibility.Visible;
            }
        }

        private void ShowAllComboBoxes()
        {
            comboBox2.Visibility = ProizvodacTextBlock.Visibility =
            comboBox3.Visibility = comboBox4.Visibility = Visibility.Visible;
        }

        private void SetupForDirektno()
        {
            SetupComboBox(comboBox2, new[] { "Siemens", "Schneider" });
            SetupSubComboBoxes("Direktno");
        }

        private void SetupForZvezdaTrougao()
        {
            SetupComboBox(comboBox2, new[] { "Siemens", "Schneider" });
            SetupSubComboBoxes("Zvezda-Trougao");
        }

        private void SetupForSoft()
        {
            // Implementirajte specifičnu logiku za Soft
            SetupSubComboBoxes("Soft");
        }

        private void SetupForFrekventno()
        {
            SetupComboBox(comboBox2, new[] { "Siemens", "Schneider", "Danfoss" });
            SetupSubComboBoxes("Frekventno");
        }

        private void SetupSubComboBoxes(string key)
        {
            if (subComboBoxSetups.ContainsKey(key))
            {
                foreach (var setup in subComboBoxSetups[key])
                {
                    setup.Value.Invoke();
                }
            }
        }

        private void SetupComboBox(ComboBox comboBox, string[] items)
        {
            comboBox.Items.Clear();
            foreach (var item in items)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = item });
            }
        }

        private void ResetComboBoxes(params ComboBox[] comboBoxes)
        {
            foreach (var cb in comboBoxes)
            {
                cb.SelectedIndex = -1;
                cb.Items.Clear();
            }
        }

        public void UpdateProcedure()
        {
            string procedureName = GetProcedureNameForSelectedOptions();
            if (procedureName != null)
            {
                LoadDataFromProcedure(procedureName);
            }
        }

        private string GetProcedureNameForSelectedOptions()
        {
            string selectedNacinPokretanja = (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string selectedBrojSmerova = (comboBox3.SelectedItem as ComboBoxItem)?.Content?.ToString();
            string selectedSnaga = (comboBox4.SelectedItem as ComboBoxItem)?.Content?.ToString();

            return (selectedNacinPokretanja, selectedProizvodac) switch
            {
                ("Direktno", "Siemens") => selectedBrojSmerova == "Reverzibilni"
                    ? selectedSnaga switch
                    {
                        "0,09kW" => "Reverzibilni_D_SI_0_09kW",
                        "0,12kW" => "Reverzibilni_D_SI_0_12kW",
                        "0,18kW" => "Reverzibilni_D_SI_0_18kW",
                        "0,25kW" => "Reverzibilni_D_SI_0_25kW",
                        "0,37kW" => "Reverzibilni_D_SI_0_37kW",
                        "0,55kW" => "Reverzibilni_D_SI_0_55kW",
                        "0,75kW" => "Reverzibilni_D_SI_0_75kW",
                        _ => "Reverzibilni_D_SI"
                    }
                    : selectedSnaga switch
                    {
                        "0,09kW" => "Direktini_d_si_0_09kW",
                        "0,12kW" => "Direktini_d_si_0_12kW",
                        "0,18kW" => "Direktini_d_si_0_18kW",
                        "0,25kW" => "Direktini_d_si_0_25kW",
                        "0,37kW" => "Direktini_d_si_0_37kW",
                        "0,55kW" => "Direktini_d_si_0_55kW",
                        "0,75kW" => "Direktini_d_si_0_75kW",
                        "1,1kW" => "Direktini_d_si_1_1kW",
                        "1,5kW" => "Direktini_d_si_1_5kW",
                        "2,2kW" => "Direktini_d_si_2_2kW",
                        "3kW" => "Direktini_d_si_3kW",
                        "4kW" => "Direktini_d_si_4kW",
                        "5,5kW" => "Direktini_d_si_5_5kW",
                        "5,5kW_class20" => "Direktini_d_si_5_5kW_class",
                        "7,5kW" => "Direktini_d_si_7_5kW",
                        "7,5kW_class20" => "Direktini_d_si_7_5kW_class",
                        "9,2kW" => "Direktini_d_si_9_2kW",
                        "9,2kW_class20" => "Direktini_d_si_9_2kW_class",
                        "11kW" => "Direktini_d_si_11kW",
                        "11kW_class20" => "Direktini_d_si_11kW_class",
                        "15kW" => "Direktini_d_si_15kW",
                        "15kW_class20" => "Direktini_d_si_15kW_class",
                        "18,5kW" => "Direktini_d_si_18_5kW",
                        "18,5kW_class20" => "Direktini_d_si_18_5kW_class",
                        "22kW" => "Direktini_d_si_22kW",
                        "22kW_class20" => "Direktini_d_si_22kW_class",
                        "30kW" => "Direktini_d_si_30kW",
                        "30kW_class20" => "Direktini_d_si_30kW_class",
                        "37kW" => "Direktini_d_si_37kW",
                        "45kW" => "Direktini_d_si_45kW",
                        "55kW_(S3)" => "Direktini_d_si_55kW_S3",
                        "55kW_(P)" => "Direktini_d_si_55kW_P",
                        "75kW" => "Direktini_d_si_75kW",
                        "90kW" => "Direktini_d_si_90kW",
                        "110kW" => "Direktini_d_si_110kW",
                        _ => "Direktini_d_si"
                    },
                ("Direktno", "Schneider") => selectedBrojSmerova switch
                {
                    "Reverzibilni" => selectedSnaga switch
                    {
                        "0,09kW" => "Reverzibilni_D_SE_0_09kW",
                        "0,12kW" => "Reverzibilni_D_SE_0_12kW",
                        "0,18kW" => "Reverzibilni_D_SE_0_18kW",
                        "0,25kW" => "Reverzibilni_D_SE_0_25kW",
                        "0,37kW" => "Reverzibilni_D_SE_0_37kW",
                        "0,55kW" => "Reverzibilni_D_SE_0_55kW",
                        "0,75kW" => "Reverzibilni_D_SE_0_75kW",
                        _ => "Reverzibilni_D_SE"
                    },
                    _ => selectedSnaga switch
                    {
                        "0,09kW" => "Direktini_d_se_0_09kW",
                        "0,12kW" => "Direktini_d_se_0_12kW",
                        "0,18kW" => "Direktini_d_se_0_18kW",
                        "0,25kW" => "Direktini_d_se_0_25kW",
                        "0,37kW" => "Direktini_d_se_0_37kW",
                        "0,55kW" => "Direktini_d_se_0_55kW",
                        "0,75kW" => "Direktini_d_se_0_75kW",
                        "1,1kW" => "Direktini_d_se_1_1kW",
                        "1,5kW" => "Direktini_d_se_1_5kW",
                        "2,2kW" => "Direktini_d_se_2_2kW",
                        "3kW" => "Direktini_d_se_3kW",
                        "4kW" => "Direktini_d_se_4kW",
                        "5,5kW" => "Direktini_d_se_5_5kW",
                        "5,5kW_2" => "Direktini_d_se_5_5kW2",
                        "7,5kW" => "Direktini_d_se_7_5kW",
                        "7,5kW_2" => "Direktini_d_se_7_5kW2",
                        "9,2kW" => "Direktini_d_se_9_2kW",
                        "9,2kW_2" => "Direktini_d_se_9_2kW2",
                        "11kW" => "Direktini_d_se_11kW",
                        "11kW_2" => "Direktini_d_se_11kW2",
                        "15kW" => "Direktini_d_se_15kW",
                        "15kW_2" => "Direktini_d_se_15kW2",
                        "15kW_3" => "Direktini_d_se_15kW3",
                        "18,5kW" => "Direktini_d_se_18_5kW",
                        "18,5kW_2" => "Direktini_d_se_18_5kW2",
                        "22kW" => "Direktini_d_se_22kW",
                        "22kW_2" => "Direktini_d_se_22kW2",
                        "30kW" => "Direktini_d_se_30kW",
                        "30kW_2" => "Direktini_d_se_30kW2",
                        "37kW" => "Direktini_d_se_37kW",
                        "37kW_2" => "Direktini_d_se_37kW2",
                        "45kW" => "Direktini_d_se_45kW",
                        "45kW_2" => "Direktini_d_se_45kW2",
                        "55kW" => "Direktini_d_se_55kW",
                        "55kW_2" => "Direktini_d_se_55kW2",
                        "75kW" => "Direktini_d_se_75kW",
                        "75kW_2" => "Direktini_d_se_75kW2",
                        "90kW" => "Direktini_d_se_90kW",
                        "90kW_2" => "Direktini_d_se_90kW2",
                        "110kW" => "Direktini_d_se_110kW",
                        "110kW_2" => "Direktini_d_se_110kW2",
                        _ => "Direktini_d_se"
                    }
                },
                ("Zvezda-Trougao", "Siemens") => "YD_si_start",
                ("Zvezda-Trougao", "Schneider") => "YD_se_start",
                ("Soft", _) => "sp_get_soft",
                ("Frekventno", "Siemens") => "FC_si",
                ("Frekventno", "Schneider") => "FC_se",
                ("Frekventno", "Danfoss") => "FC_d",
                _ => null
            };
        }

        private void LoadDataFromProcedure(string procedureName)
        {
            if (procedureName == null) return;

            DataTable resultTable = new DataTable();
            using (MySqlCommand cmd = new MySqlCommand(procedureName, connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                adapter.Fill(resultTable);
            }

            PonudaItems.Clear(); // Clear existing items before adding new ones

            foreach (DataRow row in resultTable.Rows)
            {
                var item = new PonudaItem
                {
                    Fabricki_kod = row["Fabricki_kod"].ToString(),
                    Opis = row["Opis"].ToString(),
                    Puna_cena = row["Puna_cena"] != DBNull.Value ? Convert.ToDecimal(row["Puna_cena"]) : 0m,
                    Dimenzije = row["Dimenzije"].ToString(),
                    Disipacija = row["Disipacija"] != DBNull.Value ? Convert.ToDecimal(row["Disipacija"]) : 0m,
                    Tezina = row["Tezina"] != DBNull.Value ? Convert.ToDecimal(row["Tezina"]) : 0m,
                    IsSelected = false,
                    Kolicina = row["Kolicina"] != DBNull.Value ? Convert.ToInt32(row["Kolicina"]) : 0,
                    Vrednost_rabata = row["Vrednost_rabata"] != DBNull.Value ? Convert.ToDecimal(row["Vrednost_rabata"]) : 0m,
                };

                PonudaItems.Add(item);
            }

            ResultsDataGrid.ItemsSource = PonudaItems; // Bind ObservableCollection to DataGrid
        }

        // Automatically selects all rows in the DataGrid
        private void AutoSelectAllRows()
        {
            ResultsDataGrid.SelectAll();
        }


        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();

            if (selectedItems.Count > 0)
            {
                var selectedItem = selectedItems.First();
            }
        }

        private void HandleGrouping(PonudaItem selectedItem, List<PonudaItem> selectedItems)
        {
            if (selectedItems.Count > 0)
            {

                selectedItem.IsSelected = true;
            }
        }

        private void CreateOrUpdateGroup(List<PonudaItem> selectedItems)
        {
            bool groupFound = false;

            foreach (var group in groupedPonudaItems.Values)
            {
                // Proverava da li neka od stavki u selectedItems već postoji u grupi
                if (selectedItems.Any(item => group.Any(g => g.Fabricki_kod == item.Fabricki_kod)))
                {
                    // Ako postoji, dodajte sve stavke
                    foreach (var item in selectedItems)
                    {
                        if (!group.Contains(item))
                        {
                            group.Add(item);
                        }
                    }
                    groupFound = true;
                    break;
                }
            }

            if (!groupFound)
            {
                // Kreirajte novu grupu ako nije pronađena postojeća
                currentGroupId++;
                var newGroup = new ObservableCollection<PonudaItem>(selectedItems);
                groupedPonudaItems.Add(currentGroupId, newGroup);
            }

            UpdateGroupedDataGrid();
        }

        private void CreateNewGroup(List<PonudaItem> selectedItems)
        {
            currentGroupId++;
            var newGroup = new ObservableCollection<PonudaItem>(selectedItems);

            foreach (var selectedItem in selectedItems)
            {
               
            }

            groupedPonudaItems.Add(currentGroupId, newGroup);
            UpdateGroupedDataGrid();
        }

        private void UpdateGroupedDataGrid()
        {
            foreach (var group in groupedPonudaItems.Values)
            {
                foreach (var item in group)
                {
                    var existingItem = GroupedItems.FirstOrDefault(i => i.GroupName == item.Fabricki_kod);
                    if (existingItem == null)
                    {
                        GroupedItems.Add(new GroupedItem
                        {
                            Opis = item.Opis,
                            GroupName = item.Fabricki_kod,
                            Quantity = 1 // Postavite početnu količinu na 1
                        });
                    }
                    else
                    {
                        // Ako stavka već postoji, povećajte količinu
                        existingItem.Quantity++;
                    }
                }
            }
        }

        private DataGridRow GetRowFromFabrickiKod(string fabricki_kod)
        {
            foreach (var item in ResultsDataGrid.Items)
            {
                if (item is PonudaItem ponudaItem && ponudaItem.Fabricki_kod == fabricki_kod)
                {
                    return ResultsDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                }
            }
            return null;
        }

        private void ConfirmSelection_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            // Transfer selected items to GroupedDataGrid and accumulate in allGroupedItems
            foreach (var item in selectedItems)
            {
                if (!allGroupedItems.Any(g => g.Fabricki_kod == item.Fabricki_kod))
                {
                    allGroupedItems.Add(item);
                    GroupedItems.Add(new GroupedItem
                    {
                        Opis = item.Opis,
                        GroupName = item.Fabricki_kod,
                        Quantity = 0 // Adjust logic if needed
                    });
                }
            }

            // Optionally clear the selection from ResultsDataGrid after moving
            ResultsDataGrid.SelectedItems.Clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // Proveri da li postoji količina 0
            bool hasZeroQuantity = GroupedItems.Any(item => item.Quantity <= 0);

            if (hasZeroQuantity)
            {
                MessageBox.Show("Nije moguće nastaviti jer je broj komada 0.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Prekini metodu ako je količina 0
            }

            List<PonudaItem> selectedItems = new List<PonudaItem>();

            foreach (var groupedItem in GroupedItems)
            {
                var matchingPonudaItem = allGroupedItems.FirstOrDefault(p => p.Fabricki_kod == groupedItem.GroupName);
                if (matchingPonudaItem != null)
                {
                    // Kopiraj originalni PonudaItem
                    PonudaItem itemForOrder = new PonudaItem
                    {
                        Fabricki_kod = matchingPonudaItem.Fabricki_kod,
                        Opis = matchingPonudaItem.Opis,
                        Puna_cena = matchingPonudaItem.Puna_cena,
                        Dimenzije = matchingPonudaItem.Dimenzije,
                        Disipacija = matchingPonudaItem.Disipacija,
                        Tezina = matchingPonudaItem.Tezina,
                        Kolicina = matchingPonudaItem.Kolicina,  // Originalna količina
                        Vrednost_rabata = matchingPonudaItem.Vrednost_rabata,
                        KolicinaZaNarucivanje = groupedItem.Quantity  // Nova količina za naručivanje
                    };

                    // Izračunaj ukupne vrijednosti koristeći KolicinaZaNarucivanje
                    itemForOrder.Ukupna_puna = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Puna_cena;
                    itemForOrder.Ukupna_rabat = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Puna_cena * (1 - itemForOrder.Vrednost_rabata);
                    itemForOrder.Ukupna_Disipacija = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Disipacija;
                    itemForOrder.Ukupna_Tezina = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Tezina;

                    selectedItems.Add(itemForOrder);
                }
            }

            // Otvorite Racunanjeponude i prosledite selectedItems kao parametar
            Racunanjeponude racunanjePonude = new Racunanjeponude(this, selectedItems);
            racunanjePonude.Show();
        }


        private void GroupedDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }


}
