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
        private Racunanjeponude racunanjePonudeWindow;
        private HashSet<string> preneseniKodovi = new HashSet<string>();


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
                {"comboBox4", () =>
                    {
                        string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();

                        if (selectedProizvodac == "Siemens")
                        {
                            // Opcije za Siemens
                            SetupComboBox(comboBox4, new[] {"1,5kW", "1,5kW_2", "2,2kW", "2,2kW_2", "3kW", "3kW_2", "4kW", "4kW_2", "5,5kW", "5,5kW_2", "5,5kW_class", "7,5kW", "7,5kW_2", "7,5kW_class",
                    "9,2kW", "9,2kW_2", "9,2kW_class","11kW", "11kW_2", "11kW_class","15kW", "15kW_2", "15kW_class","18,5kW", "18,5kW_2", "18,5kW_class","22kW", "22kW_2", "22kW_class", "30kW", "30kW_2", "30kW_class",
                    "37kW", "37kW_2","45kW", "45kW_2","55kW", "55kW_2","55kW_3","75kW", "75kW_2", "90kW", "90kW_2","110kW", "110kW_2","132kW", "132kW_2","160kW", "160kW_2","200kW", "200kW_2",
                    "250kW", "250kW_2","315kW","355kW","400kW", "400kW_2"});
                        }
                        else if (selectedProizvodac == "Schneider")
                        {
                            // Opcije za Schneider
                            SetupComboBox(comboBox4, new[] {"1,5kW", "2,2kW","3kW", "4kW", "5,5kW", "7,5kW", "11kW", "15kW", "18,5kW", "22kW", "30kW", "37kW", "45kW", "45kW_2", "55kW", "55kW_2", "75kW", "75kW_2",
                     "90kW", "90kW_2", "110kW", "110kW_2", "132kW", "132kW_2", "160kW", "160kW_2", "200kW", "200kW_2", "250kW", "250kW_2", "315kW", "315kW_2", "355kW", "355kW_2"});
                        }
                    }
                }
            }
        },
        {"Soft", new Dictionary<string, Action>
    {
        {"comboBox4", () =>
            {
                string selectedOption = (comboBox3.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (selectedOption == "Soft Starter motor 1 SI")
            {
                SetupComboBox(comboBox4, new[] { "0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW", "5,5kW", "5,5kW_2", "5,5kW_3", "5,5kW_4", "5,5kW_5", "7,5kW", "7,5kW_2", "7,5kW_3", "7,5kW_4", "7,5kW_5", "9,2kW", "9,2kW_2", "9,2kW_3", "9,2kW_4", "9,2kW_5",
                    "11kW", "11kW_2", "11kW_3", "11kW_4", "11kW_5", "15kW", "15kW_2", "15kW_3", "15kW_4", "15kW_5","18,5kW", "18,5kW_2", "18,5kW_3", "18,5kW_4", "18,5kW_5","22kW", "22kW_2", "22kW_3", "22kW_4", "22kW_5", "30kW", "30kW_2", "30kW_3", "30kW_4", "30kW_5",
                    "37kW", "37kW_2", "37kW_3", "37kW_4", "37kW_5","45kW", "45kW_2", "45kW_3", "45kW_4", "45kW_5", "55kW", "55kW_2", "55kW_3", "55kW_4", "55kW_5","55kW_6","75kW", "75kW_2", "75kW_3", "75kW_4", "75kW_5","90kW", "90kW_2", "90kW_3", "90kW_4", "90kW_5",
                    "110kW", "110kW_2", "110kW_3", "110kW_4", "110kW_5","132kW", "132kW_2", "132kW_3", "132kW_4", "132kW_5", "160kW", "160kW_2", "160kW_3", "160kW_4", "160kW_5", "200kW", "200kW_2", "200kW_3", "200kW_4", "200kW_5","250kW", "250kW_2", "250kW_3", "250kW_4", "250kW_5",
                    "315kW", "315kW_2","315kW_3", "315kW_4"});
            }
            else if (selectedOption == "Soft Starter motor 1 SE")
            {
                SetupComboBox(comboBox4, new[] {"15kW", "18_5kW", "22kW", "30kW", "37kW", "45kW", "55kW", "75kW", "75kW_2", "90kW", "90kW_2", "110kW", "110kW_2", "132kW", "132kW_2", "160kW", "160kW_2", 
                    "200kW", "200kW_2", "250kW", "250kW_2", "315kW", "315kW_2"});
            }
            else if (selectedOption == "Soft starter 2 motora (SI)")
            {
                SetupComboBox(comboBox4, new[] { "2x0_55kW", "2x0_75kW", "2x1_1kW","2x1_5kW","2x2_2kW","2x3kW","2x4kW","2x5_5kW","2x7_5kW",
                    "2x11kW","2x15kW","2x18_5kW","2x22kW","2x30kW","2x37kW","2x45kW","2x45kW_2","2x55kW", "2x75kW", "2x75kW_2"});
            }
            }
        }
    }
},
        {
    "Frekventno", new Dictionary<string, Action>
    {
        {"comboBox3", () =>
            {
                string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (selectedProizvodac == "Siemens")
                {
                    SetupComboBox(comboBox3, new[] { "V/F regulatori(V20, Siemens)" });
                }
                else if (selectedProizvodac == "Schneider")
                {
                    SetupComboBox(comboBox3, new[] { "V/F regulatori(ATV310, 320 and 930 Schneider)" });
                }
                else if (selectedProizvodac == "Danfoss")
                {
                    SetupComboBox(comboBox3, new[] { "V/F regulatori(FC51, Danfoss)", "V/F regulatori(FC102, Danfoss)", "V/F regulatori(FC302, Danfoss)" });
                }
            }
        },
        {"comboBox4", () =>
            {
                string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string selectedOption = (comboBox3.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (selectedProizvodac == "Siemens" && selectedOption == "V/F regulatori(V20, Siemens)")
                {
                    SetupComboBox(comboBox4, new[] {"0,12kW","0,25kW","0,37kW","0,55kW","0,75kW","1,1kW","1,5kW","2,2kW","3kW","0,18kW","0,25kW_2","0,37kW_2","0,55kW_2","0,75kW_2",
                        "1,1kW_2","1,5kW_2","2,2kW_2","3kW_2","4kW","5,5kW","5,5kW","7,5kW","9,2kW","11kW","15kW","22kW","30kW"});
                }
                else if (selectedProizvodac == "Schneider" && selectedOption == "V/F regulatori(ATV310, 320 and 930 Schneider)")
                {
                    SetupComboBox(comboBox4, new[] { "0,25kW","0,25kW_2","0,25kW_3","0,37kW","0,37kW_2","0,37kW_3","0,55kW","0,55kW_2","0,55kW_3","0,75kW","0,75kW_2","0,75kW_3","1,1kW","1,1kW_2","1,1kW_3","1,5kW","1,5kW_2","1,5kW_3",
                    "2,2kW","2,2kW_2","2,2kW_3","3kW","3kW_2","3kW_3","4kW","4kW_2","4kW_3","5,5kW","5,5kW_2","5,5kW_3","7,5kW","7,5kW_2","7,5kW_3","9,2kW","9,2kW_2","9,2kW_3","11kW","11kW_2","11kW_3","11kW_4","15kW","15kW_2","15kW_3","15kW_4","18,5kW","18,5kW_2",
                    "22kW","22kW_2","30kW","37kW","45kW","55kW","75kW","90kW"});
                }
                else if (selectedProizvodac == "Danfoss")
{
                    if (selectedOption == "V/F regulatori(FC51, Danfoss)")
                {
                    SetupComboBox(comboBox4, new[] {"0,37kW_FC51","0,55kW_FC51","0,75kW_FC51","1,1kW_FC51","1,5kW_FC51","2,2kW_FC51","3kW_FC51","4kW_FC51","5,5kW_FC51","7,5kW_FC51","9,2kW_FC51",
                        "11kW_FC51","15kW_FC51","18,5kW_FC51","22kW_FC51"});
                }
                else if (selectedOption == "V/F regulatori(FC102, Danfoss)")
                {
                    SetupComboBox(comboBox4, new[] { "1,1kW_FC102","1,5kW_FC102","2,2kW_FC102","3kW_FC102","4kW_FC102","5,5kW_FC102","7,5kW_FC102","9,2kW_FC102",
                        "11kW_FC102","15kW_FC102","18,5kW_FC102","22kW_FC102","30kW_FC102", "37kW_FC102","45kW_FC102","55kW_FC102","75kW_FC102","90kW_FC102"});
                }
                else if (selectedOption == "V/F regulatori(FC302, Danfoss)")
                {
                    SetupComboBox(comboBox4, new[] { "0,37kW_FC302","0,55kW_FC302","0,75kW_FC302","1,1kW_FC302","1,5kW_FC302","2,2kW_FC302","3kW_FC302","4kW_FC302","5,5kW_FC302","7,5kW_FC302","9,2kW_FC302","11kW_FC302",
                    "15kW_FC302","18,5kW_FC302","22kW_FC302","30kW_FC302","37kW_FC302","45kW_FC302","55kW_FC302","75kW_FC302"});
                }
}

            }
        }
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
            // Provera da li je promenjen način pokretanja (comboBox1)
            if (sender == comboBox1)
            {
                string selectedNacinPokretanja = (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString();

                // Provera da li je odabran validan način pokretanja
                if (!string.IsNullOrEmpty(selectedNacinPokretanja) && comboBoxSetups.ContainsKey(selectedNacinPokretanja))
                {
                    ResetComboBoxes(comboBox2, comboBox3, comboBox4);  // Resetovanje ComboBox-ova
                    comboBoxSetups[selectedNacinPokretanja].Invoke();  // Postavljanje opcija za ComboBox-eve
                    UpdateComboBoxVisibility(selectedNacinPokretanja); // Pozivanje funkcije za ažuriranje vidljivosti
                }

            }

            // Posebna logika za "Direktno" način pokretanja
            if (sender == comboBox2 && (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Direktno")
            {
                string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();

                // Reset both comboBox3 and comboBox4 when comboBox2 changes in "Direktno"
                ResetComboBoxes(comboBox3, comboBox4);

                if (selectedProizvodac == "Siemens")
                {
                    SetupComboBox(comboBox3, new[] { "Direktno", "Reverzibilni" });
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,37kW", "110kW" });
                }
                else if (selectedProizvodac == "Schneider")
                {
                    SetupComboBox(comboBox3, new[] { "Direktno", "Reverzibilni" });
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,37kW" });
                }
            }

            // Posebna logika za "Zvezda-Trougao" način pokretanja
            if (sender == comboBox2 && (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Zvezda-Trougao")
            {
                string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (selectedProizvodac == "Siemens")
                {
                    SetupComboBox(comboBox4, new[] { "1,5kW", "1,5kW_2", "2,2kW", "2,2kW_2", "3kW", "3kW_2", "4kW", "4kW_2", "5,5kW", "5,5kW_2", "5,5kW_class", "7,5kW", "7,5kW_2", "7,5kW_class",
                    "9,2kW", "9,2kW_2", "9,2kW_class","11kW", "11kW_2", "11kW_class","15kW", "15kW_2", "15kW_class","18,5kW", "18,5kW_2", "18,5kW_class","22kW", "22kW_2", "22kW_class", "30kW", "30kW_2", "30kW_class",
                    "37kW", "37kW_2","45kW", "45kW_2","55kW", "55kW_2","55kW_3","75kW", "75kW_2", "90kW", "90kW_2","110kW", "110kW_2","132kW", "132kW_2","160kW", "160kW_2","200kW", "200kW_2",
                    "250kW", "250kW_2","315kW","355kW","400kW", "400kW_2"});
                }
                else if (selectedProizvodac == "Schneider")
                {
                    SetupComboBox(comboBox4, new[] {"1,5kW", "2,2kW","3kW", "4kW", "5,5kW", "7,5kW", "11kW", "15kW", "18,5kW", "22kW", "30kW", "37kW", "45kW", "45kW_2", "55kW", "55kW_2", "75kW", "75kW_2",
                     "90kW", "90kW_2", "110kW", "110kW_2", "132kW", "132kW_2", "160kW", "160kW_2", "200kW", "200kW_2", "250kW", "250kW_2", "315kW", "315kW_2", "355kW", "355kW_2"});
                }
            }

            // Posebna logika za "Soft" način pokretanja
            if (sender == comboBox3 && (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Soft")
            {
                string selectedOption = (comboBox3.SelectedItem as ComboBoxItem)?.Content?.ToString();

                // Resetovanje comboBox4 kada se promeni comboBox3 u "Soft" režimu
                ResetComboBoxes(comboBox4);

                if (selectedOption == "Soft Starter motor 1 SI")
                {
                    SetupComboBox(comboBox4, new[] { "0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW", "5,5kW", "5,5kW_2", "5,5kW_3", "5,5kW_4", "5,5kW_5", "7,5kW", "7,5kW_2", "7,5kW_3", "7,5kW_4", "7,5kW_5", "9,2kW", "9,2kW_2", "9,2kW_3", "9,2kW_4", "9,2kW_5",
                    "11kW", "11kW_2", "11kW_3", "11kW_4", "11kW_5", "15kW", "15kW_2", "15kW_3", "15kW_4", "15kW_5","18,5kW", "18,5kW_2", "18,5kW_3", "18,5kW_4", "18,5kW_5","22kW", "22kW_2", "22kW_3", "22kW_4", "22kW_5", "30kW", "30kW_2", "30kW_3", "30kW_4", "30kW_5",
                    "37kW", "37kW_2", "37kW_3", "37kW_4", "37kW_5","45kW", "45kW_2", "45kW_3", "45kW_4", "45kW_5", "55kW", "55kW_2", "55kW_3", "55kW_4", "55kW_5","55kW_6","75kW", "75kW_2", "75kW_3", "75kW_4", "75kW_5","90kW", "90kW_2", "90kW_3", "90kW_4", "90kW_5",
                    "110kW", "110kW_2", "110kW_3", "110kW_4", "110kW_5","132kW", "132kW_2", "132kW_3", "132kW_4", "132kW_5", "160kW", "160kW_2", "160kW_3", "160kW_4", "160kW_5", "200kW", "200kW_2", "200kW_3", "200kW_4", "200kW_5","250kW", "250kW_2", "250kW_3", "250kW_4", "250kW_5",
                    "315kW", "315kW_2","315kW_3", "315kW_4"});
                }
                else if (selectedOption == "Soft Starter motor 1 SE")
                {
                    SetupComboBox(comboBox4, new[] { "15kW", "18_5kW", "22kW", "30kW", "37kW", "45kW", "55kW", "75kW", "75kW_2", "90kW", "90kW_2", "110kW", "110kW_2", "132kW", "132kW_2", 
                        "160kW", "160kW_2", "200kW", "200kW_2", "250kW", "250kW_2", "315kW", "315kW_2"});
                }
                else if (selectedOption == "Soft starter 2 motora (SI)")  // Dodavanje nove opcije
                {
                    SetupComboBox(comboBox4, new[] {"2x0_55kW", "2x0_75kW", "2x1_1kW","2x1_5kW","2x2_2kW","2x3kW","2x4kW","2x5_5kW","2x7_5kW",
                    "2x11kW","2x15kW","2x18_5kW","2x22kW","2x30kW","2x37kW","2x45kW","2x45kW_2","2x55kW", "2x75kW", "2x75kW_2" });
                }
            }

            // Posebna logika za "Frekventno" način pokretanja
            if (sender == comboBox2 && (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Frekventno")
            {
                string selectedProizvodac = (comboBox2.SelectedItem as ComboBoxItem)?.Content?.ToString();
                ResetComboBoxes(comboBox3, comboBox4);

                if (!string.IsNullOrEmpty(selectedProizvodac))
                {
                    if (selectedProizvodac == "Siemens")
                    {
                        SetupComboBox(comboBox3, new[] { "V/F regulatori(V20, Siemens)" });
                    }
                    else if (selectedProizvodac == "Schneider")
                    {
                        SetupComboBox(comboBox3, new[] { "V/F regulatori(ATV310, 320 and 930 Schneider)" });
                    }
                    else if (selectedProizvodac == "Danfoss")
                    {
                        SetupComboBox(comboBox3, new[] { "V/F regulatori(FC51, Danfoss)", "V/F regulatori(FC102, Danfoss)", "V/F regulatori(FC302, Danfoss)" });
                    }
                }
            }

            // Ako se promeni comboBox4, ažuriraj proceduru
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
            string selectedNacinPokretanja = (comboBox1.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // Logika za Siemens i Schneider
            if (selectedProizvodac == "Siemens")
            {
                if (selectedOption == "Direktno")
                {
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,37kW", "0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW", "5,5kW", "5,5kW_class20", "7,5kW", "7,5kW_class20",
        "9,2kW", "9,2kW_class20", "11kW", "11kW_class20", "15kW", "15kW_class20", "18,5kW", "18,5kW_class20", "22kW", "22kW_class20", "30kW", "30kW_class20s", "37kW", "45kW", "55kW_(S3)",
        "55kW_(P)", "75kW", "90kW", "110kW" });
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
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,18kW", "0,25kW", "0,37kW", "0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW", "5,5kW", "5,5kW_2", "7,5kW", "7,5kW_2",
        "9,2kW", "9,2kW_2", "11kW", "11kW_2", "15kW", "15kW_2", "15kW_3", "18,5kW", "18,5kW_2", "22kW", "22kW_2", "30kW", "30kW_2", "37kW", "37kW_2", "45kW", "45kW_2", "55kW", "55kW_2",
        "75kW", "75kW_2", "90kW", "90kW_2", "110kW", "110kW_2" });
                }
                else if (selectedOption == "Reverzibilni")
                {
                    SetupComboBox(comboBox4, new[] { "0,09kW", "0,12kW", "0,18kW", "0,25kW", "0,37kW", "0,55kW", "0,75kW" });
                }
            }

            if (selectedNacinPokretanja == "Zvezda-Trougao")
            {
                if (selectedProizvodac == "Siemens")
                {
                    // Opcije za Siemens: "1,5kW", "2,2kW", "3kW", "4kW"
                    SetupComboBox(comboBox4, new[] { "1,5kW", "1,5kW_2", "2,2kW", "2,2kW_2", "3kW", "3kW_2", "4kW", "4kW_2", "5,5kW", "5,5kW_2", "5,5kW_class", "7,5kW", "7,5kW_2", "7,5kW_class",
                    "9,2kW", "9,2kW_2", "9,2kW_class","11kW", "11kW_2", "11kW_class","15kW", "15kW_2", "15kW_class","18,5kW", "18,5kW_2", "18,5kW_class","22kW", "22kW_2", "22kW_class", "30kW", "30kW_2", "30kW_class",
                    "37kW", "37kW_2","45kW", "45kW_2","55kW", "55kW_2","55kW_3","75kW", "75kW_2", "90kW", "90kW_2","110kW", "110kW_2","132kW", "132kW_2","160kW", "160kW_2","200kW", "200kW_2",
                    "250kW", "250kW_2","315kW","355kW","400kW", "400kW_2"});
                }
                else if (selectedProizvodac == "Schneider")
                {
                    // Opcije za Schneider: "1,5kW", "2,2kW"
                    SetupComboBox(comboBox4, new[] { "1,5kW", "2,2kW","3kW", "4kW", "5,5kW", "7,5kW", "11kW", "15kW", "18,5kW", "22kW", "30kW", "37kW", "45kW", "45kW_2", "55kW", "55kW_2", "75kW", "75kW_2",
                     "90kW", "90kW_2", "110kW", "110kW_2", "132kW", "132kW_2", "160kW", "160kW_2", "200kW", "200kW_2", "250kW", "250kW_2", "315kW", "315kW_2", "355kW", "355kW_2"});
                }
            }
            else if (selectedNacinPokretanja == "Soft")
            {
                if (selectedOption == "Soft Starter motor 1 SI")
                {
                    SetupComboBox(comboBox4, new[] {"0,55kW", "0,75kW", "1,1kW", "1,5kW", "2,2kW", "3kW", "4kW", "5,5kW", "5,5kW_2", "5,5kW_3", "5,5kW_4", "5,5kW_5", "7,5kW", "7,5kW_2", "7,5kW_3", "7,5kW_4", "7,5kW_5", "9,2kW", "9,2kW_2", "9,2kW_3", "9,2kW_4", "9,2kW_5",
                    "11kW", "11kW_2", "11kW_3", "11kW_4", "11kW_5", "15kW", "15kW_2", "15kW_3", "15kW_4", "15kW_5","18,5kW", "18,5kW_2", "18,5kW_3", "18,5kW_4", "18,5kW_5","22kW", "22kW_2", "22kW_3", "22kW_4", "22kW_5", "30kW", "30kW_2", "30kW_3", "30kW_4", "30kW_5",
                    "37kW", "37kW_2", "37kW_3", "37kW_4", "37kW_5","45kW", "45kW_2", "45kW_3", "45kW_4", "45kW_5", "55kW", "55kW_2", "55kW_3", "55kW_4", "55kW_5","55kW_6","75kW", "75kW_2", "75kW_3", "75kW_4", "75kW_5","90kW", "90kW_2", "90kW_3", "90kW_4", "90kW_5",
                    "110kW", "110kW_2", "110kW_3", "110kW_4", "110kW_5","132kW", "132kW_2", "132kW_3", "132kW_4", "132kW_5", "160kW", "160kW_2", "160kW_3", "160kW_4", "160kW_5", "200kW", "200kW_2", "200kW_3", "200kW_4", "200kW_5","250kW", "250kW_2", "250kW_3", "250kW_4", "250kW_5",
                    "315kW", "315kW_2","315kW_3", "315kW_4"});
                }
                else if (selectedOption == "Soft Starter motor 1 SE")
                {
                    SetupComboBox(comboBox4, new[] {"15kW", "18_5kW", "22kW", "30kW", "37kW", "45kW", "55kW", "75kW", "75kW_2", "90kW", "90kW_2", "110kW", "110kW_2", "132kW", "132kW_2", "160kW", "160kW_2",
                    "200kW", "200kW_2", "250kW", "250kW_2", "315kW", "315kW_2"});
                }
                else if (selectedOption == "Soft starter 2 motora (SI)") 
                {
                    SetupComboBox(comboBox4, new[] {"2x0_55kW", "2x0_75kW", "2x1_1kW","2x1_5kW","2x2_2kW","2x3kW","2x4kW","2x5_5kW","2x7_5kW",
                    "2x11kW","2x15kW","2x18_5kW","2x22kW","2x30kW","2x37kW","2x45kW","2x45kW_2","2x55kW", "2x75kW", "2x75kW_2" });
                }
            }
            else if (selectedNacinPokretanja == "Frekventno")
            {
                if (selectedProizvodac == "Siemens" && selectedOption == "V/F regulatori(V20, Siemens)")
                {
                    SetupComboBox(comboBox4, new[] {"0,12kW","0,25kW","0,37kW","0,55kW","0,75kW","1,1kW","1,5kW","2,2kW","3kW","0,18kW","0,25kW_2","0,37kW_2","0,55kW_2","0,75kW_2",
                        "1,1kW_2","1,5kW_2","2,2kW_2","3kW_2","4kW","5,5kW","5,5kW","7,5kW","9,2kW","11kW","15kW","22kW","30kW"});
                }
                else if (selectedProizvodac == "Schneider" && selectedOption == "V/F regulatori(ATV310, 320 and 930 Schneider)")
                {
                    SetupComboBox(comboBox4, new[] {"0,25kW","0,25kW_2","0,25kW_3","0,37kW","0,37kW_2","0,37kW_3","0,55kW","0,55kW_2","0,55kW_3","0,75kW","0,75kW_2","0,75kW_3","1,1kW","1,1kW_2","1,1kW_3","1,5kW","1,5kW_2","1,5kW_3",
                    "2,2kW","2,2kW_2","2,2kW_3","3kW","3kW_2","3kW_3","4kW","4kW_2","4kW_3","5,5kW","5,5kW_2","5,5kW_3","7,5kW","7,5kW_2","7,5kW_3","9,2kW","9,2kW_2","9,2kW_3","11kW","11kW_2","11kW_3","11kW_4","15kW","15kW_2","15kW_3","15kW_4","18,5kW","18,5kW_2",
                    "22kW","22kW_2","30kW","37kW","45kW","55kW","75kW","90kW"});
                }
                else if (selectedProizvodac == "Danfoss")
                {
                    if (selectedOption == "V/F regulatori(FC51, Danfoss)")
                    {
                        SetupComboBox(comboBox4, new[] { "0,37kW_FC51","0,55kW_FC51","0,75kW_FC51","1,1kW_FC51","1,5kW_FC51","2,2kW_FC51","3kW_FC51","4kW_FC51","5,5kW_FC51","7,5kW_FC51","9,2kW_FC51",
                        "11kW_FC51","15kW_FC51","18,5kW_FC51","22kW_FC51" });
                    }
                    else if (selectedOption == "V/F regulatori(FC102, Danfoss)")
                    {
                        SetupComboBox(comboBox4, new[] {"1,1kW_FC102","1,5kW_FC102","2,2kW_FC102","3kW_FC102","4kW_FC102","5,5kW_FC102","7,5kW_FC102","9,2kW_FC102",
                        "11kW_FC102","15kW_FC102","18,5kW_FC102","22kW_FC102","30kW_FC102", "37kW_FC102","45kW_FC102","55kW_FC102","75kW_FC102","90kW_FC102"});
                    }
                    else if (selectedOption == "V/F regulatori(FC302, Danfoss)")
                    {
                        SetupComboBox(comboBox4, new[] {"0,37kW_FC302","0,55kW_FC302","0,75kW_FC302","1,1kW_FC302","1,5kW_FC302","2,2kW_FC302","3kW_FC302","4kW_FC302","5,5kW_FC302","7,5kW_FC302","9,2kW_FC302","11kW_FC302",
                    "15kW_FC302","18,5kW_FC302","22kW_FC302","30kW_FC302","37kW_FC302","45kW_FC302","55kW_FC302","75kW_FC302" });
                    }
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

            if (selectedNacinPokretanja == "Zvezda-Trougao")
            {
                BrojSmerovaPanel.Visibility = Visibility.Collapsed; // Sakrij ceo StackPanel za "Broj smerova" i comboBox3
            }
            else
            {
                BrojSmerovaPanel.Visibility = Visibility.Visible;   // Prikaži StackPanel za druge opcije
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
            SetupComboBox(comboBox3, new[] { "Soft Starter motor 1 SI", "Soft Starter motor 1 SE", "Soft starter 2 motora (SI)" });
        }

        private void SetupForFrekventno()
        {
            // Postavljanje opcija za comboBox2 (proizvođači)
            SetupComboBox(comboBox2, new[] { "Siemens", "Schneider", "Danfoss" });
            // Ne postavljamo comboBox3 ovde jer korisnik još nije izabrao proizvođača
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
                ("Zvezda-Trougao", "Siemens") => selectedSnaga switch
                {
                    "1,5kW" => "YD_si_start_1_5kW",
                    "1,5kW_2" => "YD_si_start_1_5kW2",
                    "2,2kW" => "YD_si_start_2_2kW",
                    "2,2kW_2" => "YD_si_start_2_2kW2",
                    "3kW" => "YD_si_start_3kW",
                    "3kW_2" => "YD_si_start_3kW2",
                    "4kW" => "YD_si_start_4kW",
                    "4kW_2" => "YD_si_start_4kW2",
                    "5,5kW" => "YD_si_start_5_5kW",
                    "5,5kW_2" => "YD_si_start_5_5kW2",
                    "5,5kW_class" => "YD_si_start_5_5kWclass",
                    "7,5kW" => "YD_si_start_7_5kW",
                    "7,5kW_2" => "YD_si_start_7_5kW2",
                    "7,5kW_class" => "YD_si_start_7_5kWclass",
                    "9,2kW" => "YD_si_start_9_2kW",
                    "9,2kW_2" => "YD_si_start_9_2kW2",
                    "9,2kW_class" => "YD_si_start_9_2kWclass",
                    "11kW" => "YD_si_start_11kW",
                    "11kW_2" => "YD_si_start_11kW2",
                    "11kW_class" => "YD_si_start_11kWclass",
                    "15kW" => "YD_si_start_15kW",
                    "15kW_2" => "YD_si_start_15kW2",
                    "15kW_class" => "YD_si_start_15kWclass",
                    "18,5kW" => "YD_si_start_18_5kW",
                    "18,5kW_2" => "YD_si_start_18_5kW2",
                    "18,5kW_class" => "YD_si_start_18_5kWclass",
                    "22kW" => "YD_si_start_22kW",
                    "22kW_2" => "YD_si_start_22kW2",
                    "22kW_class" => "YD_si_start_22kWclass",
                    "30kW" => "YD_si_start_30kW",
                    "30kW_2" => "YD_si_start_30kW2",
                    "30kW_class" => "YD_si_start_30kWclass",
                    "37kW" => "YD_si_start_37kW",
                    "37kW_2" => "YD_si_start_37kW2",
                    "45kW" => "YD_si_start_45kW",
                    "45kW_2" => "YD_si_start_45kW2",
                    "55kW" => "YD_si_start_55kW",
                    "55kW_2" => "YD_si_start_55kW2",
                    "55kW_3" => "YD_si_start_55kW3",
                    "75kW" => "YD_si_start_75kW",
                    "75kW_2" => "YD_si_start_75kW2",
                    "90kW" => "YD_si_start_90kW",
                    "90kW_2" => "YD_si_start_90kW2",
                    "110kW" => "YD_si_start_110kW",
                    "110kW_2" => "YD_si_start_110kW2",
                    "132kW" => "YD_si_start_132kw",
                    "132kW_2" => "YD_si_start_132kW2",
                    "160kW" => "YD_si_start_160kw",
                    "160kW_2" => "YD_si_start_160kw2",
                    "200kW" => "YD_si_start_200kw",
                    "200kW_2" => "YD_si_start_200kw2",
                    "250kW" => "YD_si_start_250kw",
                    "250kW_2" => "YD_si_start_250kw2",
                    "315kW" => "YD_si_start_315kw",
                    "355kW" => "YD_si_start_355kw",
                    "400kW" => "YD_si_start_400kw",
                    "400kW_2" => "YD_si_start_400kw2",
                    _ => "YD_si_start"
                },
                // Zvezda-Trougao Schneider
                ("Zvezda-Trougao", "Schneider") => selectedSnaga switch
                {
                    "1,5kW" => "YD_se_start_1_5kW",
                    "2,2kW" => "YD_se_start_2_2kW",
                    "3kW" => "YD_se_start_3kW",
                    "4kW" => "YD_se_start_4kW",
                    "5,5kW" => "YD_se_start_5_5kW",
                    "7,5kW" => "YD_se_start_7_5kW",
                    "11kW" => "YD_se_start_11kW",
                    "15kW" => "YD_se_start_15kW",
                    "18,5kW" => "YD_se_start_18_5kW",
                    "22kW" => "YD_se_start_22kW",
                    "30kW" => "YD_se_start_30kW",
                    "37kW" => "YD_se_start_37kW",
                    "45kW" => "YD_se_start_45kW",
                    "45kW_2" => "YD_se_start_45kW2",
                    "55kW" => "YD_se_start_55kW",
                    "55kW_2" => "YD_se_start_55kW2",
                    "75kW" => "YD_se_start_75kW",
                    "75kW_2" => "YD_se_start_7 5kW2",
                    "90kW" => "YD_se_start_90kW",
                    "90kW_2" => "YD_se_start_90kW2",
                    "110kW" => "YD_se_start_110kW",
                    "110kW_2" => "YD_se_start_110kW2",
                    "132kW" => "YD_se_start_132kW",
                    "132kW_2" => "YD_se_start_132kW2",
                    "160kW" => "YD_se_start_160kW",
                    "160kW_2" => "YD_se_start_160kW2",
                    "200kW" => "YD_se_start_200kW",
                    "200kW_2" => "YD_se_start_200kW2",
                    "250kW" => "YD_se_start_250kW",
                    "250kW_2" => "YD_se_start_250kW2",
                    "315kW" => "YD_se_start_315kW",
                    "315kW_2" => "YD_se_start_315kW2",
                    "355kW" => "YD_se_start_355kW",
                    "355kW_2" => "YD_se_start_355kW",
                    _ => "YD_se_start"
                },
                ("Soft", _) => selectedBrojSmerova switch
                {
                    "Soft Starter motor 1 SI" => selectedSnaga switch
                    {
                        "0,55kW" => "Soft_starter1_0_55kW",
                        "0,75kW" => "Soft_starter1_0_75kW",
                        "1,1kW" => "Soft_starter1_1_1kW",
                        "1,5kW" => "Soft_starter1_1_5kW",
                        "2,2kW" => "Soft_starter1_2_2kW",
                        "3kW" => "Soft_starter1_3kW",
                        "4kW" => "Soft_starter1_4kW",
                        "5,5kW" => "Soft_starter1_5_5kW",
                        "5,5kW_2" => "Soft_starter1_5_5kW2",
                        "5,5kW_3" => "Soft_starter1_5_5kW3",
                        "5,5kW_4" => "Soft_starter1_5_5kW4",
                        "5,5kW_5" => "Soft_starter1_5_5kW5",
                        "7,5kW" => "Soft_starter1_7_5kW",
                        "7,5kW_2" => "Soft_starter1_7_5kW2",
                        "7,5kW_3" => "Soft_starter1_7_5kW2",
                        "7,5kW_4" => "Soft_starter1_7_5kW2",
                        "7,5kW_5" => "Soft_starter1_7_5kW2",
                        "9,2kW" => "Soft_starter1_9_2kW",
                        "9,2kW_2" => "Soft_starter1_9_2kW2",
                        "9,2kW_3" => "Soft_starter1_9_2kW3",
                        "9,2kW_4" => "Soft_starter1_9_2kW4",
                        "9,2kW_5" => "Soft_starter1_9_2kW5",
                        "11kW" => "Soft_starter1_11kW",
                        "11kW_2" => "Soft_starter1_11kW2",
                        "11kW_3" => "Soft_starter1_11kW3",
                        "11kW_4" => "Soft_starter1_11kW4",
                        "11kW_5" => "Soft_starter1_11kW5",
                        "15kW" => "Soft_starter1_15kW",
                        "15kW_2" => "Soft_starter1_15kW2",
                        "15kW_3" => "Soft_starter1_15kW3",
                        "15kW_4" => "Soft_starter1_15kW4",
                        "15kW_5" => "Soft_starter1_15kW5",
                        "18,5kW" => "Soft_starter1_18_5kW",
                        "18,5kW_2" => "Soft_starter1_18_5kW2",
                        "18,5kW_3" => "Soft_starter1_18_5kW3",
                        "18,5kW_4" => "Soft_starter1_18_5kW4",
                        "18,5kW_5" => "Soft_starter1_18_5kW5",
                        "22kW" => "Soft_starter1_22kW",
                        "22kW_2" => "Soft_starter1_22kW2",
                        "22kW_3" => "Soft_starter1_22kW3",
                        "22kW_4" => "Soft_starter1_22kW4",
                        "22kW_5" => "Soft_starter1_22kW5",
                        "30kW" => "Soft_starter1_30kW",
                        "30kW_2" => "Soft_starter1_30kW2",
                        "30kW_3" => "Soft_starter1_30kW3",
                        "30kW_4" => "Soft_starter1_30kW4",
                        "30kW_5" => "Soft_starter1_30kW5",
                        "37kW" => "Soft_starter1_37kW",
                        "37kW_2" => "Soft_starter1_37kW2",
                        "37kW_3" => "Soft_starter1_37kW3",
                        "37kW_4" => "Soft_starter1_37kW4",
                        "37kW_5" => "Soft_starter1_37kW5",
                        "45kW" => "Soft_starter1_45kW",
                        "45kW_2" => "Soft_starter1_45kW2",
                        "45kW_3" => "Soft_starter1_45kW3",
                        "45kW_4" => "Soft_starter1_45kW4",
                        "45kW_5" => "Soft_starter1_45kW5",
                        "55kW" => "Soft_starter1_55kW",
                        "55kW_2" => "Soft_starter1_55kW2",
                        "55kW_3" => "Soft_starter1_55kW3",
                        "55kW_4" => "Soft_starter1_55kW4",
                        "55kW_5" => "Soft_starter1_55kW5",
                        "55kW_6" => "Soft_starter1_55kW6",
                        "75kW" => "Soft_starter1_75kW",
                        "75kW_2" => "Soft_starter1_75kW2",
                        "75kW_3" => "Soft_starter1_75kW3",
                        "75kW_4" => "Soft_starter1_75kW4",
                        "75kW_5" => "Soft_starter1_75kW5",
                        "90kW" => "Soft_starter1_90kW",
                        "90kW_2" => "Soft_starter1_90kW2",
                        "90kW_3" => "Soft_starter1_90kW3",
                        "90kW_4" => "Soft_starter1_90kW4",
                        "90kW_5" => "Soft_starter1_90kW5",
                        "110kW" => "Soft_starter1_110kW",
                        "110kW_2" => "Soft_starter1_110kW2",
                        "110kW_3" => "Soft_starter1_110kW3",
                        "110kW_4" => "Soft_starter1_110kW4",
                        "110kW_5" => "Soft_starter1_110kW5",
                        "132kW" => "Soft_starter1_132kW",
                        "132kW_2" => "Soft_starter1_132kW2",
                        "132kW_3" => "Soft_starter1_132kW3",
                        "132kW_4" => "Soft_starter1_132kW4",
                        "132kW_5" => "Soft_starter1_132kW5",
                        "160kW" => "Soft_starter1_160kW",
                        "160kW_2" => "Soft_starter1_160kW2",
                        "160kW_3" => "Soft_starter1_160kW3",
                        "160kW_4" => "Soft_starter1_160kW4",
                        "160kW_5" => "Soft_starter1_160kW5",
                        "200kW" => "Soft_starter1_200kW",
                        "200kW_2" => "Soft_starter1_200kW2",
                        "200kW_3" => "Soft_starter1_200kW3",
                        "200kW_4" => "Soft_starter1_200kW4",
                        "200kW_5" => "Soft_starter1_200kW5",
                        "250kW" => "Soft_starter1_250kW",
                        "250kW_2" => "Soft_starter1_250kW2",
                        "250kW_3" => "Soft_starter1_250kW3",
                        "250kW_4" => "Soft_starter1_250kW4",
                        "250kW_5" => "Soft_starter1_250kW5",
                        "315kW" => "Soft_starter1_315kW",
                        "315kW_2" => "Soft_starter1_315kW2",
                        "315kW_3" => "Soft_starter1_315kW3",
                        "315kW_4" => "Soft_starter1_315kW4",
                        _ => null
                    },
                    "Soft Starter motor 1 SE" => selectedSnaga switch
                    {
                        "15kW" => "Soft_starter1_SE_15kW",
                        "18,5kW" => "Soft_starter1_SE_18_5kW",
                        "22kW" => "Soft_starter1_SE_22kW",
                        "30kW" => "Soft_starter1_SE_30kW",
                        "37kW" => "Soft_starter1_SE_37kW",
                        "45kW" => "Soft_starter1_SE_45kW",
                        "55kW" => "Soft_starter1_SE_55kW",
                        "75kW" => "Soft_starter1_SE_75kW",
                        "75kW_2" => "Soft_starter1_SE_75kW2",
                        "90kW" => "Soft_starter1_SE_90kW",
                        "90kW_2" => "Soft_starter1_SE_90kW2",
                        "110kW" => "Soft_starter1_SE_110kW",
                        "110kW_2" => "Soft_starter1_SE_110kW2",
                        "132kW" => "Soft_starter1_SE_132kW",
                        "132kW_2" => "Soft_starter1_SE_132kW2",
                        "160kW" => "Soft_starter1_SE_160kW",
                        "160kW_2" => "Soft_starter1_SE_160kW2",
                        "200kW" => "Soft_starter1_SE_200kW",
                        "200kW_2" => "Soft_starter1_SE_200kW2",
                        "250kW" => "Soft_starter1_SE_250kW",
                        "250kW_2" => "Soft_starter1_SE_250kW2",
                        "315kW" => "Soft_starter1_SE_315kW",
                        "315kW_2" => "Soft_starter1_SE_315kW2",
                        _ => null
                    },
                    "Soft starter 2 motora (SI)" => selectedSnaga switch
                    {
                        "2x0_55kW" => "Soft_starter2_SI_2x0_55kW",
                        "2x0_75kW" => "Soft_starter2_SI_2x0_75kW",
                        "2x1_1kW" => "Soft_starter2_SI_2x1_1kW",
                        "2x1_5kW" => "Soft_starter2_SI_2x1_5kW",
                        "2x2_2kW" => "Soft_starter2_SI_2x2_2kW",
                        "2x3kW" => "Soft_starter2_SI_2x3kW",
                        "2x4kW" => "Soft_starter2_SI_2x4kW",
                        "2x5_5kW" => "Soft_starter2_SI_2x5_5kW",
                        "2x7_5kW" => "Soft_starter2_SI_2x7_5kW",
                        "2x11kW" => "Soft_starter2_SI_2x11kW",
                        "2x15kW" => "Soft_starter2_SI_2x15kW",
                        "2x18_5kW" => "Soft_starter2_SI_2x18_5kW",
                        "2x22kW" => "Soft_starter2_SI_2x22kW",
                        "2x30kW" => "Soft_starter2_SI_2x30kW",
                        "2x37kW" => "Soft_starter2_SI_2x37kW",
                        "2x45kW" => "Soft_starter2_SI_2x45kW",
                        "2x45kW_2" => "Soft_starter2_SI_2x45kW2",
                        "2x55kW" => "Soft_starter2_SI_2x55kW",
                        "2x75kW" => "Soft_starter2_SI_2x75kW",
                        "2x75kW_2" => "Soft_starter2_SI_2x75kW2",
                        _ => null
                    },
                    _ => null
                },
                ("Frekventno", "Siemens") => selectedSnaga switch
                {
                    "0,12kW" => "FC_si_regulatoriV20_0_12kW",
                    "0,18kW" => "FC_si_regulatoriV20_0_18kW",
                    "0,25kW" => "FC_si_regulatoriV20_0_25kW",
                    "0,25kW_2" => "FC_si_regulatoriV20_0_25kW2",
                    "0,37kW" => "FC_si_regulatoriV20_0_37kW",
                    "0,37kW_2" => "FC_si_regulatoriV20_0_37kW2",
                    "0,55kW" => "FC_si_regulatoriV20_0_55kW",
                    "0,55kW_2" => "FC_si_regulatoriV20_0_55kW2",
                    "0,75kW" => "FC_si_regulatoriV20_0_75kW",
                    "0,75kW_2" => "FC_si_regulatoriV20_0_75kW2",
                    "1,1kW" => "FC_si_regulatoriV20_1_1kW",
                    "1,1kW_2" => "FC_si_regulatoriV20_1_1kW2",
                    "1,5kW" => "FC_si_regulatoriV20_1_5kW",
                    "1,5kW_2" => "FC_si_regulatoriV20_1_5kW2",
                    "2,2kW" => "FC_si_regulatoriV20_2_2kW",
                    "2,2kW_2" => "FC_si_regulatoriV20_2_2kW2",
                    "3kW" => "FC_si_regulatoriV20_3kW",
                    "3kW_2" => "FC_si_regulatoriV20_3kW2",
                    "4kW" => "FC_si_regulatoriV20_4kW",
                    "5,5kW" => "FC_si_regulatoriV20_5_5kW",
                    "7,5kW" => "FC_si_regulatoriV20_7_5kW",
                    "9,2kW" => "FC_si_regulatoriV20_9_2kW",
                    "11kW" => "FC_si_regulatoriV20_11kW",
                    "15kW" => "FC_si_regulatoriV20_15kW",
                    "22kW" => "FC_si_regulatoriV20_22kW",
                    "30kW" => "FC_si_regulatoriV20_30kW",
                    _ => null
                },
                ("Frekventno", "Schneider") => selectedSnaga switch
                {
                    "0,25kW" => "FC_se_regulatori_0_25kW",
                    "0,25kW_2" => "FC_se_regulatori_0_25kW2",
                    "0,25kW_3" => "FC_se_regulatori_0_25kW3",
                    "0,37kW" => "FC_se_regulatori_0_37kW",
                    "0,37kW_2" => "FC_se_regulatori_0_37kW2",
                    "0,37kW_3" => "FC_se_regulatori_0_37kW3",
                    "0,55kW" => "FC_se_regulatori_0_55kW",
                    "0,55kW_2" => "FC_se_regulatori_0_55kW2",
                    "0,55kW_3" => "FC_se_regulatori_0_55kW3",
                    "0,75kW" => "FC_se_regulatori_0_75kW",
                    "0,75kW_2" => "FC_se_regulatori_0_75kW2",
                    "0,75kW_3" => "FC_se_regulatori_0_75kW3",
                    "1,1kW" => "FC_se_regulatori_1_1kW",
                    "1,1kW_2" => "FC_se_regulatori_1_1kW2",
                    "1,1kW_3" => "FC_se_regulatori_1_1kW3",
                    "1,5kW" => "FC_se_regulatori_1_5kW",
                    "1,5kW_2" => "FC_se_regulatori_1_5kW2",
                    "1,5kW_3" => "FC_se_regulatori_1_5kW3",
                    "2,2kW" => "FC_se_regulatori_2_2kW",
                    "2,2kW_2" => "FC_se_regulatori_2_2kW2",
                    "2,2kW_3" => "FC_se_regulatori_2_2kW3",
                    "3kW" => "FC_se_regulatori_3kW",
                    "3kW_2" => "FC_se_regulatori_3kW2",
                    "3kW_3" => "FC_se_regulatori_3kW3",
                    "4kW" => "FC_se_regulatori_4kW",
                    "4kW_2" => "FC_se_regulatori_4kW2",
                    "4kW_3" => "FC_se_regulatori_4kW3",
                    "5,5kW" => "FC_se_regulatori_5_5kW",
                    "5,5kW_2" => "FC_se_regulatori_5_5kW2",
                    "5,5kW_3" => "FC_se_regulatori_5_5kW3",
                    "7,5kW" => "FC_se_regulatori_7_5kW",
                    "7,5kW_2" => "FC_se_regulatori_7_5kW2",
                    "7,5kW_3" => "FC_se_regulatori_7_5kW3",
                    "11kW" => "FC_se_regulatori_11kW",
                    "11kW_2" => "FC_se_regulatori_11kW2",
                    "11kW_3" => "FC_se_regulatori_11kW3",
                    "11kW_4" => "FC_se_regulatori_11kW4",
                    "15kW" => "FC_se_regulatori_15kW",
                    "15kW_2" => "FC_se_regulatori_15kW2",
                    "15kW_3" => "FC_se_regulatori_15kW3",
                    "15kW_4" => "FC_se_regulatori_15kW4",
                    "18,5kW" => "FC_se_regulatori_18_5kW",
                    "18,5kW_2" => "FC_se_regulatori_18_5kW2",
                    "22kW" => "FC_se_regulatori_22kW",
                    "22kW_2" => "FC_se_regulatori_22kW2",
                    "30kW" => "FC_se_regulatori_30kW",
                    "37kW" => "FC_se_regulatori_37kW",
                    "45kW" => "FC_se_regulatori_45kW",
                    "55kW" => "FC_se_regulatori_55kW",
                    "75kW" => "FC_se_regulatori_75kW",
                    "90kW" => "FC_se_regulatori_90kW",
                    _ => null
                },
                ("Frekventno", "Danfoss") => selectedSnaga switch
                {
                    "0,37kW_FC51" => "FC_d_regulatori1_0_37kW",
                    "0,55kW_FC51" => "FC_d_regulatori1_0_55kW",
                    "0,75kW_FC51" => "FC_d_regulatori1_0_75kW",
                    "1,1kW_FC51" => "FC_d_regulatori1_1_1kW",
                    "1,5kW_FC51" => "FC_d_regulatori1_1_5kW",
                    "2,2kW_FC51" => "FC_d_regulatori1_2_2kW",
                    "3kW_FC51" => "FC_d_regulatori1_3kW",
                    "4kW_FC51" => "FC_d_regulatori1_4kW",
                    "5,5kW_FC51" => "FC_d_regulatori1_5_5kW",
                    "7,5W_FC51" => "FC_d_regulatori1_7_5kW",
                    "9,2W_FC51" => "FC_d_regulatori1_9_2kW",
                    "11kW_FC51" => "FC_d_regulatori1_11kW",
                    "15kW_FC51" => "FC_d_regulatori1_15kW",
                    "18,5kW_FC51" => "FC_d_regulatori1_18_5kW",
                    "22kW_FC51" => "FC_d_regulatori1_22kW",
                    "1,1kW_FC102" => "FC_d_regulatori2_1_1kW",
                    "1,5kW_FC102" => "FC_d_regulatori2_1_5kW",
                    "2,2kW_FC102" => "FC_d_regulatori2_2_2kW",
                    "3kW_FC102" => "FC_d_regulatori2_3kW",
                    "4kW_FC102" => "FC_d_regulatori2_4kW",
                    "5,5kW_FC102" => "FC_d_regulatori2_5_5kW",
                    "7,5kW_FC102" => "FC_d_regulatori2_7_5kW",
                    "9,2kW_FC102" => "FC_d_regulatori2_9_2kW",
                    "11kW_FC102" => "FC_d_regulatori2_11kW",
                    "15kW_FC102" => "FC_d_regulatori2_15kW",
                    "18,5kW_FC102" => "FC_d_regulatori2_18_5kW",
                    "22kW_FC102" => "FC_d_regulatori2_22kW",
                    "30kW_FC102" => "FC_d_regulatori2_30kW",
                    "37kW_FC102" => "FC_d_regulatori2_37kW",
                    "45kW_FC102" => "FC_d_regulatori2_45kW",
                    "55kW_FC102" => "FC_d_regulatori2_55kW",
                    "75kW_FC102" => "FC_d_regulatori2_75kW",
                    "90kW_FC102" => "FC_d_regulatori2_90kW",
                    "0,37kW_FC302" => "FC_d_regulatori3_0_37kW",
                    "0,55kW_FC302" => "FC_d_regulatori3_0_55kW",
                    "0,75kW_FC302" => "FC_d_regulatori3_0_75kW",
                    "1,1kW_FC302" => "FC_d_regulatori3_1_1kW",
                    "1,5kW_FC302" => "FC_d_regulatori3_1_5kW",
                    "2,2kW_FC302" => "FC_d_regulatori3_2_2kW",
                    "3kW_FC302" => "FC_d_regulatori3_3kW",
                    "4kW_FC302" => "FC_d_regulatori3_4kW",
                    "5,5kW_FC302" => "FC_d_regulatori3_5_5kW",
                    "7,5kW_FC302" => "FC_d_regulatori3_7_5kW",
                    "9,2kW_FC302" => "FC_d_regulatori3_9_2kW",
                    "11kW_FC302" => "FC_d_regulatori3_11kW",
                    "15kW_FC302" => "FC_d_regulatori3_15kW",
                    "18,5kW_FC302" => "FC_d_regulatori3_18_5kW",
                    "22kW_FC302" => "FC_d_regulatori3_22kW",
                    "30kW_FC302" => "FC_d_regulatori3_30kW",
                    "37kW_FC302" => "FC_d_regulatori3_37kW",
                    "45kW_FC302" => "FC_d_regulatori3_45kW",
                    "55kW_FC302" => "FC_d_regulatori3_55kW",
                    "75kW_FC302" => "FC_d_regulatori3_75kW",
                    _ => null
                },
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
                // Check if the item exists in GroupedItems (even if it was deleted before, allow re-adding)
                var existingGroupedItem = GroupedItems.FirstOrDefault(g => g.GroupName == item.Fabricki_kod);
                if (existingGroupedItem == null)
                {
                    // Add the item if it's not already present
                    allGroupedItems.Add(item);  // Store in case we need to re-add later
                    GroupedItems.Add(new GroupedItem
                    {
                        Opis = item.Opis,
                        GroupName = item.Fabricki_kod,
                        Quantity = 0 // Adjust logic if needed
                    });
                }
                else
                {
                    
                    continue;
                }
            }

            // Optionally clear the selection from ResultsDataGrid after moving
            ResultsDataGrid.SelectedItems.Clear();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (GroupedItems == null || GroupedItems.Count == 0)
            {
                MessageBox.Show("Nema odabranih podataka!", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Prekini metodu ako nema podataka
            }

            // Proveri da li postoji količina 0
            bool hasZeroQuantity = GroupedItems.Any(item => item.Quantity <= 0);
            if (hasZeroQuantity)
            {
                MessageBox.Show("Nije moguće nastaviti jer je broj komada 0.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; // Prekini metodu ako je količina 0
            }

            // Kreiraj listu za nove stavke koje još nisu prenesene
            List<PonudaItem> noviSelectedItems = new List<PonudaItem>();

            // Prolazimo kroz sve stavke u GroupedDataGrid
            foreach (var groupedItem in GroupedItems)
            {
                string fabrickiKod = groupedItem.GroupName;

                // Proveri da li je fabrički kod već prenesen
                if (!preneseniKodovi.Contains(fabrickiKod))
                {
                    // SQL upit za multiplikator
                    string query = $"SELECT Multiplikator FROM FabrickiKodovi WHERE Fabricki_kod = @fabrickiKod";

                    // Defaultna vrednost za KolicinaZaNarucivanje je broj koji je uneo korisnik (broj komada)
                    int brojKomada = groupedItem.Quantity;
                    int ukupnaKolicina = brojKomada;  // Ako nema multiplikatora, ovo će ostati nepromenjeno

                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        // Parametrizovan upit
                        cmd.Parameters.AddWithValue("@fabrickiKod", fabrickiKod);

                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            // Ako multiplikator postoji, množi sa brojem komada
                            int multiplikator = Convert.ToInt32(result);
                            ukupnaKolicina = brojKomada * multiplikator;
                        }
                    }

                    // Pronađi originalni PonudaItem
                    var matchingPonudaItem = allGroupedItems.FirstOrDefault(p => p.Fabricki_kod == fabrickiKod);
                    if (matchingPonudaItem != null)
                    {
                        PonudaItem itemForOrder = new PonudaItem
                        {
                            Fabricki_kod = matchingPonudaItem.Fabricki_kod,
                            Opis = matchingPonudaItem.Opis,
                            Puna_cena = matchingPonudaItem.Puna_cena,
                            Dimenzije = matchingPonudaItem.Dimenzije,
                            Disipacija = matchingPonudaItem.Disipacija,
                            Tezina = matchingPonudaItem.Tezina,
                            KolicinaZaNarucivanje = ukupnaKolicina // Osiguraj da je ukupnaKolicina ispravna i nije 0
                        };

                        // Ostatak koda za izračunavanje vrednosti
                        itemForOrder.Ukupna_puna = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Puna_cena;
                        itemForOrder.Ukupna_rabat = Math.Round(itemForOrder.KolicinaZaNarucivanje * itemForOrder.Puna_cena * (1 - itemForOrder.Vrednost_rabata), 2);
                        itemForOrder.Ukupna_Disipacija = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Disipacija;
                        itemForOrder.Ukupna_Tezina = itemForOrder.KolicinaZaNarucivanje * itemForOrder.Tezina;

                        noviSelectedItems.Add(itemForOrder);
                    }

                }
            }

            // Proveri da li postoje novi redovi za prenos
            if (noviSelectedItems.Count == 0)
            {
                MessageBox.Show("Nema novih stavki za prenos.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Proveri da li je prozor RacunanjePonude već otvoren
            if (racunanjePonudeWindow != null)
            {
                racunanjePonudeWindow.Close(); // Zatvori stari prozor
                racunanjePonudeWindow = null;
            }
            // Kreiraj listu svih prenesenih stavki
            List<PonudaItem> svePreneseneStavke = new List<PonudaItem>();

            // Dodaj sve prenesene kodove koji su već u listi
            svePreneseneStavke.AddRange(preneseniKodovi.Select(kod => allGroupedItems.First(p => p.Fabricki_kod == kod)));

            // Dodaj nove stavke koje nisu ranije prenesene
            svePreneseneStavke.AddRange(noviSelectedItems);

            // Otvori novi prozor sa novim stavkama
            racunanjePonudeWindow = new Racunanjeponude(this, svePreneseneStavke);
            racunanjePonudeWindow.PonudaItems = new ObservableCollection<PonudaItem>(svePreneseneStavke);
            racunanjePonudeWindow.Closed += (s, args) => racunanjePonudeWindow = null;
            racunanjePonudeWindow.Show();

        }

        private void GroupedDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Proveri sve redove u DataGrid-u da li su uneti validni brojevi veći od 0
            bool allValid = GroupedItems.All(item => item.Quantity > 0);

            // Omogući dugme ako su svi unosi ispravni, tj. ako su svi brojevi veći od 0
            potvrdiButton.IsEnabled = allValid;
        }

        private void GroupedDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

    }
}