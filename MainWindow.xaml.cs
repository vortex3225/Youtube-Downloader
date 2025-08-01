using Microsoft.Win32;
using System.IO;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Youtube_Downloader.Scripts;

namespace Youtube_Downloader
{

    public static class Utils
    {
        public const string STRING_SPLITTER = @"??18891??[]]]}\\!!";
        public struct ExtractedVideo
        {
            public string Title { get; set; }
            public string Url { get; set; }
        }

        public static string NormalizeYouTubeURL(string rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
                return string.Empty;

            rawUrl = rawUrl.Trim();

            if (!rawUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = "https://" + rawUrl;
            }
            if (!rawUrl.StartsWith("https://www.", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = rawUrl.Replace("https://", "https://www.");
            }

            return rawUrl;
        }

        public static ExtractedVideo GetVideoInfo(string rawString)
        {
            string[] split = rawString.Split(STRING_SPLITTER);
            return new ExtractedVideo { Title = split[0], Url = split[1] };
        }

    }

    public partial class MainWindow : Window
    {


        private string selected_mode = "playlist"; // selected download menu option
        private string playlist_dwnl_mode = "auto";
        private string mode = "playlist"; // valid modes: playlist, single_video, batch_video
        private string link = string.Empty;
        private string output_dir_path = AppContext.BaseDirectory;
        private string special = "hav";
        private bool is_downloading = false;
        private bool is_fetching = false;
        private string previous_url = string.Empty; //stores previous url
        private string previous_count = "0"; // stores previous count of fetched data
        private string previously_fetched = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt")); // stores previous text of fetched_data
        private List<string> selected_links = new List<string>(); // selected links in batch mode or manual playlist mode
        private int batch_count_vids = 0;
        private Canvas ?previously_open = null;
        private bool is_dark_mode = false;
        private AppSettings fetched_settings = new AppSettings();

        public MainWindow()
        {
            InitializeComponent();
            playlist_dwnl_mode_all.IsChecked = true;
            playlist_mode_radio.IsChecked = true;
            audio_video_radio.IsChecked = true;
            download_single_video_btn.IsEnabled = false;
            download_batch_video_list_btn.IsEnabled = false;
            fetched_settings = SettingsHandler.InitaliseSettings();
            download_path_box.Text = fetched_settings.OutputPath;
            this.Closed += MainWindow_Closed;

        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            if (fetched_settings.SaveOutputPath)
            {
                fetched_settings.OutputPath = download_path_box.Text;
            }
            Console.WriteLine("closing");
            Console.WriteLine(fetched_settings.DisplayWarnings);
            Console.WriteLine(fetched_settings.SaveOutputPath);
            SettingsHandler.SaveSettings(fetched_settings);
        }

        private void EnablePlaylistDownloadButton(bool isAuto)
        {
            if (playlist_url_box.Text == string.Empty || playlist_url_box.Text == null)
            {
                download_selected_playlist_items_btn.IsEnabled = false;
                download_all_playlist_items_btn.IsEnabled = false;
                playlist_download_mode_frame.IsEnabled = false;
            }
            else
            {
                playlist_download_mode_frame.IsEnabled = true;
                if (isAuto)
                {
                    download_selected_playlist_items_btn.IsEnabled = false;
                    download_all_playlist_items_btn.IsEnabled = true;
                }
                else
                {
                    if (playlist_items_count_display.Text.Contains("Loading"))
                    {
                        download_selected_playlist_items_btn.IsEnabled = true;
                    }

                    download_all_playlist_items_btn.IsEnabled = false;
                }
            }
        }

        private void ClearPreviousData()
        {
            link = string.Empty;
            mode = string.Empty;
            is_downloading = false;
            is_fetching = false;
            batch_count_vids = 0;
        }

        private string CompileLinks()
        {
            string link_string = string.Empty;
            if (selected_links.Count == 0) { return link_string; }
            for (int i = 0; i < selected_links.Count; i++)
            {
                string extracted_link = selected_links[i];
                if (i < selected_links.Count - 1)
                {

                    link_string += extracted_link + "|";
                }
                else
                {
                    link_string += extracted_link;
                }
            }
            return link_string;
        }

