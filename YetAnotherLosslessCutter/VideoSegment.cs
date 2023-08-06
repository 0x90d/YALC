using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using YetAnotherLosslessCutter.MVVM;

namespace YetAnotherLosslessCutter
{
    sealed class VideoSegment : Segment
    {
        internal bool initialized;
        readonly MainWindow host;
        public VideoSegment() => host = Application.Current.MainWindow as MainWindow;
        public VideoSegment(MainWindow window)
        {
            initialized = true;
            host = window;
        }
        [JsonIgnore]
        public DelegateCommand DeleteThisSegment => new DelegateCommand(() =>
        {
            //You really shouldn't do this
            if (Status == ProgressStatus.Idle)
                ((MainWindowVM)host.DataContext).DeleteSegment(this);
            else
                ((MainWindowVM)host.DataContext).DeleteSegmentFromQueue(this);

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
                await ffmpeg.Cut(this, Settings.Instance.LowCuttingProcessPriority ?
                                        System.Diagnostics.ProcessPriorityClass.BelowNormal :
                                        System.Diagnostics.ProcessPriorityClass.Normal);
                Status = ProgressStatus.Finished;
                Progress = 0d;
                return new CuttingTaskResult(true, null, ffmpeg);
            }
            catch (Exception e)
            {
                Progress = 0d;
                Status = ProgressStatus.Failed;
                return new CuttingTaskResult(false, e, ffmpeg);
            }


        }
        [JsonIgnore]
        public DelegateCommand OpenInExplorer => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            Process.Start(new ProcessStartInfo("explorer.exe", $" /select, \"{SourceFile}\"")
            {
                UseShellExecute = true,
            });
        });

        public bool MarkedForDeletion;

        string _SourceFile;
        [JsonPropertyName("SourceFile")]
        public string SourceFile
        {
            get => _SourceFile;
            set => Set(ref _SourceFile, value);
        }

        [JsonIgnore]
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
        [JsonPropertyName("Thumbnail"), JsonConverter(typeof(JsonImageSourceConverter))]
        public ImageSource Thumbnail { get; set; }

        TimeSpan _MaxDuration;
        [JsonPropertyName("SourceDuration")]
        public TimeSpan MaxDuration
        {
            get => _MaxDuration;
            set
            {
                if (!Set(ref _MaxDuration, value)) return;
                OnPropertyChanged(nameof(DurationWidth));
                _CutTo = _MaxDuration;
                OnPropertyChanged(nameof(CutTo));
                OnPropertyChanged(nameof(RightMarker));
            }
        }

        TimeSpan _CurrentPosition = TimeSpan.Zero;
        [JsonIgnore]
        public TimeSpan CurrentPosition
        {
            get => _CurrentPosition;
            set
            {
                if (initialized && value > MaxDuration) value = MaxDuration;
                if (!Set(ref _CurrentPosition, value)) return;
                host.TimelineSlider.Value = _CurrentPosition.TotalMilliseconds;
                ((MainWindowVM)host.DataContext).HostPlayer.CurTime = _CurrentPosition.Ticks;
                OnPropertyChanged(nameof(CurrentPositionDouble));
            }
        }

        TimeSpan _CutFrom = TimeSpan.Zero;
        [JsonPropertyName("CutFrom")]
        public TimeSpan CutFrom
        {
            get => _CutFrom;
            set
            {
                if (initialized && value > MaxDuration) value = MaxDuration;
                if (initialized && value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (!Set(ref _CutFrom, value)) return;
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
        [JsonPropertyName("CutTo")]
        public TimeSpan CutTo
        {
            get => _CutTo;
            set
            {
                if (initialized && value < CutFrom) return;
                if (initialized && value > MaxDuration) value = MaxDuration;
                if (initialized && value < TimeSpan.Zero) value = TimeSpan.Zero;
                if (!Set(ref _CutTo, value)) return;
                CurrentPosition = _CutTo;
                OnPropertyChanged(nameof(RightMarker));
                OnPropertyChanged(nameof(CutDuration));
                OnPropertyChanged(nameof(DurationWidth));
            }
        }

        [JsonIgnore]
        public TimeSpan CutDuration => CutTo - CutFrom;

        public double DurationWidth => MaxDuration.TotalMilliseconds;
        [JsonIgnore]
        public double CurrentPositionDouble
        {
            get => _CurrentPosition.TotalMilliseconds;
            set => CurrentPosition = TimeSpan.FromMilliseconds(value);
        }
        [JsonIgnore]
        public Point LeftMarker => new Point(_CutFrom.TotalMilliseconds, 1);

        [JsonIgnore]
        public Point RightMarker => new Point(_CutTo.TotalMilliseconds, 2);
    }
}
