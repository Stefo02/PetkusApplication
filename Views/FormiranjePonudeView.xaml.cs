using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class FormiranjePonudeView : UserControl
    {
        private MySqlConnection connection;
        private Dictionary<string, List<string>> relatedItemsMapping = new Dictionary<string, List<string>>()
{
    { "3RV2011-0EA10", new List<string> { "3RT2015-1BB42" } },
    { "3RT2015-1BB42", new List<string> { "3RV2011-0EA10" } },
    { "3RM1201-1AA04", new List<string> { "3RV2011-1FA10" } },
    { "3RV2011-1FA10", new List<string> { "3RM1201-1AA04" } }
};



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
                        ? relatedItemsMapping[row["Fabricki_kod"].ToString()]
                        : new List<string>()
                };

                PonudaItems.Add(item);
            }

            ResultsDataGrid.ItemsSource = PonudaItems; // Bind ObservableCollection
        }

        private void ResultsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Privremeno onemogućite događaj da biste sprečili rekurziju
            ResultsDataGrid.SelectionChanged -= ResultsDataGrid_SelectionChanged;

            try
            {
                // Pronađite sve selektovane redove
                var selectedItems = ResultsDataGrid.SelectedItems.Cast<PonudaItem>().ToList();

                // Svi redovi koji treba da budu selektovani
                var allSelectedItems = new HashSet<PonudaItem>(selectedItems);

                foreach (var item in selectedItems)
                {
                    // Pronađite sve povezane redove
                    var relatedCodes = item.RelatedFabricki_kod;
                    foreach (var relatedCode in relatedCodes)
                    {
                        var relatedItem = PonudaItems.FirstOrDefault(x => x.Fabricki_kod == relatedCode);
                        if (relatedItem != null)
                        {
                            allSelectedItems.Add(relatedItem);
                        }
                    }
                }

                // Postavite sve redove koje treba da budu selektovani
                ResultsDataGrid.SelectedItems.Clear();
                foreach (var selectedItem in allSelectedItems)
                {
                    ResultsDataGrid.SelectedItems.Add(selectedItem);
                }
            }
            finally
            {
                // Ponovo omogućite događaj
                ResultsDataGrid.SelectionChanged += ResultsDataGrid_SelectionChanged;
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
