﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.EntityFrameworkCore;
using PetkusApplication.Data;
using PetkusApplication.Models;
using Squirrel;

namespace PetkusApplication.Views
{
    public partial class MainView : Window
    {
        private readonly User _currentUser;
        private readonly AppDbContext _context;
        private DispatcherTimer sessionTimer;
        UpdateManager manager;

        public MainView(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;

            // Dodaj event handlere za praćenje aktivnosti
            this.PreviewKeyDown += MainView_PreviewKeyDown;
            this.PreviewMouseMove += MainView_PreviewMouseMove;

            _context = new AppDbContext(App.GetDbContextOptions());

            this.DataContext = this;
            FormiranjePonudeView = new FormiranjePonudeView();
            MagacinView = new MagacinView();

            StartSessionTimer();

            Loaded += Mainwindow_Loaded;

            this.Closing += MainView_Closing; 
        }

        public FormiranjePonudeView FormiranjePonudeView { get; set; }
        public MagacinView MagacinView { get; set; }

        private async void Mainwindow_Loaded(object sender, RoutedEventArgs e)
        {
            #if !DEBUG
                try
                    {
                string token = "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";
                string url = $"https://{token}:x-oauth-basic@github.com/Stefo02/PetkusApplication";

                manager = await UpdateManager
                    .GitHubUpdateManager(url);

                CurrentVersionTextBox.Text = manager.CurrentlyInstalledVersion().ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error while checking for updates: " + ex.Message);
        }
            #else
            // In debug mode, skip update check
            CurrentVersionTextBox.Text = "Debug mode: Update check skipped.";
            #endif
        }

        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            #if !DEBUG
                if (manager == null) return; // Ensure manager is initialized

                var updateInfo = await manager.CheckForUpdate();

                UpdateButton.IsEnabled = updateInfo.ReleasesToApply.Count > 0;
            #else
            // In debug mode, disable the button or handle accordingly
            UpdateButton.IsEnabled = false;
            #endif
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            #if !DEBUG
                if (manager != null)
        {
                    await manager.UpdateApp();
                    MessageBox.Show("Ažuriranje uspešno");
        }
            #else
            // In debug mode, inform the user that updates are not available
            MessageBox.Show("Update functionality is disabled in debug mode.");
            #endif
        }

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
            // Use the globally defined DbContextOptions from App.xaml.cs
            using (var context = new AppDbContext(App.GetDbContextOptions()))
            {
                var session = context.UserSessions.FirstOrDefault(s => s.UserId == _currentUser.Id && s.IsActive);
                if (session != null && DateTime.Now - session.LoginTime > TimeSpan.FromMinutes(30))
                {
                    session.LogoutTime = DateTime.Now;
                    session.IsActive = false;
                    context.UserSessions.Update(session);
                    context.SaveChanges();

                    MessageBox.Show("Your session has expired.");
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