        private async void SetPlaylistItemCount()
        {
            if (playlist_url_box.Text != previous_url)
            {
                if (!isLinkValid(link))
                {
                    MessageBox.Show("Please insert a valid youtube playlist link!");
                    return;
                }
                previous_url = playlist_url_box.Text;
                playlist_items_count_display.Text = "Loading playlist items, please wait...";
                set_download_properties_btn.IsEnabled = false;
                is_fetching = true;
                playlist_url_box.IsEnabled = false;
                await Task.Run(() => YoutubeDownloader.Open("playlist", link, AppContext.BaseDirectory, "", "fetch"));
                try
                {
                    string[] fetched_text = File.ReadAllLines(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt"));
                    previously_fetched = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt"));
                    Console.WriteLine(fetched_text.Length);
                    int last_index = fetched_text.Length - 1;
                    string count = fetched_text[last_index];
                    if (count != null)
                    {
                        count = count.Replace("PIC:", "");
                        playlist_items_count_display.Text = $"Videos in playlist: {count}";
                        if (count != "0" && System.IO.Path.Exists(download_path_box.Text))
                        {
                            set_download_properties_btn.IsEnabled = true;
                        }
                        else
                        {
                            set_download_properties_btn.IsEnabled = false;
                        }
                        previous_count = count;
                    }
                }
                catch (Exception ex)
                {
                    playlist_items_count_display.Text = $"Videos in playlist: 0";
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                playlist_items_count_display.Text = $"Videos in playlist: {previous_count}";
            }
            playlist_url_box.IsEnabled = true;
        }

        private bool isLinkValid(string to_check_link)
        {
            if (link.Contains("youtube"))
            {
                if (mode == "playlist")
                {
                    if (link.Contains("playlist"))
                    {
                        return true;
                    }
                }
                else if (mode == "batch_video" || mode == "single_video")
                {
                    if (link.Contains("?v="))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private async void PopulatePlaylistItemList()
        {
            if (playlist_url_box.Text != previous_url)
            {
                try
                {
                    if (!isLinkValid(link))
                    {
                        MessageBox.Show("Please insert a valid youtube playlist link!");
                        return;
                    }
                    playlist_contents.Items.Clear(); // clears previous playlist items for new playlist
                    previous_url = playlist_url_box.Text;
                    loading_items_display.Foreground = System.Windows.Media.Brushes.Orange;
                    loading_items_display.Text = $"Loading playlist contents...";
                    await Task.Run(() => YoutubeDownloader.Open("playlist", link, AppContext.BaseDirectory, "", "fetch"));
                    string[] fetched_text = File.ReadAllLines(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt"));
                    previously_fetched = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt"));
                    if (fetched_text.Length == 0)
                    {
                        download_selected_playlist_items_btn.IsEnabled = false;
                        select_buttons_display.IsEnabled = false;
                        loading_items_display.Foreground = System.Windows.Media.Brushes.Orange;
                        playlist_contents.IsEnabled = false;
                        playlist_url_box.IsEnabled = true;
                        return;
                    } // mo items written to file
                    int count = 0;
                    foreach (string fetchedLine in fetched_text)
                    {
                        if (fetchedLine.Contains("PIC:"))
                        {
                            break;
                        }
                        Utils.ExtractedVideo extracted_video = Utils.GetVideoInfo(fetchedLine);
                        CheckBox box = new CheckBox();
                        box.Name = $"Box{count}";
                        box.Content = $"{extracted_video.Title} - {extracted_video.Url}";
                        box.IsChecked = false;
                        box.Tag = count;
                        box.Click += OnPlaylistItemCheckboxClick;
                        playlist_contents.Items.Add(box);
                        count++;
                    }
                    loading_items_display.Text = $"Playlist items: {count}";
                    previous_count = count.ToString();
                    loading_items_display.Foreground = System.Windows.Media.Brushes.Black;
                    select_buttons_display.IsEnabled = true;
                    playlist_contents.IsEnabled = true;
                    playlist_url_box.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                loading_items_display.Text = $"Playlist items: {previous_count}";
            }

        }

        private void DisableDisplays()
        {
            playlist_mode_display.Visibility = Visibility.Hidden;
            batch_video_mode_display.Visibility = Visibility.Hidden;
            video_mode_display.Visibility = Visibility.Hidden;
            video_output_display.Visibility = Visibility.Hidden;
            ClearPreviousData();
        }

        private void OnPlaylistItemCheckboxClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = e.Source as CheckBox;
            if (checkbox != null)
            {
                string link = checkbox.Content.ToString().Split(" - ")[1];
                if (checkbox.IsChecked == true)
                {
                    if (!selected_links.Contains(link))
                    {
                        selected_links.Add(link);
                    }
                }
                else if (checkbox.IsChecked == false)
                {
                    selected_links.Remove(link);
                }
            }
            if (selected_links.Count == 0)
            {
                download_selected_playlist_items_btn.IsEnabled = false;
            }
            else
            {
                download_selected_playlist_items_btn.IsEnabled = true;
            }
        }

        private void download_all_playlist_items_btn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(link) && System.IO.Path.Exists(download_path_box.Text))
            {
                video_output_display.Visibility = Visibility.Visible;
                SetPlaylistItemCount();
                playlist_mode_display.Visibility = Visibility.Hidden;
            }
        }

        private void download_selected_playlist_items_btn_Click(object sender, RoutedEventArgs e)
        {
            link = CompileLinks();
            if (!string.IsNullOrEmpty(link) && System.IO.Path.Exists(download_path_box.Text))
            {
                video_output_display.Visibility = Visibility.Visible;
                set_download_properties_btn.IsEnabled = true;
            }
            playlist_mode_display.Visibility = Visibility.Hidden;
        }

        private void playlist_dwnl_mode_all_Checked(object sender, RoutedEventArgs e)
        {
            if (is_fetching)
            {
                is_fetching = false;
                YoutubeDownloader.Cancel(true);
            }
            manual_playlist_dwnl_mode.Visibility = Visibility.Collapsed;
            auto_playlist_dwnl_mode.Visibility = Visibility.Visible;
            mode = "playlist";
            playlist_dwnl_mode = "auto";
            EnablePlaylistDownloadButton(true);
            
        }

        private async void playlist_dwn_mode_select_Checked(object sender, RoutedEventArgs e)
        {
            manual_playlist_dwnl_mode.Visibility = Visibility.Visible;
            auto_playlist_dwnl_mode.Visibility = Visibility.Collapsed;
            mode = "batch_video";
            playlist_dwnl_mode = "manual";
            EnablePlaylistDownloadButton(false);
            is_fetching = true;
            playlist_url_box.IsEnabled = false;
            if (playlist_url_box.Text != previous_url)
            {
                if (is_fetching)
                {
                    PopulatePlaylistItemList();
                }
            }
            playlist_url_box.IsEnabled = true;
        }

        private void playlist_mode_radio_Checked(object sender, RoutedEventArgs e)
        {
            DisableDisplays();
            playlist_mode_display.Visibility = Visibility.Visible;
        }

        private void single_vid_mode_radio_Checked(object sender, RoutedEventArgs e)
        {
            DisableDisplays();
            video_mode_display.Visibility = Visibility.Visible;
            mode = "single_video";
        }

        private void batch_vid_mode_radio_Checked(object sender, RoutedEventArgs e)
        {
            DisableDisplays();
            batch_video_mode_display.Visibility = Visibility.Visible;
            mode = "batch_video";
        }

        private void playlist_url_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            string url = Utils.NormalizeYouTubeURL(playlist_url_box.Text);

            bool isAuto = (playlist_dwnl_mode == "auto") ? true : false;
            EnablePlaylistDownloadButton(isAuto);
            link = playlist_url_box.Text;
            if (previous_url != playlist_url_box.Text && !string.IsNullOrEmpty(previous_url) && isLinkValid(link))
            {
                refresh_playlist_btn.IsEnabled = true;
                refresh_playlist_btn.Visibility = Visibility.Visible;
            }
            
            if (!isLinkValid(link))
            {
                download_all_playlist_items_btn.IsEnabled = false;
                download_selected_playlist_items_btn.IsEnabled = false;
                playlist_download_mode_frame.IsEnabled = false;
            }
            else
            {
                download_all_playlist_items_btn.IsEnabled = true;
                download_selected_playlist_items_btn.IsEnabled = true;
                playlist_download_mode_frame.IsEnabled = true;
            }
        }

        private void audio_video_radio_Checked(object sender, RoutedEventArgs e)
        {
            special = "hav"; // sets special to 'hav' for python to download highest quality audio+video stream
        }

        private void audio_only_radio_Checked(object sender, RoutedEventArgs e)
        {
            special = "a"; // sets special to 'a' for python to only download audio
        }

        private async void set_download_properties_btn_Click(object sender, RoutedEventArgs e)
        {
            output_dir_path = download_path_box.Text;

            // starts the download 
            if (fetched_settings.DisplayWarnings == true)
            {
                if (MessageBoxResult.Yes != MessageBox.Show($"Are you sure you wish to start download to the following path: {output_dir_path}?", "Processing download...", MessageBoxButton.YesNo, MessageBoxImage.Question)) {
                    return;
                }
            }
            set_download_properties_btn.IsEnabled = false;
            is_downloading = true;
            is_fetching = false;
            download_status_label.Visibility = Visibility.Visible;
            download_status_label.Text = "Download status: downloading...";
            await Task.Run(() => YoutubeDownloader.Open(mode, link, output_dir_path, special));
            download_status_label.Text = "Download status: finished!";
            download_status_label.Foreground = System.Windows.Media.Brushes.Green;
            set_download_properties_btn.IsEnabled = true;
            await Task.Run(() => Thread.Sleep(5000)); // waits for 5 seconds without freezing ui thread to hide download status
            download_status_label.Visibility = Visibility.Hidden;
            download_status_label.Foreground = System.Windows.Media.Brushes.Black;
        }

        private void folder_open_button_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                download_path_box.Text = dialog.FolderName;
            }
            else
            {
                download_path_box.Text = AppContext.BaseDirectory;
            }
        }

        private void return_btn_Click(object sender, RoutedEventArgs e)
        {
            video_output_display.Visibility = Visibility.Hidden;
            if (previously_open != null)
            {
                previously_open.Visibility = Visibility.Visible;
            }
            else
            {
                playlist_mode_display.Visibility = Visibility.Visible;
            }
            if (is_downloading)
            {
                YoutubeDownloader.Cancel();
                is_downloading = false;
            }
            else if (is_fetching)
            {
                YoutubeDownloader.Cancel(is_fetching);
                is_fetching = false;
                is_downloading = false;
            }
        }

        private void download_path_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!System.IO.Path.Exists(download_path_box.Text))
            {
                set_download_properties_btn.IsEnabled = false;
            }
            else if (mode == "playlist" && System.IO.Path.Exists(download_path_box.Text))
            {
                string playlist_item_count = playlist_items_count_display.Text.Replace("Videos in playlist: ", "");
                if (playlist_item_count != "0" && !playlist_items_count_display.Text.Contains("Loading"))
                {
                    set_download_properties_btn.IsEnabled = true;
                }
            }
            else if (mode != "playlist" && !string.IsNullOrEmpty(link))
            {
                set_download_properties_btn.IsEnabled = true;
            }
        }

