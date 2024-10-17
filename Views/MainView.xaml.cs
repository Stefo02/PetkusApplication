using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class MainView : Window
    {
        private readonly User _currentUser;
        private readonly AppDbContext _context;
        private DispatcherTimer sessionTimer;

        public MainView(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Dodaj event handlere za praćenje aktivnosti
            this.PreviewKeyDown += MainView_PreviewKeyDown;
            this.PreviewMouseMove += MainView_PreviewMouseMove;

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
            optionsBuilder.UseMySql("Server=192.168.8.118;Port=3307;Database=myappdb;Uid=username;Pwd=;", serverVersion);
            _context = new AppDbContext(optionsBuilder.Options);

            this.DataContext = this;
            FormiranjePonudeView = new FormiranjePonudeView();
            MagacinView = new MagacinView();

            StartSessionTimer();

            this.Closing += MainView_Closing; 
        }

        public FormiranjePonudeView FormiranjePonudeView { get; set; }
        public MagacinView MagacinView { get; set; }

        private void MainView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ResetSessionTimer(); // Resetuje tajmer kada korisnik koristi tastaturu
        }

        private void MainView_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ResetSessionTimer(); // Resetuje tajmer kada korisnik pomera miš
        }

        private void ResetSessionTimer()
        {
            // Zaustavi trenutni tajmer
            sessionTimer.Stop();

            // Ponovo startuj tajmer
            sessionTimer.Start();
        }

        private void StartSessionTimer()
        {
            // Postavi tajmer za proveru sesije
            sessionTimer = new DispatcherTimer();
            sessionTimer.Interval = TimeSpan.FromMinutes(30); // Primer: 30 minuta trajanje sesije
            sessionTimer.Tick += (s, e) => EndInactiveSessions(); // Poziv metode za isteknuće sesije
            sessionTimer.Start();
        }

        private void EndInactiveSessions()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
            optionsBuilder.UseMySql("Server=192.168.8.118;Port=3307;Database=myappdb;Uid=username;Pwd=;", serverVersion);

            using (var context = new AppDbContext(optionsBuilder.Options))
            {
                var session = context.UserSessions.FirstOrDefault(s => s.UserId == _currentUser.Id && s.IsActive);
                if (session != null && DateTime.Now - session.LoginTime > TimeSpan.FromMinutes(30))
                {
                    session.LogoutTime = DateTime.Now;
                    session.IsActive = false;
                    context.UserSessions.Update(session);
                    context.SaveChanges();

                    MessageBox.Show("Vaša sesija je istekla.");
                    LogOutCurrentUser();
                    this.Close(); 
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogOutCurrentUser();
        }

        private void MainView_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            LogOutCurrentUser();
        }

        private bool _isLoggingOut = false;

        private void LogOutCurrentUser()
        {
            _currentUser.LastLogout = DateTime.Now;
            _currentUser.IsLoggedIn = false;

            // Ažuriranje sesije korisnika
            var session = _context.UserSessions.FirstOrDefault(s => s.UserId == _currentUser.Id && s.IsActive);
            if (session != null)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                _context.UserSessions.Update(session);
            }

            // Ažuriranje korisnika
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

        private void FormiranjePonudeView_Loaded_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
