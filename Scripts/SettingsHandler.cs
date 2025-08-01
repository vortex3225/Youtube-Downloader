using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;

namespace Youtube_Downloader.Scripts
{
    public static class SettingsHandler
    {

        public static AppSettings InitaliseSettings()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "app_config.json");
            try
            {
                return (File.Exists(path)) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) : new AppSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new AppSettings();
            }
        }

        public static void SaveSettings(AppSettings settings)
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "app_config.json");
                string returned_json = JsonSerializer.Serialize(settings);
                File.WriteAllText(path, returned_json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}


