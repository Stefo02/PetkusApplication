using System;
using System.Net.Http;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data; // Update with the correct namespace
using PetkusApplication.Models; // Update with the correct namespace if needed


namespace PetkusApplication
{
    public partial class App : Application
    {
        public static string ConnectionString { get; private set; }
        private static User _currentUser; // Assuming you have a way to access the current user
        public static User CurrentUser
        {
            get { return _currentUser; }
            set { _currentUser = value; }
        }
        private static DbContextOptions<AppDbContext> _dbContextOptions;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Define the connection string once here
            ConnectionString = "Server=10.10.10.130;Database=Petkus;Uid=Petkus;Pwd=Petkus1995;Port=3307;";

            // Initialize DbContextOptions if using Entity Framework DbContext
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 28));
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql(ConnectionString, serverVersion)
                .Options;
        }

        public static DbContextOptions<AppDbContext> GetDbContextOptions()
        {
            return _dbContextOptions;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Perform any final cleanup or save state as necessary
            LogoutCurrentUser();
            base.OnExit(e);
        }

        protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
        {
            // Perform any cleanup required for session management
            LogoutCurrentUser();
            base.OnSessionEnding(e);
        }

        private void LogoutCurrentUser()
        {
            if (_currentUser != null)
            {
                _currentUser.LastLogout = DateTime.Now;
                _currentUser.IsLoggedIn = false;

                using (var context = new AppDbContext(_dbContextOptions))
                {
                    // Ažuriranje sesije korisnika
                    var session = context.UserSessions.FirstOrDefault(s => s.UserId == _currentUser.Id && s.IsActive);
                    if (session != null)
                    {
                        session.LogoutTime = DateTime.Now;
                        session.IsActive = false;
                        context.UserSessions.Update(session);
                    }

                    // Ažuriranje korisnika
                    context.Users.Update(_currentUser);
                    context.SaveChanges();
                }

                _currentUser = null; // Očisti referencu na korisnika nakon odjave
            }
        }

    }
}
