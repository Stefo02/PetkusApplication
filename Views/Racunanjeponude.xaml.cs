using PetkusApplication.Models;
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

    public partial class Racunanjeponude : Window
    {
        public Racunanjeponude(List<PonudaItem> selectedItems)
        {
            InitializeComponent();
            SelectedItemsDataGrid.ItemsSource = selectedItems;
        }
    }
}
