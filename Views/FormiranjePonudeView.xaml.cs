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
        private Dictionary<string, (List<string> RelatedCodes, bool OfferChoice)> relatedItemsMapping = new Dictionary<string, (List<string> RelatedCodes, bool OfferChoice)>
        {
            { "3RV2011-0EA10", (new List<string> { "3RT2015-1BB42" }, false) },
            { "3RT2015-1BB42", (new List<string> { "3RV2011-0EA10", "3RV2011-0GA10" }, true) },
            { "3RV2011-0GA10", (new List<string> { "3RT2015-1BB42" }, false) },
            { "3RM1201-1AA04", (new List<string> { "3RV2011-1FA10" }, false) },
            { "3RV2011-1FA10", (new List<string> { "3RM1201-1AA04" }, false) }

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

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string procedureName = GetProcedureNameForSelectedOptions();
            if (procedureName != null)
            {
                LoadDataFromProcedure(procedureName);
            }
        }

        private string GetProcedureNameForSelectedOptions()
        {
            string selectedNacinPokretanja = (comboBox1.SelectedItem as ComboBoxItem)?.Content.ToString();
            string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content.ToString();
            string selectedBrojSmerova = (comboBox3.SelectedItem as ComboBoxItem)?.Content.ToString();
            string selectedSnaga = (comboBox4.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedNacinPokretanja == "Direktno")
            {
                if (selectedProizvodac == "Siemens")
                {
                    if (selectedBrojSmerova == "Reverzibilni")
                    {
                        if (selectedSnaga == "0,09kW")
                        {
                            return "Reverzibilni_D_SI_0_09kW";
                        }
                        return "Reverzibilni_D_SI";
                    }
                    return "sp_get_direktno_si";
                }
                return "sp_get_direktno";
            }

            return null;
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
                    Puna_cena = Convert.ToDecimal(row["Puna_cena"]),
                    Dimenzije = row["Dimenzije"].ToString(),
                    Disipacija = Convert.ToDecimal(row["Disipacija"]),
                    Tezina = Convert.ToDecimal(row["Tezina"]),
                    IsSelected = false,
                    Kolicina = Convert.ToInt32(row["Kolicina"]),
                    Vrednost_rabata = Convert.ToDecimal(row["Vrednost_rabata"]),
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

                    if (offerChoice)
                    {
                        foreach (var item in PonudaItems)
                        {
                            item.IsSelected = false;
                        }

                        selectedItem.IsSelected = true;
                        MessageBox.Show($"Marked selected item as selected: {selectedItem.Fabricki_kod}");

                        var selectedRelatedItems = selectedItems.Where(i => relatedCodes.Contains(i.Fabricki_kod)).ToList();
                        selectedItems.AddRange(selectedRelatedItems);

                        MessageBox.Show($"Total items in the new group: {selectedItems.Count}");

                        CreateNewGroup(selectedItems); // Create a new group and update GroupedItems
                    }
                    else
                    {
                        selectedItem.IsSelected = true;
                    }

                    // Add selected items to GroupedItems without removing from PonudaItems
                    foreach (var item in selectedItems)
                    {
                        if (!GroupedItems.Any(g => g.GroupName == item.Fabricki_kod))
                        {
                            GroupedItems.Add(new GroupedItem
                            {
                                Opis = item.Opis,
                                GroupName = item.Fabricki_kod,
                                Quantity = 1 // Or calculate based on how many times it's in selectedItems
                            });
                        }
                        else
                        {
                            var existingItem = GroupedItems.First(g => g.GroupName == item.Fabricki_kod);
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
            }
            else
            {
                // Process for ungrouping items
                foreach (var groupedItem in GroupedItems.ToList())
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

