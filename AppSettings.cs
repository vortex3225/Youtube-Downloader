using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Youtube_Downloader
{
    public class AppSettings
    {
        // TODO add more settings.
        public bool DisplayWarnings { get; set; } = true;
        public bool SaveOutputPath { get; set; } = false;
        public string OutputPath { get; set; } = AppContext.BaseDirectory;
    }
}
