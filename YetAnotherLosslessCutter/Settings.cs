using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using YetAnotherLosslessCutter.MVVM;

namespace YetAnotherLosslessCutter
{
    public sealed class Settings : ViewModelBase
    {
        static Settings instance;
        [JsonIgnore]
        public static Settings Instance => instance ??= new Settings();

        private static string currentFolder;
        public static string CurrentFolder =>
            currentFolder ??= Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        private static string settingsLocation;
        private static string SettingsLocation => settingsLocation ??=
            Path.Combine(CurrentFolder, "Settings.json");
        public static void SaveSettings()
        {
            File.WriteAllText(SettingsLocation, System.Text.Json.JsonSerializer.Serialize(instance));
        }
        public static void LoadSettings()
        {
            if (File.Exists(SettingsLocation))
                instance = System.Text.Json.JsonSerializer.Deserialize<Settings>(File.ReadAllText(SettingsLocation));
        }

        private bool _RemoveAudio;
        [JsonIgnore]
        public bool RemoveAudio
        {
            get => _RemoveAudio;
            set => Set(ref _RemoveAudio, value);
        }

        private bool _MergeSegments;
        [JsonIgnore]
        public bool MergeSegments
        {
            get => _MergeSegments;
            set => Set(ref _MergeSegments, value);
        }

        private bool _IncludeAllStreams = true;
        [JsonIgnore]
        public bool IncludeAllStreams
        {
            get => _IncludeAllStreams;
            set => Set(ref _IncludeAllStreams, value);
        }
        private bool _DeleteSourceFileAfterDone;
        [JsonIgnore]
        public bool DeleteSourceFileAfterDone
        {
            get => _DeleteSourceFileAfterDone;
            set => Set(ref _DeleteSourceFileAfterDone, value);
        }


        private bool _SaveToSourceFolder = true;
        [JsonPropertyName("SaveToSourceFolder")]
        public bool SaveToSourceFolder
        {
            get => _SaveToSourceFolder;
            set => Set(ref _SaveToSourceFolder, value);
        }
        private bool _RemoveFinishedSegments;
        [JsonPropertyName("RemoveFinishedSegments")]
        public bool RemoveFinishedSegments
        {
            get => _RemoveFinishedSegments;
            set => Set(ref _RemoveFinishedSegments, value);
        }
        private bool _AutoStartQueue = true;
        [JsonPropertyName("AutoStartQueue")]
        public bool AutoStartQueue
        {
            get => _AutoStartQueue;
            set => Set(ref _AutoStartQueue, value);
        }
        private string _OutputDirectory = string.Empty;
        [JsonPropertyName("OutputDirectory")]
        public string OutputDirectory
        {
            get => _OutputDirectory;
            set => Set(ref _OutputDirectory, value);
        }
    }
}
