using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Youtube_Downloader.Scripts
{
    public static class YoutubeDownloader
    {
        private static int process_id = -1;
        public static void Open(string mode, string link, string output_dir_path, string special, string auxMode = "download")
        {
            if (!Path.Exists(output_dir_path))
            {
                MessageBox.Show($"Invalid output path: {output_dir_path}", "Invalid output path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                Process yt_downloader = new Process();
                yt_downloader.EnableRaisingEvents = true;
                yt_downloader.Exited += Yt_downloader_Exited;
                yt_downloader.StartInfo = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "yt-downloader.exe"))
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    
                };
                yt_downloader.Start();
                /*
                 * 
                    mode = input("Mode: ")
                    link = input("Link: ")
                    api_key = input("Api: ")
                    output_path = input("Output: ")
                    aux = input("Aux: ")
                    special_format = input("Special: ")
                 * 
                 */
                process_id = yt_downloader.Id;
                Console.WriteLine("Process ID: " + process_id);
                yt_downloader.StandardInput.WriteLine(mode);
                yt_downloader.StandardInput.WriteLine(link);
                yt_downloader.StandardInput.WriteLine(" ");
                yt_downloader.StandardInput.WriteLine(output_dir_path);
                yt_downloader.StandardInput.WriteLine(auxMode);
                yt_downloader.StandardInput.WriteLine(special);

                yt_downloader.StandardInput.Flush();
                yt_downloader.StandardInput.Close();

                yt_downloader.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public static void Cancel(bool is_fetch = false)
        {
            try
            {
                Process? fetched_process = Process.GetProcessById(process_id);
                if (fetched_process != null)
                {
                    fetched_process.Kill(true);
                    if (is_fetch)
                    {
                        MessageBox.Show("Cancelled loading playlist items!");
                    }
                    else
                    {
                        MessageBox.Show("Cancelled download!");
                    }
                }
            }
            catch (Exception ex) { }
        }

        private static void Yt_downloader_Exited(object? sender, EventArgs e)
        {
            Console.WriteLine("YT-DOWNLOADER.EXE EXITED!");
        }
    }
}
