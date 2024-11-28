using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
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
    /// <summary>
    /// Interaction logic for AdminWindow.xaml
    /// </summary>
    public partial class AdminWindow : Window
    {
        private List<User> _validUsers;
        private AppDbContext _dbContext;
        public AdminWindow()
        {
            InitializeComponent();
            _dbContextttttt = new AppDbContext(App.GetDbContextOptions());

            LoadUsers();
        }

        private void LoadUsers()
        {
            _validUsers = _dbContext.Users.ToList();
            UpdateUserList();
        }

        private void UpdateUserList()
        {
            // Filter users who are not admins
            var nonAdminUsers = _validUsers.Where(u => !u.IsAdmin).ToList();

            // Display non-admin users
            lstUsers.ItemsSource = null;
            lstUsers.ItemsSource = nonAdminUsers;
        }

        private void AddUserBtn_Click(object sender, RoutedEventArgs e)
        {
            string newUsername = txtNewUsername.Text;
            string newPassword = txtNewPassword.Password;

            if (string.IsNullOrEmpty(newUsername) || string.IsNullOrEmpty(newPassword))
            {
                MessageBox.Show("Polja za korisničko ime i lozinku ne mogu biti prazni.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (_validUsers.Exists(u => u.Username == newUsername))
            {
                MessageBox.Show("Korisničko ime već postoji.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            User newUser = new User
            {
                Username = newUsername,
                Password = newPassword,
                IsAdmin = false
            };

            _dbContext.Users.Add(newUser);
            _dbContext.SaveChanges();

            _validUsers.Add(newUser);
            UpdateUserList();

            MessageBox.Show($"Korisnik '{newUsername}' je dodat uspešno.", "Uspešno", MessageBoxButton.OK, MessageBoxImage.Information);

            txtNewUsername.Text = "";
            txtNewPassword.Password = "";
        }

        private void OpenPopupBtn_Click(object sender, RoutedEventArgs e)
        {
            // Use the DbContextOptions defined in App.xaml.cs
            using (var context = new AppDbContext(App.GetDbContextOptions()))
            {
                // Retrieve data from the audit logs table
                var auditLogs = context.GetAuditLogs();

                // Set the data source for the DataGrid
                auditLogsGrid.ItemsSource = auditLogs;
            }

            // Open the popup
            popupExample.IsOpen = true;
        }

        private void AuditLogsGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            switch (e.Column.Header.ToString())
            {
                case "Username":
                    e.Column.Header = "Korisničko Ime";
                    break;
                case "Action":
                    e.Column.Header = "Akcija";
                    break;
                case "TableAffected":
                    e.Column.Header = "Tabela";
                    break;
                case "Timestamp":
                    e.Column.Header = "Vreme";
                    break;
                case "OldValue":
                    e.Column.Header = "Stare Vrednosti";
                    break;
                case "NewValue":
                    e.Column.Header = "Nove Vrednosti";
                    break;
                case "Id": // Izbaci ID kolonu
                case "RecordId": // Izbaci ID Zapisa kolonu
                case "UserId": // Izbaci UserId kolonu
                    e.Cancel = true;
                    break;
            }
        }

        private void ClosePopupBtn_Click(object sender, RoutedEventArgs e)
        {
            // Zatvori popup
            popupExample.IsOpen = false;
        }

        private void DeleteUserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (lstUsers.SelectedItem == null)
            {
                MessageBox.Show("Izaberite korisnika za brisanje.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            User selectedUser = (User)lstUsers.SelectedItem;

            if (selectedUser.Username == "admin")
            {
                MessageBox.Show("Nije moguće obrisati admin korisnika.", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult result = MessageBox.Show($"Da li ste sigurni da želite obrisati korisnika '{selectedUser.Username}'?", "Obriši", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _dbContext.Users.Remove(selectedUser);
                _dbContext.SaveChanges();

                _validUsers.Remove(selectedUser);
                UpdateUserList();

                MessageBox.Show($"Korisnik '{selectedUser.Username}' je uspešno obrisan.", "Uspešno", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LogOutBtn_Click(object sender, RoutedEventArgs e)
        {
            Login loginWindow = new Login();
            loginWindow.Show();
            this.Close();
        }

    }
}
