using GalaSoft.MvvmLight;
using System;

namespace YetAnotherLosslessCutter
{
    sealed class ProjectSettings : ViewModelBase
    {
        readonly MainWindow host;
        public ProjectSettings(MainWindow window) => host = window;

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

        TimeSpan _CurrentPosition = TimeSpan.Zero;
        public TimeSpan CurrentPosition
        {
            get => _CurrentPosition;
            set
            {
                if (value > MaxDuration) return;
                if (!Set(() => CurrentPosition, ref _CurrentPosition, value)) return;
                host.TimelineSlider.Value = _CurrentPosition.TotalMilliseconds;
                host.MediaElement1.Position = _CurrentPosition;
                RaisePropertyChanged(nameof(CurrentPositionDouble));
            }
        }

        TimeSpan _CutFrom = TimeSpan.Zero;
        public TimeSpan CutFrom
        {
            get => _CutFrom;
            set
            {
                if (value > MaxDuration) return;
                if (!Set(() => CutFrom, ref _CutFrom, value)) return;
                CurrentPosition = _CutFrom;
                RaisePropertyChanged(nameof(LeftPositionDouble));
                RaisePropertyChanged(nameof(CutDuration));
                RaisePropertyChanged(nameof(MaxPositionDouble));
            }
        }
        TimeSpan _CutTo = TimeSpan.Zero;
        public TimeSpan CutTo
        {
            get => _CutTo;
            set
            {
                if (value > MaxDuration || value < CutFrom) return;
                if (!Set(() => CutTo, ref _CutTo, value)) return;
                CurrentPosition = _CutTo;
                RaisePropertyChanged(nameof(RightPositionDouble));
                RaisePropertyChanged(nameof(CutDuration));
                RaisePropertyChanged(nameof(MaxPositionDouble));
            }
        }

        TimeSpan _MaxDuration;
       public TimeSpan MaxDuration
        {
            get => _MaxDuration;
            set
            {
                if (!Set(() => MaxDuration, ref _MaxDuration, value)) return;
                RaisePropertyChanged(nameof(MaxPositionDouble));
                _CutTo = _MaxDuration;
                RaisePropertyChanged(nameof(CutTo));
                RaisePropertyChanged(nameof(RightPositionDouble));
            }
        }

        public TimeSpan CutDuration =>  CutTo - CutFrom;

        public double MaxPositionDouble
        {
            get => MaxDuration.TotalMilliseconds;
        }
        public double CurrentPositionDouble
        {
            get => _CurrentPosition.TotalMilliseconds;
            set => CurrentPosition = TimeSpan.FromMilliseconds(value);
        }
        public double LeftPositionDouble
        {
            get => _CutFrom.TotalMilliseconds;
        }
        public double RightPositionDouble
        {
            get => _CutTo.TotalMilliseconds;
        }
    }
}
