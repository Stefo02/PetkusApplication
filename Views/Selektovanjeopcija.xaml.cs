using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PetkusApplication.Views
{
    /// <summary>
    /// Interaction logic for Selektovanjeopcija.xaml
    /// </summary>
    public partial class Selektovanjeopcija : Window
    {
        public string SelectedOption { get; private set; }

        public Selektovanjeopcija(IEnumerable<string> options)
        {
            InitializeComponent();
            OptionsListBox.ItemsSource = options;
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedOption = OptionsListBox.SelectedItem as string;
            DialogResult = true;
        }
    }
}
