using GalaSoft.MvvmLight;
using System;

namespace YetAnotherLosslessCutter
{
    sealed class ProjectSettings : ViewModelBase
    {

        string _SourceFile;
        public string SourceFile
        {
            get => _SourceFile;
            set => Set(() => SourceFile, ref _SourceFile, value);
        }

        string _OutputFile;
        public string OutputFile
        {
            get => _OutputFile;
            set => Set(() => OutputFile, ref _OutputFile, value);
        }

        bool _RemoveAudio;
        public bool RemoveAudio
        {
            get => _RemoveAudio;
            set => Set(() => RemoveAudio, ref _RemoveAudio, value);
        }

        bool _IncludeAllStreams = true;
        public bool IncludeAllStreams
        {
            get => _IncludeAllStreams;
            set => Set(() => IncludeAllStreams, ref _IncludeAllStreams, value);
        }


        TimeSpan _CutFrom;
        public TimeSpan CutFrom
        {
            get => _CutFrom;
            set => Set(() => CutFrom, ref _CutFrom, value);
        }

        TimeSpan _CutTo;
        public TimeSpan CutTo
        {
            get => _CutTo;
            set => Set(() => CutTo, ref _CutTo, value);
        }

        public TimeSpan CutDuration =>  CutTo - CutFrom;
    }
}
