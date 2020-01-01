using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MahApps.Metro.Controls.Dialogs;
using YetAnotherLosslessCutter.FFProbe;

namespace YetAnotherLosslessCutter
{
    sealed class MainWindowVM : ViewModelBase
    {
        readonly FfmpegUtil ffmpeg = new FfmpegUtil();
        readonly FfprobeUtil ffprobe = new FfprobeUtil();
        readonly MainWindow host;
        Track timeLineTrack;
        ProgressDialogController progressDialogController;
        public string Title => $"YALC - {YALCConstants.ASSEMBLY_INFORMATIONAL_VERSION}";
        MediaInfo SourceInfo;
        public MainWindowVM(MainWindow mainWindow)
        {
            host = mainWindow;
            ffmpeg.Progress += Ffmpeg_Progress;
            host.InputBindings.Add(new KeyBinding(Jump1SecondForward, new KeyGesture(Key.Right, ModifierKeys.None)));
            host.InputBindings.Add(new KeyBinding(Jump1FrameForward, new KeyGesture(Key.Right, ModifierKeys.Control)));
            host.InputBindings.Add(new KeyBinding(Jump10SecondForward, new KeyGesture(Key.Right, ModifierKeys.Shift)));
            host.InputBindings.Add(new KeyBinding(Jump1SecondBackward, new KeyGesture(Key.Left, ModifierKeys.None)));
            host.InputBindings.Add(new KeyBinding(Jump1FrameBackward, new KeyGesture(Key.Left, ModifierKeys.Control)));
            host.InputBindings.Add(new KeyBinding(Jump10SecondBackward, new KeyGesture(Key.Left, ModifierKeys.Shift)));
        }

        private void Ffmpeg_Progress(ProgressEventArgs obj)
        {
            Application.Current.Dispatcher.Invoke(() => host.TaskbarInfo.ProgressValue = obj.Progress,
                System.Windows.Threading.DispatcherPriority.Background);

            progressDialogController?.SetProgress(obj.Progress);
            progressDialogController?.SetMessage($"{Math.Round(obj.Progress * 100)}%");
        }



        TimeSpan _CurrentPosition = TimeSpan.Zero;
        public TimeSpan CurrentPosition
        {
            get => _CurrentPosition;
            set
            {
                if (value > MaxPosition) return;
                if (!Set(() => CurrentPosition, ref _CurrentPosition, value)) return;
                host.TimelineSlider.Value = _CurrentPosition.TotalMilliseconds;
                host.MediaElement1.Position = _CurrentPosition;
                RaisePropertyChanged(nameof(CurrentPositionDouble));
            }
        }
        TimeSpan _LeftPosition = TimeSpan.Zero;
        public TimeSpan LeftPosition
        {
            get => _LeftPosition;
            set
            {
                if (value > MaxPosition) return;
                if (!Set(() => LeftPosition, ref _LeftPosition, value)) return;
                CurrentPosition = _LeftPosition;
                RaisePropertyChanged(nameof(LeftPositionDouble));
            }
        }
        TimeSpan _RightPosition = TimeSpan.Zero;
        public TimeSpan RightPosition
        {
            get => _RightPosition;
            set
            {
                if (value > MaxPosition || value < LeftPosition) return;
                if (!Set(() => RightPosition, ref _RightPosition, value)) return;
                CurrentPosition = _RightPosition;
                RaisePropertyChanged(nameof(RightPositionDouble));
            }
        }

        TimeSpan _MaxPosition;
        TimeSpan MaxPosition
        {
            get => _MaxPosition;
            set
            {
                if (!Set(() => MaxPosition, ref _MaxPosition, value)) return;
                RaisePropertyChanged(nameof(MaxPositionDouble));
                _RightPosition = MaxPosition;
                RaisePropertyChanged(nameof(RightPosition));
                RaisePropertyChanged(nameof(RightPositionDouble));
            }
        }

        public double MaxPositionDouble
        {
            get => MaxPosition.TotalMilliseconds;
        }
        public double CurrentPositionDouble
        {
            get => _CurrentPosition.TotalMilliseconds;
            set => CurrentPosition = TimeSpan.FromMilliseconds(value);
        }
        public double LeftPositionDouble
        {
            get => _LeftPosition.TotalMilliseconds;
        }
        public double RightPositionDouble
        {
            get => _RightPosition.TotalMilliseconds;
        }