        private void select_all_playlist_vid_btn_Click(object sender, RoutedEventArgs e)
        {
            if (playlist_contents.Items.Count > 0)
            {
                if (fetched_settings.DisplayWarnings)
                {
                    if (MessageBoxResult.Yes != MessageBox.Show($"Are you sure you wish to select: {playlist_contents.Items.Count} videos?", $"Selecting: {playlist_contents.Items.Count} videos...", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    {
                        return;
                    }
                }

                foreach (CheckBox item in playlist_contents.Items)
                {
                    item.IsChecked = true;
                    string link = item.Content.ToString().Split(" - ")[1];
                    if (!selected_links.Contains(link))
                    {
                        selected_links.Add(link);
                    }
                }
                download_selected_playlist_items_btn.IsEnabled = true;
            }
        }

        private void desselect_all_playlist_vid_btn_Click(object sender, RoutedEventArgs e)
        {
            if (playlist_contents.Items.Count > 0)
            {
                if (fetched_settings.DisplayWarnings)
                {
                    if (MessageBoxResult.Yes != MessageBox.Show($"Are you sure you wish to deselect: {playlist_contents.Items.Count} videos?", $"Deselecting: {playlist_contents.Items.Count} videos...", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    {
                        return;
                    }
                }

                foreach (CheckBox item in playlist_contents.Items)
                {
                    if (item.IsChecked == true)
                    {
                        item.IsChecked = false;
                    }
                }
                download_selected_playlist_items_btn.IsEnabled = false;
                selected_links.Clear();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
           try
            {
                File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt"), "");
            }
            catch (Exception ex) { }
        }

        private void refresh_playlist_btn_Click(object sender, RoutedEventArgs e)
        {
            if (fetched_settings.DisplayWarnings)
            {
                if (MessageBoxResult.Yes != MessageBox.Show("Are you sure you wish to refresh playlist items? This will load a new playlist.", "Refresh playlist", MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    return;
                }
            }
            if (playlist_dwnl_mode == "manual")
            {
                PopulatePlaylistItemList();
            }
            else
            {
                SetPlaylistItemCount();
            }
            previous_url = playlist_url_box.Text;
            refresh_playlist_btn.IsEnabled = false;
        }

        private void download_single_video_btn_Click(object sender, RoutedEventArgs e)
        {
            video_output_display.Visibility = Visibility.Visible;
            video_mode_display.Visibility= Visibility.Hidden;
            link = video_url_box.Text;
        }

        private async void add_vid_btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ListViewItem new_video = new ListViewItem();
                string url = _batch_video_url_box.Text;
                string fetched_video = previously_fetched;

                if (previous_url != url)
                {
                    loading_batch_video_display.Visibility = Visibility.Visible;
                    await Task.Run(() => YoutubeDownloader.Open("single_video", url, output_dir_path, string.Empty, auxMode: "fetch"));
                    fetched_video = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "fetched_data.txt"));
                    loading_batch_video_display.Visibility = Visibility.Hidden;
                    previous_url = url;
                    previously_fetched = fetched_video;
                }

                if (fetched_video != null)
                {
                    batch_video_list.IsEnabled = true;
                    remove_selected_batch_video_btn.IsEnabled = true;

                    string[] split = fetched_video.Split("??18891??[]]]}\\!!");
                    foreach (string s in split)
                    {
                        Console.WriteLine(s);
                    }
                    Utils.ExtractedVideo extracted = new Utils.ExtractedVideo { Title = split[0], Url = split[1] };
                    new_video.Content = $"{extracted.Title} - {extracted.Url}";
                    batch_video_list.Items.Add(new_video);
                    if (!selected_links.Contains(extracted.Url))
                    {
                        selected_links.Add(extracted.Url);
                        download_batch_video_list_btn.IsEnabled = true;
                    }
                    batch_count_vids++;
                    batch_video_item_count_display.Text = $"Videos: {batch_count_vids}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void remove_selected_batch_video_btn_Click(object sender, RoutedEventArgs e)
        {
            ListViewItem ?selected_item = batch_video_list.SelectedItem as ListViewItem;

            if (selected_item != null)
            {
                string[] split = selected_item.Content.ToString().Split(" - ");
                string title = split[0];
                string url = split[1];

                if (fetched_settings.DisplayWarnings)
                {
                    if (MessageBoxResult.OK != MessageBox.Show($"Are you sure you wish to remove: {title} from the download list?", $"Removing {title}...", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation))
                    {
                        return;
                    }
                }

                if (selected_links.Contains(url))
                {
                    selected_links.Remove(url);
                }
                batch_video_list.Items.Remove(selected_item);
                batch_count_vids--;
                batch_video_item_count_display.Text = $"Videos: {batch_count_vids}";

                if (batch_count_vids <= 0)
                {
                    download_batch_video_list_btn.IsEnabled = false;
                    batch_video_list.IsEnabled = false;
                    remove_selected_batch_video_btn.IsEnabled = false;
                }
            }

        }

        private void download_batch_video_list_btn_Click(object sender, RoutedEventArgs e)
        {
            video_output_display.Visibility = Visibility.Visible;
            batch_video_mode_display.Visibility = Visibility.Hidden;
            link = CompileLinks();
        }

        private void _batch_video_url_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            link = _batch_video_url_box.Text;
            if (isLinkValid(_batch_video_url_box.Text))
            {
                add_vid_btn.IsEnabled = true;
            }
            else
            {
                download_batch_video_list_btn.IsEnabled = false;
                add_vid_btn.IsEnabled = false;
            }
        }

        private void video_url_box_TextChanged(object sender, TextChangedEventArgs e)
        {
            link = video_url_box.Text;
            if (isLinkValid(video_url_box.Text))
            {
                download_single_video_btn.IsEnabled = true;
            }
            else
            {
                download_single_video_btn.IsEnabled = false;
            }
        }

        private void video_output_display_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (mode == "single_video" || mode == "batch_video")
            {
                playlist_items_count_display.Visibility = Visibility.Hidden;
                if (isLinkValid(link))
                {
                    set_download_properties_btn.IsEnabled = true;
                }
            }
            else if (mode == "playlist")
            {
                playlist_items_count_display.Visibility = Visibility.Visible;
            }
        }

        private void info_menu_btn_Click(object sender, RoutedEventArgs e)
        {
            long unix_compilestamp = 1754069400;
            DateTime date = DateTimeOffset.FromUnixTimeSeconds(unix_compilestamp).DateTime;
            TimeSpan diff = DateTimeOffset.UtcNow - date;
            int daysPassed = (int)diff.TotalDays;
            MessageBox.Show($"""
                Youtube Downloader

                Version 1.0.0
                Compiled: {date.ToString("dd.MM.yyyy")} ({daysPassed} days ago)


                Easily download playlists or videos from Youtube.
                ! You cannot download private content, playlists/videos must be public or unlisted to be able to be downloaded. !

                Github page: https://github.com/vortex3225/Youtube-Downloader
                """);
        }

        private void settings_menu_btn_Click(object sender, RoutedEventArgs e)
        {
            Settings settings_form = new Settings();
            this.IsEnabled = false;
            settings_form.Closed += Settings_form_Closed;
            settings_form.Show();
        }

        private void Settings_form_Closed(object? sender, EventArgs e)
        {
            this.IsEnabled = true;
            fetched_settings = SettingsHandler.InitaliseSettings();
        }
    }
}