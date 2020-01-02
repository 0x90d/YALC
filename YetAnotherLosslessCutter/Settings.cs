using GalaSoft.MvvmLight;
using System.Text.Json.Serialization;

namespace YetAnotherLosslessCutter
{
   public sealed class Settings : ViewModelBase
    {
        static Settings instance;
        [JsonIgnore]
        public static Settings Instance => instance ??= new Settings();



        private bool _RemoveAudio;
        [JsonIgnore]
        public bool RemoveAudio
        {
            get => _RemoveAudio;
            set => Set(() => RemoveAudio, ref _RemoveAudio, value); 
        }

        private bool _MergeSegments;
        [JsonIgnore]
        public bool MergeSegments
        {
            get => _MergeSegments;
            set => Set(() => MergeSegments, ref _MergeSegments, value);
        }

        private bool _IncludeAllStreams = true;
        [JsonIgnore]
        public bool IncludeAllStreams
        {
            get => _IncludeAllStreams;
            set => Set(() => IncludeAllStreams, ref _IncludeAllStreams, value);
        }


        private bool _SaveToSourceFolder = true;
        [JsonPropertyName("SaveToSourceFolder")]
        public bool SaveToSourceFolder
        {
            get => _SaveToSourceFolder;
            set => Set(() => SaveToSourceFolder, ref _SaveToSourceFolder, value);
        }

        private string _OutputDirectory = string.Empty;
        [JsonPropertyName("OutputDirectory")]
        public string OutputDirectory
        {
            get => _OutputDirectory;
            set => Set(() => OutputDirectory, ref _OutputDirectory, value);
        }
    }
}
