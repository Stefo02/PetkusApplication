using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace PetkusApplication.Settings
{
    internal class SettingsService
    {
        private const string FilePath = "settings.json";

        public AppSettings LoadSettings()
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                return JsonSerializer.Deserialize<AppSettings>(json);
            }
            return new AppSettings(); // Vraća prazne postavke ako fajl ne postoji
        }

        public void SaveSettings(AppSettings settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("settings.json", json);  // Sačuvaj u JSON fajl
        }

    }
}
