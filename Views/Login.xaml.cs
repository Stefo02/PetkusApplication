using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private readonly AppDbContext _context;

        public Login()
        {
            InitializeComponent();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32)); // Adjust version as per your MySQL server version
            optionsBuilder.UseMySql("Server=localhost;Database=myappdb;Uid=root;Pwd=;", serverVersion);

            _context = new AppDbContext(optionsBuilder.Options);
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            // Query the database for the user
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                if (user.IsAdmin)
                {
                    // Authentication successful for admin user, navigate to AdminWindow
                    AdminWindow adminWindow = new AdminWindow();
                    adminWindow.Show();
                }
                else
                {
                    // Regular user login (not admin), navigate to MainView or other appropriate window
                    MainView mainView = new MainView();
                    mainView.Show();
                }

                Close(); // Close the Login window
            }
            else
            {
                MessageBox.Show("Invalid username or password. Please try again.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginBtn_Click(this, new RoutedEventArgs());
            }
        }
    }
}
