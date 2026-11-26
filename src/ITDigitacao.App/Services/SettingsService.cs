using System;
using System.IO;
using System.Web.Script.Serialization;

namespace ITDigitacao.App.Services
{
    internal sealed class AppSettings
    {
        public int FontSize { get; set; }
        public bool Sounds { get; set; }
        public bool ShowMetrics { get; set; }
        public bool FocusMode { get; set; }
        public AppSettings() { FontSize = 18; Sounds = false; ShowMetrics = true; FocusMode = false; }
    }
    internal static class SettingsService
    {
        private static string FilePath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ITDigitacao", "settings.json"); } }
        public static AppSettings Load()
        {
            try { if (File.Exists(FilePath)) return new JavaScriptSerializer().Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings(); }
            catch { }
            return new AppSettings();
        }
        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, new JavaScriptSerializer().Serialize(settings));
        }
    }
}
