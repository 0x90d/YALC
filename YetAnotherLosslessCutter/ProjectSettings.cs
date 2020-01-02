using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Windows;
using System.Windows.Media;

namespace YetAnotherLosslessCutter
{
    sealed class ProjectSettings : ViewModelBase
    {
        readonly MainWindow host;
        public ProjectSettings(MainWindow window) => host = window;

        public RelayCommand DeleteThisSegment => new RelayCommand(() =>
        {
            //You really shouldn't do this
            ((MainWindowVM) host.DataContext).DeleteSegment(this);
        });

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

        ImageSource _Thumbnail;
        public ImageSource Thumbnail => _Thumbnail;

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
                RaisePropertyChanged(nameof(LeftMarker));
                RaisePropertyChanged(nameof(CutDuration));
                RaisePropertyChanged(nameof(DurationWidth));
                UpdateThumbnail();
            }
        }

        async void UpdateThumbnail()
        {
            _Thumbnail = await FfmpegUtil.GetThumbnail(SourceFile, CutFrom);
            RaisePropertyChanged(nameof(Thumbnail));
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
                RaisePropertyChanged(nameof(RightMarker));
                RaisePropertyChanged(nameof(CutDuration));
                RaisePropertyChanged(nameof(DurationWidth));
            }
        }

        TimeSpan _MaxDuration;
       public TimeSpan MaxDuration
        {
            get => _MaxDuration;
            set
            {
                if (!Set(() => MaxDuration, ref _MaxDuration, value)) return;
                RaisePropertyChanged(nameof(DurationWidth));
                _CutTo = _MaxDuration;
                RaisePropertyChanged(nameof(CutTo));
                RaisePropertyChanged(nameof(RightMarker));
            }
        }

        public TimeSpan CutDuration =>  CutTo - CutFrom;

        public double DurationWidth
        {
            get => MaxDuration.TotalMilliseconds;
        }
        public double CurrentPositionDouble
        {
            get => _CurrentPosition.TotalMilliseconds;
            set => CurrentPosition = TimeSpan.FromMilliseconds(value);
        }
        public Point LeftMarker
        {
            get => new Point( _CutFrom.TotalMilliseconds, 1);
        }
        public Point RightMarker
        {
            get => new Point(_CutTo.TotalMilliseconds,2);
        }
    }
}
