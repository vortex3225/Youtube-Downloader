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
using Youtube_Downloader.Scripts;

namespace Youtube_Downloader
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private AppSettings set = new AppSettings();
        public Settings()
        {
            InitializeComponent();
            this.Closed += Settings_Closed;
            set = SettingsHandler.InitaliseSettings();
            display_warnings_checkbox.IsChecked = set.DisplayWarnings;
            save_output_path_checkbox.IsChecked = set.SaveOutputPath;
        }

        private void Settings_Closed(object? sender, EventArgs e)
        {
            Console.WriteLine("closing settings");
            SettingsHandler.SaveSettings(set);
        }

        private void display_warnings_checkbox_Click(object sender, RoutedEventArgs e)
        {
            bool check_val = (bool)display_warnings_checkbox.IsChecked;
            set.DisplayWarnings = check_val;
        }

        private void save_output_path_checkbox_Click(object sender, RoutedEventArgs e)
        {
            bool check_val = (bool)save_output_path_checkbox.IsChecked;
            set.SaveOutputPath = check_val;
        }

        
    }
}
