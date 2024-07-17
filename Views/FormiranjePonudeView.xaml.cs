using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PetkusApplication.Views
{
    public partial class FormiranjePonudeView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<SelectionData> Selections { get; set; }
        public ObservableCollection<SelectionData2> Selections2 { get; set; }

        public FormiranjePonudeView()
        {
            InitializeComponent();

            Selections = new ObservableCollection<SelectionData>();
            Selections2 = new ObservableCollection<SelectionData2>();

            Selections.Add(new SelectionData());
            Selections2.Add(new SelectionData2());

            DataContext = this;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectionData = Selections[Selections.Count - 1];

                switch (comboBox.Name)
                {
                    case "comboBox1":
                        selectionData.ComboBox1Selection = selectedItem.Content.ToString();
                        break;
                    case "comboBox2":
                        selectionData.ComboBox2Selection = selectedItem.Content.ToString();
                        break;
                    case "comboBox3":
                        selectionData.ComboBox3Selection = selectedItem.Content.ToString();
                        break;
                    case "comboBox4":
                        selectionData.ComboBox4Selection = selectedItem.Content.ToString();
                        break;
                }

                dataGrid1.Items.Refresh();
            }
        }

        private void ComboBox_SelectionChanged2(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var selectionData = Selections2[Selections2.Count - 1];

                switch (comboBox.Name)
                {
                    case "comboBox6":
                        selectionData.ComboBox6Selection = selectedItem.Content.ToString();
                        break;
                    case "comboBox7":
                        selectionData.ComboBox7Selection = selectedItem.Content.ToString();
                        break;
                    case "comboBox8":
                        selectionData.ComboBox8Selection = selectedItem.Content.ToString();
                        break;
                }

                dataGrid2.Items.Refresh();
            }
        }

        private void ConfirmSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            var currentSelectionData = Selections[Selections.Count - 1];

            if (int.TryParse(textBoxNumber.Text, out int enteredNumber))
            {
                currentSelectionData.EnteredNumber = enteredNumber;

                ResetControlsOnTab1();

                Selections.Add(new SelectionData());

                dataGrid1.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Molimo unesite ispravan broj.");
            }
        }

        private void ConfirmSelectionButton2_Click(object sender, RoutedEventArgs e)
        {
            var currentSelectionData = Selections2[Selections2.Count - 1];

            if (int.TryParse(textBoxNumber1.Text, out int enteredNumber))
            {
                currentSelectionData.EnteredNumber = enteredNumber;

                ResetControlsOnTab2();

                Selections2.Add(new SelectionData2());

                dataGrid2.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Molimo unesite ispravan broj.");
            }
        }

        private void ResetControlsOnTab1()
        {
            comboBox1.SelectedIndex = -1;
            comboBox2.SelectedIndex = -1;
            comboBox3.SelectedIndex = -1;
            comboBox4.SelectedIndex = -1;
            textBoxNumber.Text = "";
        }

        private void ResetControlsOnTab2()
        {
            comboBox6.SelectedIndex = -1;
            comboBox7.SelectedIndex = -1;
            comboBox8.SelectedIndex = -1;
            textBoxNumber1.Text = "";
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SelectionData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _comboBox1Selection;
        public string ComboBox1Selection
        {
            get { return _comboBox1Selection; }
            set
            {
                _comboBox1Selection = value;
                OnPropertyChanged(nameof(ComboBox1Selection));
            }
        }

        private string _comboBox2Selection;
        public string ComboBox2Selection
        {
            get { return _comboBox2Selection; }
            set
            {
                _comboBox2Selection = value;
                OnPropertyChanged(nameof(ComboBox2Selection));
            }
        }

        private string _comboBox3Selection;
        public string ComboBox3Selection
        {
            get { return _comboBox3Selection; }
            set
            {
                _comboBox3Selection = value;
                OnPropertyChanged(nameof(ComboBox3Selection));
            }
        }

        private string _comboBox4Selection;
        public string ComboBox4Selection
        {
            get { return _comboBox4Selection; }
            set
            {
                _comboBox4Selection = value;
                OnPropertyChanged(nameof(ComboBox4Selection));
            }
        }

        private int _enteredNumber;
        public int EnteredNumber
        {
            get { return _enteredNumber; }
            set
            {
                _enteredNumber = value;
                OnPropertyChanged(nameof(EnteredNumber));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SelectionData2 : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _comboBox6Selection;
        public string ComboBox6Selection
        {
            get { return _comboBox6Selection; }
            set
            {
                _comboBox6Selection = value;
                OnPropertyChanged(nameof(ComboBox6Selection));
            }
        }

        private string _comboBox7Selection;
        public string ComboBox7Selection
        {
            get { return _comboBox7Selection; }
            set
            {
                _comboBox7Selection = value;
                OnPropertyChanged(nameof(ComboBox7Selection));
            }
        }

        private string _comboBox8Selection;
        public string ComboBox8Selection
        {
            get { return _comboBox8Selection; }
            set
            {
                _comboBox8Selection = value;
                OnPropertyChanged(nameof(ComboBox8Selection));
            }
        }

        private int _enteredNumber;
        public int EnteredNumber
        {
            get { return _enteredNumber; }
            set
            {
                _enteredNumber = value;
                OnPropertyChanged(nameof(EnteredNumber));
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
