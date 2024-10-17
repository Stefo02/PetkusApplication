using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;

namespace PetkusApplication.Views
{
    public partial class Login : Window
    {
        private readonly AppDbContext _context;

        public Login()
        {
            InitializeComponent();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
            optionsBuilder.UseMySql("Server=192.168.8.118;Port=3307;Database=myappdb;Uid=username;Pwd=;", serverVersion);

            _context = new AppDbContext(optionsBuilder.Options);
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.Password == password);

            if (user != null)
            {
                // Provera da li ima više od 200 sesija
                var sessionCount = _context.UserSessions.Count();

                if (sessionCount > 200)
                {
                    // Pronalaženje najstarijih 20 sesija i brisanje
                    var sessionsToDelete = _context.UserSessions
                        .OrderBy(s => s.LoginTime)
                        .Take(20)  // Uvek brišemo prvih 20 najstarijih sesija
                        .ToList();

                    // Brisanje najstarijih sesija
                    _context.UserSessions.RemoveRange(sessionsToDelete);
                }

                // Ažuriraj poslednju prijavu i stanje korisnika
                user.LastLogin = DateTime.Now;
                user.IsLoggedIn = true;
                _context.SaveChanges();

                // Prikaži loading animaciju
                LoadingWindow loadingWindow = new LoadingWindow();
                loadingWindow.Show();

                // Pokretanje tajmera za 3 sekunde
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    loadingWindow.Close();

                    // Dodajemo sesiju samo za obične korisnike
                    if (!user.IsAdmin)
                    {
                        var session = new UserSession
                        {
                            UserId = user.Id,
                            LoginTime = DateTime.Now,
                            IsActive = true,
                            Username = user.Username
                        };
                        _context.UserSessions.Add(session);
                    }

                    _context.SaveChanges();

                    // Set _currentUser to the logged-in user
                    App.CurrentUser = user;

                    // Otvori sledeći prozor nakon što se loading završi
                    Window targetWindow = user.IsAdmin ? new AdminWindow() : new MainView(user);
                    targetWindow.Show();
                    this.Close();  // Zatvori login prozor
                };
                timer.Start();
            }
            else
            {
                MessageBox.Show("Nevažeće korisničko ime ili lozinka. Pokušajte ponovo.", "Prijava nije uspela", MessageBoxButton.OK, MessageBoxImage.Error);
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
