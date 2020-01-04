using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using YetAnotherLosslessCutter.MVVM;

namespace YetAnotherLosslessCutter
{
    sealed class VideoSegment : Segment
    {
        readonly MainWindow host;
        public VideoSegment(MainWindow window) => host = window;

        public DelegateCommand DeleteThisSegment => new DelegateCommand(() =>
        {
            //You really shouldn't do this
            ((MainWindowVM)host.DataContext).DeleteSegment(this);
        });
        public async Task<CuttingTaskResult> Cut()
        {
            var ffmpeg = new FfmpegUtil();

            ffmpeg.Progress += (e) =>
            {
                Application.Current.Dispatcher.Invoke(() => host.TaskbarInfo.ProgressValue = e.Progress,
                    System.Windows.Threading.DispatcherPriority.Background);
                Progress = e.Progress;
            };

            Status = ProgressStatus.Running;
            CutTo = CutTo < CutFrom ? MaxDuration : CutTo;
         
            try
            {
                await ffmpeg.Cut(this);
                Status = ProgressStatus.Finished;
                Progress = 0d;
                return new CuttingTaskResult(true, null);
            }
            catch (Exception e)
            {
                Progress = 0d;
                Status = ProgressStatus.Idle;
                return new CuttingTaskResult(false, e);
            }
           
           
        }

        string _SourceFile;
        public string SourceFile
        {
            get => _SourceFile;
            set => Set( ref _SourceFile, value);
        }

        public string OutputFile
        {
            get
            {
                //TODO: Guess file ending can be customizable as well, someday...
                var fileEnding = $"-{CutFrom:hh\\.mm\\.ss\\.fff}-{CutTo:hh\\.mm\\.ss\\.fff}{Path.GetExtension(SourceFile)}";
                return Settings.Instance.SaveToSourceFolder
                     ? Path.ChangeExtension(SourceFile, fileEnding)
                     : Path.Combine(Settings.Instance.OutputDirectory, $"{Path.GetFileNameWithoutExtension(SourceFile)}{fileEnding}");
            }

        }
        public ImageSource Thumbnail { get; private set; }

        TimeSpan _CurrentPosition = TimeSpan.Zero;
        public TimeSpan CurrentPosition
        {
            get => _CurrentPosition;
            set
            {
                if (value > MaxDuration) return;
                if (!Set( ref _CurrentPosition, value)) return;
                host.TimelineSlider.Value = _CurrentPosition.TotalMilliseconds;
                host.MediaElement1.Position = _CurrentPosition;
                OnPropertyChanged(nameof(CurrentPositionDouble));
            }
        }

        TimeSpan _CutFrom = TimeSpan.Zero;
        public TimeSpan CutFrom
        {
            get => _CutFrom;
            set
            {
                if (value > MaxDuration) return;
                if (!Set( ref _CutFrom, value)) return;
                CurrentPosition = _CutFrom;
                OnPropertyChanged(nameof(LeftMarker));
                OnPropertyChanged(nameof(CutDuration));
                OnPropertyChanged(nameof(DurationWidth));
                UpdateThumbnail();
            }
        }

        async void UpdateThumbnail()
        {
            Thumbnail = await FfmpegUtil.GetThumbnail(SourceFile, CutFrom);
            OnPropertyChanged(nameof(Thumbnail));
        }
        TimeSpan _CutTo = TimeSpan.Zero;
        public TimeSpan CutTo
        {
            get => _CutTo;
            set
            {
                if (value > MaxDuration || value < CutFrom) return;
                if (!Set( ref _CutTo, value)) return;
                CurrentPosition = _CutTo;
                OnPropertyChanged(nameof(RightMarker));
                OnPropertyChanged(nameof(CutDuration));
                OnPropertyChanged(nameof(DurationWidth));
            }
        }

        TimeSpan _MaxDuration;
        public TimeSpan MaxDuration
        {
            get => _MaxDuration;
            set
            {
                if (!Set( ref _MaxDuration, value)) return;
                OnPropertyChanged(nameof(DurationWidth));
                _CutTo = _MaxDuration;
                OnPropertyChanged(nameof(CutTo));
                OnPropertyChanged(nameof(RightMarker));
            }
        }

        public TimeSpan CutDuration => CutTo - CutFrom;

        public double DurationWidth => MaxDuration.TotalMilliseconds;
        public double CurrentPositionDouble
        {
            get => _CurrentPosition.TotalMilliseconds;
            set => CurrentPosition = TimeSpan.FromMilliseconds(value);
        }
        public Point LeftMarker => new Point(_CutFrom.TotalMilliseconds, 1);

        public Point RightMarker => new Point(_CutTo.TotalMilliseconds, 2);
    }
}
