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
        private static User _currentUser; // Assuming you have a way to access the current user
        public static User CurrentUser
        {
            get { return _currentUser; }
            set { _currentUser = value; }
        }
        private static DbContextOptions<AppDbContext> _dbContextOptions;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Step 1: Initialize DbContextOptions
            var serverVersion = new MySqlServerVersion(new Version(10, 4, 32));
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseMySql("Server=192.168.8.118;Port=3307;Database=myappdb;Uid=username;Pwd=;", serverVersion)
                .Options;

            // You can now use _dbContextOptions to initialize your DbContext wherever needed in your application



            // Continue with other startup logic if needed
            // Example: Initialize session, load main window, etc.
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
