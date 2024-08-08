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
            { "3RV2011-0GA10", (new List<string> { "3RT2015-1BB42" }, false) }
        };

        private List<DataGridRow> blinkingRows = new List<DataGridRow>();

        public ObservableCollection<PonudaItem> PonudaItems { get; set; }
        public ObservableCollection<GroupedItem> GroupedItems { get; set; }

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
                    return "sp_get_d_si";
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

        private void DataGridRow_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Logic for handling mouse left button up event on DataGridRow
        }

        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();

            // Zaustavi animaciju za prethodne trepćuće redove
            foreach (var row in blinkingRows)
            {
                var animation = (Storyboard)FindResource("BlinkingAnimation");
                animation.Stop(row);
            }
            blinkingRows.Clear();

            if (selectedItems.Count > 0)
            {
                var selectedItem = selectedItems.First();

                // Pronađi povezane kodove
                if (relatedItemsMapping.ContainsKey(selectedItem.Fabricki_kod))
                {
                    var (relatedCodes, offerChoice) = relatedItemsMapping[selectedItem.Fabricki_kod];

                    if (offerChoice && relatedCodes.Count > 1)
                    {
                        // Ako treba ponuditi izbor između dva koda
                        foreach (var code in relatedCodes)
                        {
                            var row = GetRowFromFabrickiKod(code);
                            if (row != null)
                            {
                                var result = MessageBox.Show($"Do you want to select {code} instead?", "Related Item", MessageBoxButton.YesNo);
                                if (result == MessageBoxResult.Yes)
                                {
                                    row.IsSelected = true;
                                }

                                StartBlinkingAnimation(row);
                            }
                        }
                    }
                    else
                    {
                        foreach (var code in relatedCodes)
                        {
                            var row = GetRowFromFabrickiKod(code);
                            if (row != null)
                            {
                                row.IsSelected = true;
                                StartBlinkingAnimation(row);
                            }
                        }
                    }
                }
            }

            // Transfer selected rows to GroupedDataGrid
            TransferSelectedRowsToGroupedDataGrid();
        }

        private void TransferSelectedRowsToGroupedDataGrid()
        {
            var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();
            foreach (var selectedItem in selectedItems)
            {
                var existingItem = GroupedItems.FirstOrDefault(i => i.GroupName == selectedItem.Fabricki_kod);
                if (existingItem == null)
                {
                    GroupedItems.Add(new GroupedItem
                    {
                        Opis = selectedItem.Opis,
                        GroupName = selectedItem.Fabricki_kod,
                        Quantity = 0 // Initial quantity
                    });
                }
            }
        }

        private void StartBlinkingAnimation(DataGridRow row)
        {
            var animation = (Storyboard)FindResource("BlinkingAnimation");
            animation.Begin(row, true); // true za kontrolu kadra
            blinkingRows.Add(row); // Dodajemo u listu trepćućih redova
        }

        private DataGridRow GetRowFromFabrickiKod(string fabrickiKod)
        {
            foreach (var item in ResultsDataGrid.Items)
            {
                var row = ResultsDataGrid.ItemContainerGenerator.ContainerFromItem(item) as DataGridRow;
                if (row != null)
                {
                    var cellContent = ResultsDataGrid.Columns[0].GetCellContent(row);
                    if (cellContent is TextBlock textBlock && textBlock.Text == fabrickiKod)
                    {
                        return row;
                    }
                }
            }
            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            List<PonudaItem> selectedItems = new List<PonudaItem>();

            foreach (var groupedItem in GroupedItems)
            {
                var matchingPonudaItem = PonudaItems.FirstOrDefault(p => p.Fabricki_kod == groupedItem.GroupName);
                if (matchingPonudaItem != null)
                {
                    int brojKomada = groupedItem.Quantity;
                    matchingPonudaItem.Kolicina = brojKomada; // Set the quantity
                    matchingPonudaItem.Ukupna_puna = brojKomada * matchingPonudaItem.Puna_cena;
                    matchingPonudaItem.Ukupna_rabat = brojKomada * matchingPonudaItem.Puna_cena * (1 - matchingPonudaItem.Vrednost_rabata);
                    matchingPonudaItem.Ukupna_Disipacija = brojKomada * matchingPonudaItem.Disipacija;
                    matchingPonudaItem.Ukupna_Tezina = brojKomada * matchingPonudaItem.Tezina;

                    selectedItems.Add(matchingPonudaItem);
                }
            }

            // Otvorite Racunanjeponude i prosledite FormiranjePonudeView kao parametar
            Racunanjeponude racunanjePonude = new Racunanjeponude(this, selectedItems);
            racunanjePonude.Show();
        }
    }
}
