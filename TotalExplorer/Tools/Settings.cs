using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Fex.TotalExplorer.Tools
{
    public class Settings
    {
        public const string applicationName = "TotalExplorer";
        public const string settingsFileName = "Settings.xml";
        static string settingsPath;

        public struct PageSetting
        {
            public string Name { get; set; }
            public string CurrentLocation { get; set; }
            public bool IsLocked { get; set; }
        }

        public Point Location { get; set; }
        public Size Size { get; set; }
        public FormWindowState WindowState { get; set; }

        public int TabLeftIndex { get; set; }
        public int TabRightIndex { get; set; }

        public static Settings Instance = new Settings();

        public List<PageSetting> TabLeftPages = new List<PageSetting>();
        public List<PageSetting> TabRightPages = new List<PageSetting>();

        static Settings()
        {
            // Find the application data path
            // e.g. C:\Users\%user%\AppData\Roaming\[App]
            var applicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            // Create the full path
            var applicationStoragePath = Path.Combine(applicationDataPath, applicationName);

            if (!Directory.Exists(applicationStoragePath))
                Directory.CreateDirectory(applicationStoragePath);

            settingsPath = Path.Combine(applicationStoragePath, settingsFileName);
        }

        public static void Save()
        {

            XmlSerializationHelper.Serialize<Settings>(Settings.Instance, settingsPath);
        }


        public static void Load()
        {
            if (!File.Exists(settingsPath))
                return;

            Settings.Instance = XmlSerializationHelper.DeserializeFile<Settings>(settingsPath);
        }
    }
}