        string _SourceFileName;
        public string SourceFileName
        {
            get => _SourceFileName;
            private set => Set(() => SourceFileName, ref _SourceFileName, value);
        }
        string _SourceFile;
        public string SourceFile
        {
            get => _SourceFile;
            set
            {
                if (!Set(() => SourceFile, ref _SourceFile, value)) return;
                if (string.IsNullOrEmpty(value))
                {
                    CloseProject();
                    return;
                }

                SourceFileName = Path.GetFileName(_SourceFile);
                ffmpeg.NewProject(_SourceFile);
                SourceInfo = ffprobe.GetInfos(_SourceFile);
                host.TimelineSlider.Maximum = SourceInfo.Duration.TotalMilliseconds;
                host.TimelineSlider.Value = 0;
                MaxPosition = SourceInfo.Duration;
                RaisePropertyChanged(nameof(Title));
                host.MediaElement1.Source = new Uri(_SourceFile);
                host.MediaElement1.Play();
                host.MediaElement1.Pause();
            }
        }
        void CloseProject()
        {
            SourceFileName = string.Empty;
            RightPosition = TimeSpan.Zero;
            LeftPosition = TimeSpan.Zero;
            CurrentPosition = TimeSpan.Zero;
            MaxPosition = TimeSpan.Zero;
            host.CutMarker.X1 = 0d;
            host.CutMarker.X2 = 0d;
        }
        public RelayCommand SetLeftPosition => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            LeftPosition = host.MediaElement1.Position;
            MarkerTimeline(0);
        });
        public RelayCommand SetRightPosition => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            RightPosition = host.MediaElement1.Position;
            MarkerTimeline(1);

        });
        public RelayCommand Jump1FrameForward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            CurrentPosition += TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate);
        });
        public RelayCommand Jump1SecondForward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            CurrentPosition += TimeSpan.FromSeconds(1);
        });
        public RelayCommand Jump10SecondForward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            CurrentPosition += TimeSpan.FromSeconds(10);
        });
        public RelayCommand Jump1FrameBackward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            CurrentPosition -= TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate);
        });
        public RelayCommand Jump1SecondBackward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            CurrentPosition -= TimeSpan.FromSeconds(1);
        });
        public RelayCommand Jump10SecondBackward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            CurrentPosition -= TimeSpan.FromSeconds(10);
        });
        public RelayCommand PlayVideo => new RelayCommand(() => host.MediaElement1.Play());
        public RelayCommand PauseVideo => new RelayCommand(() => host.MediaElement1.Pause());
        public RelayCommand ReloadVideo => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            var currentPos = host.MediaElement1.Position;
            host.MediaElement1.Close();
            host.MediaElement1.Source = null;
            host.MediaElement1.Source = new Uri(SourceFile);
            host.MediaElement1.Play();
            host.MediaElement1.Pause();
            host.MediaElement1.Position = currentPos;
        });


        public RelayCommand CutVideo => new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            ffmpeg.settings.CutFrom = LeftPosition;
            ffmpeg.settings.CutTo = RightPosition < LeftPosition ? SourceInfo.Duration : RightPosition;
            ffmpeg.settings.OutputFile = Path.ChangeExtension(SourceFile, $"-{LeftPosition:hh\\.mm\\.ss\\.fff}-{RightPosition:hh\\.mm\\.ss\\.fff}{Path.GetExtension(SourceFile)}");
            var mySettings = new MetroDialogSettings()
            {
                AnimateShow = false,
                AnimateHide = false,
            };
            host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            progressDialogController = await host.ShowProgressAsync("Please wait...", "0%", settings: mySettings);
            try
            {
                await ffmpeg.Cut();
                await progressDialogController.CloseAsync();
            }
            catch (Exception ex)
            {
                await progressDialogController.CloseAsync();
                host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                await host.ShowMessageAsync("Error", ex.ToString(), settings: mySettings);
            }
            host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        });
        public RelayCommand DeleteSource => new RelayCommand(async () =>
       {
           var mySettings = new MetroDialogSettings()
           {
               AnimateShow = false,
               AnimateHide = false,
           };
           host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
           var result = await host.ShowMessageAsync("Confirmation", $"Delete {SourceFile}?", MessageDialogStyle.AffirmativeAndNegative, settings: mySettings);
           if (result != MessageDialogResult.Affirmative) return;
           host.MediaElement1.Stop();
           host.MediaElement1.Close();
           host.MediaElement1.Source = null;
           try
           {
               var success = Do(() =>
               {
                   File.Delete(SourceFile);
                   return true;
               }, TimeSpan.FromSeconds(0.5));
               if (success)
                   SourceFile = string.Empty;
               else
                   throw new TimeoutException($"Failed to delete {SourceFile}");
           }
           catch (Exception ex)
           {
               host.MediaElement1.Source = new Uri(SourceFile);
               await host.ShowMessageAsync("Failed to delete file", ex.ToString(), MessageDialogStyle.Affirmative, settings: mySettings);
           }
       });
        static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Thread.Sleep(retryInterval);
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
            throw new AggregateException(exceptions);
        }
        public void LoadSourceFile(string file)
        {
            if (!File.Exists(file)) return;
            SourceFile = file;
        }

        void MarkerTimeline(int pos)
        {
            if (string.IsNullOrEmpty(SourceFile) == false && host.MediaElement1.NaturalDuration.HasTimeSpan)
            {
                if (timeLineTrack == null)
                    timeLineTrack = host.TimelineSlider.Template.FindName("PART_Track", host.TimelineSlider) as Track;
                Point relativePoint = timeLineTrack.Thumb.TransformToAncestor(host.TimelineGrid).Transform(new Point(0, 0));
                if (pos == 0 & (host.CutMarker.X2 == 0d || relativePoint.X > host.CutMarker.X2))
                    host.CutMarker.X2 = host.TimelineSlider.ActualWidth;
                if (pos == 0 && relativePoint.X <= host.CutMarker.X2)
                {
                    host.CutMarker.X1 = relativePoint.X;
                }
                else if (pos == 1 && relativePoint.X >= host.CutMarker.X1)
                    host.CutMarker.X2 = relativePoint.X;

            }
        }
    }
}
