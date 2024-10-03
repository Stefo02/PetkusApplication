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
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32)); // Adjust version as per your MySQL server version
            optionsBuilder.UseMySql("Server=localhost;Database=myappdb;Uid=root;Pwd=;", serverVersion);

            _dbContext = new AppDbContext(optionsBuilder.Options);

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
            // Kreiraj DbContextOptions sa konekcijskim stringom
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseMySql("server=localhost;database=myappdb;user=root;password=;", new MySqlServerVersion(new Version(10, 4, 32)));

            // Kreiraj instancu AppDbContext koristeći opcije
            using (var context = new AppDbContext(optionsBuilder.Options))
            {
                // Dohvati podatke iz tabele audit logs
                var auditLogs = context.GetAuditLogs();

                // Postavi izvor podataka za DataGrid
                auditLogsGrid.ItemsSource = auditLogs;
            }

            // Otvori popup
            popupExample.IsOpen = true;
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
