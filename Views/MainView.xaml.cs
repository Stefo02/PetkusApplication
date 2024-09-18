using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class MainView : Window
    {
        private readonly User _currentUser;
        private readonly AppDbContext _context;

        public MainView(User user)
        {
            InitializeComponent();
            _currentUser = user;

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
            optionsBuilder.UseMySql("Server=localhost;Database=myappdb;Uid=root;Pwd=;", serverVersion);
            _context = new AppDbContext(optionsBuilder.Options);

            this.DataContext = this;
            FormiranjePonudeView = new FormiranjePonudeView();
            MagacinView = new MagacinView();

            this.Closing += MainView_Closing; // Correct event handler assignment
        }

        public FormiranjePonudeView FormiranjePonudeView { get; set; }
        public MagacinView MagacinView { get; set; }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Kreirajte instancu Login prozora
            var loginWindow = new Login();

            // Prikazivanje Login prozora
            loginWindow.Show();

            // Zatvaranje trenutnog prozora
            this.Close();
        }

        private void MainView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogOutCurrentUser();
        }

        private void LogOutCurrentUser()
        {
            _currentUser.LastLogout = DateTime.Now;
            _currentUser.IsLoggedIn = false;
            _context.Users.Update(_currentUser);
            _context.SaveChanges();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl tabControl &&
                tabControl.SelectedItem is TabItem selectedTab &&
                selectedTab.Header.ToString() == "Odjavite se")
            {
                LogOutCurrentUser();

                // Kreirajte instancu Login prozora
                var loginWindow = new Login();

                // Prikazivanje Login prozora
                loginWindow.Show();

                // Zatvaranje trenutnog prozora
                this.Close();
            }
        }

        private void FormiranjePonudeView_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
