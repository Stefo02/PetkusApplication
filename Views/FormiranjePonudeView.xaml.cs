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
        private Dictionary<string, (List<string> RelatedCodes, bool OfferChoice)> relatedItemsMapping = new Dictionary<string, (List<string> RelatedCodes, bool OfferChoice)>
        {
            { "3RV2011-0EA10", (new List<string> { "3RT2015-1BB42" }, false) },
            { "3RT2015-1BB42", (new List<string> { "3RV2011-0EA10", "3RV2011-0GA10" }, true) },
            { "3RV2011-0GA10", (new List<string> { "3RT2015-1BB42" }, false) },
            { "3RM1201-1AA04", (new List<string> { "3RV2011-1FA10" }, false) },
            { "3RV2011-1FA10", (new List<string> { "3RM1201-1AA04" }, false) },
            { "3RV2321-1CC10", (new List<string> { "3RV2011-1CA10" }, false) },
            { "3RV2011-1CA10", (new List<string> { "3RV2321-1CC10" }, false) }

        };

        private List<DataGridRow> blinkingRows = new List<DataGridRow>();
        private Dictionary<int, ObservableCollection<PonudaItem>> groupedPonudaItems = new Dictionary<int, ObservableCollection<PonudaItem>>();
        private int currentGroupId = 0;

        public ObservableCollection<PonudaItem> PonudaItems { get; set; }
        public ObservableCollection<GroupedItem> GroupedItems { get; set; }

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
            comboBox3.SelectionChanged += ComboBox_SelectionChanged;
            comboBox4.SelectionChanged += ComboBox_SelectionChanged;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == comboBox1)
            {
                string selectedNacinPokretanja = (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (!string.IsNullOrEmpty(selectedNacinPokretanja) && comboBoxSetups.ContainsKey(selectedNacinPokretanja))
                {
                    ResetComboBoxes(comboBox2, comboBox3, comboBox4);
                    comboBoxSetups[selectedNacinPokretanja].Invoke();
                    UpdateComboBoxVisibility(selectedNacinPokretanja);
                }
            }
            UpdateProcedure();
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

        private void UpdateProcedure()
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
                    ? (selectedSnaga == "0,09kW" ? "Reverzibilni_D_SI_0_09kW" : "Reverzibilni_D_SI")
                    : "Direktini_d_si",
                ("Direktno", "Schneider") => "Direktini_d_se",
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

                    RelatedFabricki_kod = relatedItemsMapping.ContainsKey(row["Fabricki_kod"].ToString())
                        ? relatedItemsMapping[row["Fabricki_kod"].ToString()].RelatedCodes
                        : new List<string>()
                };

                PonudaItems.Add(item);
            }

            ResultsDataGrid.ItemsSource = PonudaItems; // Bind ObservableCollection
        }


        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();

            // Stop animation for previous blinking rows
            foreach (var row in blinkingRows)
            {
                var animation = (Storyboard)FindResource("BlinkingAnimation");
                animation.Stop(row);
            }
            blinkingRows.Clear();

            if (selectedItems.Count > 0)
            {
                var selectedItem = selectedItems.First();
                MessageBox.Show($"Selected item: {selectedItem.Fabricki_kod}");

                if (relatedItemsMapping.ContainsKey(selectedItem.Fabricki_kod))
                {
                    var (relatedCodes, offerChoice) = relatedItemsMapping[selectedItem.Fabricki_kod];

                    MessageBox.Show($"Related codes: {string.Join(", ", relatedCodes)}, Offer choice: {offerChoice}");

                    if (offerChoice)
                    {
                        // Animate related rows
                        foreach (var code in relatedCodes)
                        {
                            var row = GetRowFromFabrickiKod(code);
                            if (row != null)
                            {
                                StartBlinkingAnimation(row);
                                MessageBox.Show($"Started blinking animation for row with code: {code}");
                            }
                        }
                    }
                    else
                    {
                        // Automatically select related rows without animation
                        foreach (var code in relatedCodes)
                        {
                            var row = GetRowFromFabrickiKod(code);
                            if (row != null)
                            {
                                row.IsSelected = true;
                                MessageBox.Show($"Automatically selected row with code: {code}");
                            }
                        }
                    }
                }
            }
        }


        private void HandleGrouping(PonudaItem selectedItem, List<PonudaItem> selectedItems)
        {
            if (selectedItems.Count > 0)
            {
                MessageBox.Show("Handling grouping for selected items.");

                // Clear previous selections
                foreach (var item in PonudaItems)
                {
                    item.IsSelected = false;
                }

                // Mark the selected item as selected
                selectedItem.IsSelected = true;
                MessageBox.Show($"Marked item as selected: {selectedItem.Fabricki_kod}");

                // Check if we need to create a new group
                if (relatedItemsMapping.ContainsKey(selectedItem.Fabricki_kod))
                {
                    var (relatedCodes, offerChoice) = relatedItemsMapping[selectedItem.Fabricki_kod];
                    var relatedItems = PonudaItems.Where(p => relatedCodes.Contains(p.Fabricki_kod)).ToList();

                    if (offerChoice)
                    {
                        // Add the first related item to the group
                        if (selectedItems.Count > 1)
                        {
                            var firstRelatedItem = relatedItems.FirstOrDefault(p => !selectedItems.Contains(p));
                            if (firstRelatedItem != null)
                            {
                                selectedItems.Add(firstRelatedItem);
                                MessageBox.Show($"Added related item to group: {firstRelatedItem.Fabricki_kod}");
                            }
                        }
                    }

                    // Create a new group with the selected items
                    CreateNewGroup(selectedItems);
                }
            }
        }

        private void CreateNewGroup(List<PonudaItem> selectedItems)
        {
            currentGroupId++;
            var newGroup = new ObservableCollection<PonudaItem>(selectedItems);

            foreach (var selectedItem in selectedItems)
            {
                var relatedCodes = relatedItemsMapping.ContainsKey(selectedItem.Fabricki_kod)
                    ? relatedItemsMapping[selectedItem.Fabricki_kod].RelatedCodes
                    : new List<string>();

                foreach (var code in relatedCodes)
                {
                    var relatedItem = PonudaItems.FirstOrDefault(p => p.Fabricki_kod == code);
                    if (relatedItem != null && !newGroup.Contains(relatedItem))
                    {
                        newGroup.Add(relatedItem);
                    }
                }
            }

            groupedPonudaItems.Add(currentGroupId, newGroup);
            UpdateGroupedDataGrid();
        }

        private void UpdateGroupedDataGrid()
        {
            GroupedItems.Clear();
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
                            Quantity = 0 // Initial quantity
                        });
                    }
                }
            }
        }

        private void StartBlinkingAnimation(DataGridRow row)
        {
            var animation = (Storyboard)FindResource("BlinkingAnimation");
            animation.Begin(row, true);
            blinkingRows.Add(row);
            MessageBox.Show($"Blinking animation started for row: {row}");
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

        private void DataGridRow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var clickedRow = sender as DataGridRow;
            var clickedItem = clickedRow?.Item as PonudaItem;

            if (clickedItem != null && blinkingRows.Contains(clickedRow))
            {
                // Stop blinking for all rows
                foreach (var row in blinkingRows)
                {
                    var animation = (Storyboard)FindResource("BlinkingAnimation");
                    animation.Stop(row);
                }
                blinkingRows.Clear();

                // Set the selected item and handle grouping
                var selectedItem = ResultsDataGrid.SelectedItem as PonudaItem;

                if (selectedItem != null && relatedItemsMapping.ContainsKey(selectedItem.Fabricki_kod))
                {
                    var (relatedCodes, offerChoice) = relatedItemsMapping[selectedItem.Fabricki_kod];

                    if (offerChoice && relatedCodes.Contains(clickedItem.Fabricki_kod))
                    {
                        selectedItem.IsSelected = true;
                        clickedItem.IsSelected = true;

                        // Create a new group with the selected items
                        CreateNewGroup(new List<PonudaItem> { selectedItem, clickedItem });
                    }
                }
            }
        }

        private void ConfirmSelection_Click(object sender, RoutedEventArgs e)
        {
            if (!itemsGrouped)
            {
                var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();
                MessageBox.Show($"Number of items selected: {selectedItems.Count}");

                if (selectedItems.Count == 0)
                {
                    MessageBox.Show("Molimo izaberite bar jedan red.");
                    return;
                }

                var selectedItem = selectedItems.First();
                MessageBox.Show($"Selected item for grouping: {selectedItem.Fabricki_kod}");

                // Stop blinking animations
                foreach (var row in blinkingRows)
                {
                    var animation = (Storyboard)FindResource("BlinkingAnimation");
                    animation.Stop(row);
                }
                blinkingRows.Clear();

                if (relatedItemsMapping.ContainsKey(selectedItem.Fabricki_kod))
                {
                    var (relatedCodes, offerChoice) = relatedItemsMapping[selectedItem.Fabricki_kod];
                    MessageBox.Show($"Related codes: {string.Join(", ", relatedCodes)}, Offer choice: {offerChoice}");

                    // Collect all related items including newly selected items
                    var relatedItems = new HashSet<PonudaItem>(selectedItems);
                    foreach (var code in relatedCodes)
                    {
                        var relatedItem = PonudaItems.FirstOrDefault(p => p.Fabricki_kod == code);
                        if (relatedItem != null && !relatedItems.Contains(relatedItem))
                        {
                            relatedItems.Add(relatedItem);
                        }
                    }

                    // Add to GroupedItems if not already present
                    foreach (var item in relatedItems)
                    {
                        var existingItem = GroupedItems.FirstOrDefault(g => g.GroupName == item.Fabricki_kod);
                        if (existingItem == null)
                        {
                            GroupedItems.Add(new GroupedItem
                            {
                                Opis = item.Opis,
                                GroupName = item.Fabricki_kod,
                                Quantity = 1 // Set initial quantity
                            });
                        }
                        else
                        {
                            existingItem.Quantity++;
                        }
                    }

                    // Refresh ResultsDataGrid to show current state
                    ResultsDataGrid.ItemsSource = null; // Reset ItemsSource to force refresh
                    ResultsDataGrid.ItemsSource = PonudaItems;
                    ResultsDataGrid.Items.Refresh();

                    MessageBox.Show($"Total items in ResultsDataGrid: {PonudaItems.Count}");
                    MessageBox.Show($"Total items in GroupedDataGrid: {GroupedItems.Count}");

                    itemsGrouped = true;
                }
                else
                {
                    selectedItem.IsSelected = true;
                }
            }
            else
            {
                // Process for ungrouping items
                var itemsToRemove = GroupedItems.ToList();

                foreach (var groupedItem in itemsToRemove)
                {
                    var matchingPonudaItem = PonudaItems.FirstOrDefault(p => p.Fabricki_kod == groupedItem.GroupName);
                    if (matchingPonudaItem != null)
                    {
                        GroupedItems.Remove(groupedItem);
                        MessageBox.Show($"Moved item back to ResultsDataGrid: {matchingPonudaItem.Fabricki_kod}");
                    }
                }

                ResultsDataGrid.ItemsSource = null; // Reset ItemsSource to force refresh
                ResultsDataGrid.ItemsSource = PonudaItems;
                ResultsDataGrid.Items.Refresh();

                MessageBox.Show($"Total items in ResultsDataGrid after moving: {PonudaItems.Count}");
                MessageBox.Show($"Total items in GroupedDataGrid after ungrouping: {GroupedItems.Count}");
                itemsGrouped = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<PonudaItem> selectedItems = new List<PonudaItem>();

            foreach (var groupedItem in GroupedItems)
            {
                var matchingPonudaItem = PonudaItems.FirstOrDefault(p => p.Fabricki_kod == groupedItem.GroupName);
                if (matchingPonudaItem != null)
                {
                    selectedItems.Add(matchingPonudaItem);
                }
            }

            // Now you have the selectedItems list to be used as needed
        }
    }


}

