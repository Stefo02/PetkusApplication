using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using MySql.Data.MySqlClient;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class FormiranjePonudeView : UserControl
    {
        private MySqlConnection connection;
        private Dictionary<string, (List<string> RelatedCodes, bool OfferChoice)> relatedItemsMapping = new Dictionary<string, (List<string> RelatedCodes, bool OfferChoice)>()
        {
            { "3RV2011-0EA10", (new List<string> { "3RT2015-1BB42" }, false) },
            { "3RT2015-1BB42", (new List<string> { "3RV2011-0EA10", "3RV2011-0GA10" }, true) },
            { "3RV2011-0GA10", (new List<string> { "3RT2015-1BB42" }, false) }
        };

        private List<DataGridRow> blinkingRows = new List<DataGridRow>();

        public ObservableCollection<PonudaItem> PonudaItems { get; set; }

        public FormiranjePonudeView()
        {
            InitializeComponent();
            InitializeDatabaseConnection();
            PonudaItems = new ObservableCollection<PonudaItem>();
            DataContext = this; // Set DataContext for data binding
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
                                blinkingRows.Add(row);
                                var animation = (Storyboard)FindResource("BlinkingAnimation");
                                animation.Begin(row, true);
                            }
                        }
                    }
                    else if (relatedCodes.Count > 0)
                    {
                        // Ako treba automatski izabrati prvi kod
                        var firstCode = relatedCodes.First();
                        var row = GetRowFromFabrickiKod(firstCode);
                        if (row != null)
                        {
                            ResultsDataGrid.SelectedItems.Add(row.Item);
                        }
                    }
                }
            }
        }

        private DataGridRow GetRowFromFabrickiKod(string fabrickiKod)
        {
            foreach (var item in ResultsDataGrid.Items)
            {
                if (((PonudaItem)item).Fabricki_kod == fabrickiKod)
                {
                    return (DataGridRow)ResultsDataGrid.ItemContainerGenerator.ContainerFromItem(item);
                }
            }
            return null;
        }

        private void DataGridRow_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row != null && blinkingRows.Contains(row))
            {
                var animation = (Storyboard)FindResource("BlinkingAnimation");
                animation.Stop(row);
                blinkingRows.Remove(row);
                ResultsDataGrid.SelectedItems.Add(row.Item);
            }
        }

        private void TransferSelectedRows_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();
            if (selectedItems.Any())
            {
                Racunanjeponude secondWindow = new Racunanjeponude(selectedItems);
                secondWindow.Show();
            }
            else
            {
                MessageBox.Show("No rows selected.");
            }
        }
    }
}
