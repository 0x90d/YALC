using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        Track timeLineTrack => host.TimelineSlider.Template.FindName("PART_Track", host.TimelineSlider) as Track;
        ProgressDialogController progressDialogController;
        public string Title => $"YALC - {YALCConstants.ASSEMBLY_INFORMATIONAL_VERSION}";
        MediaInfo SourceInfo;
        readonly MetroDialogSettings dialogSettings = new MetroDialogSettings
        {
            AnimateShow = false,
            AnimateHide = false,
        };
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

        public ObservableCollection<ProjectSettings> ProjectSegmentList { get; } =
            new ObservableCollection<ProjectSettings>();
        ProjectSettings _SelectedSegment;

        public ProjectSettings SelectedSegment
        {
            get => _SelectedSegment;
            set
            {
                if (!Set(() => SelectedSegment, ref _SelectedSegment, value)) return;
                if (value == null)
                {
                    if (ProjectSegmentList.Count == 0)
                    {
                        //Clear everything
                        host.MediaElement1.Stop();
                        host.MediaElement1.Close();
                        host.MediaElement1.Source = null;
                        SourceFile = string.Empty;
                        return;
                    }
                    //Select previous segment
                    SelectedSegment = ProjectSegmentList[^1];
                    return;
                }
                //Do nothing if this is the first segment
                if (ProjectSegmentList.Count == 1) return;
                //Otherwise, update cut draw area
                host.CutMarker.X1 = 0d;
                host.CutMarker.X2 = 0d;
                host.TimelineSlider.Value = value.CutFrom.TotalMilliseconds;
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait();
                MarkerTimeline(0);
                host.TimelineSlider.Value = value.CutTo.TotalMilliseconds;
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait();
                MarkerTimeline(1);
            }
        }


        public bool MergeSegments { get; set; }


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
                SourceInfo = ffprobe.GetInfos(_SourceFile);
                var project = new ProjectSettings(host) { SourceFile = _SourceFile };
                host.TimelineSlider.Maximum = SourceInfo.Duration.TotalMilliseconds;
                host.TimelineSlider.Value = 0;
                project.MaxDuration = SourceInfo.Duration;
                ProjectSegmentList.Add(project);
                SelectedSegment = project;
                RaisePropertyChanged(nameof(Title));
                host.MediaElement1.Source = new Uri(_SourceFile);
                host.MediaElement1.Play();
                host.MediaElement1.Pause();
            }
        }
        void CloseProject()
        {
            SourceFileName = string.Empty;
            host.CutMarker.X1 = 0d;
            host.CutMarker.X2 = 0d;
            ProjectSegmentList.Clear();
        }
        public RelayCommand SetLeftPosition => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CutFrom = host.MediaElement1.Position;
            MarkerTimeline(0);
        });
        public RelayCommand SetRightPosition => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CutTo = host.MediaElement1.Position;
            MarkerTimeline(1);

        });
        public RelayCommand AddNewSegment => new RelayCommand(() =>
        {
            var project = new ProjectSettings(host)
            {
                SourceFile = SourceFile,
                MaxDuration = SourceInfo.Duration,
                CutTo = SourceInfo.Duration,
                CutFrom = SelectedSegment?.CutTo ?? TimeSpan.Zero,
            };
            ProjectSegmentList.Add(project);
            SelectedSegment = project;
            host.TimelineSlider.Value = project.CutFrom.TotalMilliseconds;
        });
        public RelayCommand DeleteSelectedSegment => new RelayCommand(() =>
        {
            if (SelectedSegment == null) return;
            ProjectSegmentList.Remove(SelectedSegment);
        });

        public RelayCommand RemoveAllSegments => new RelayCommand(() =>
        {
            SelectedSegment = null;
            ProjectSegmentList.Clear();
        });
        public RelayCommand Jump1FrameForward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CurrentPosition += TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate);
        });
        public RelayCommand Jump1SecondForward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CurrentPosition += TimeSpan.FromSeconds(1);
        });
        public RelayCommand Jump10SecondForward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CurrentPosition += TimeSpan.FromSeconds(10);
        });
        public RelayCommand Jump1FrameBackward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CurrentPosition -= TimeSpan.FromMilliseconds(SourceInfo.Streams[0].FrameRate);
        });
        public RelayCommand Jump1SecondBackward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CurrentPosition -= TimeSpan.FromSeconds(1);
        });
        public RelayCommand Jump10SecondBackward => new RelayCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CurrentPosition -= TimeSpan.FromSeconds(10);
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

            host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            progressDialogController = await host.ShowProgressAsync("Please wait...", "0%", settings: dialogSettings);
            try
            {
                var fileList = new List<string>();
                for (int i = 0; i < ProjectSegmentList.Count; i++)
                {
                    var projectSettingse = ProjectSegmentList[i];
                    projectSettingse.CutTo = projectSettingse.CutTo < projectSettingse.CutFrom
                        ? SourceInfo.Duration
                        : projectSettingse.CutTo;
                    ffmpeg.settings = projectSettingse;
                    ffmpeg.settings.OutputFile = Path.ChangeExtension(SourceFile,
                        $"-{projectSettingse.CutFrom:hh\\.mm\\.ss\\.fff}-{projectSettingse.CutTo:hh\\.mm\\.ss\\.fff}{Path.GetExtension(SourceFile)}");
                    fileList.Add(ffmpeg.settings.OutputFile);
                    progressDialogController.SetTitle($"Please wait... ({i + 1}/{ProjectSegmentList.Count})");
                    await ffmpeg.Cut();
                }

                if (MergeSegments)
                {
                    progressDialogController.SetTitle("Please wait... merging files");
                    progressDialogController.SetIndeterminate();
                    await ffmpeg.Merge(Path.ChangeExtension(SourceFile, $"_merged{Path.GetExtension(SourceFile)}"), fileList);
                    foreach (var file in fileList)
                        try
                        {
                            File.Delete(file);
                        }
                        catch { }
                }
                await progressDialogController.CloseAsync();
            }
            catch (Exception ex)
            {
                await progressDialogController.CloseAsync();
                host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                await host.ShowMessageAsync("Error", ex.ToString(), settings: dialogSettings);
            }
            host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
        });
        public RelayCommand DeleteSource => new RelayCommand(async () =>
       {
           var result = await host.ShowMessageAsync("Confirmation", $"Delete {SourceFile}?", MessageDialogStyle.AffirmativeAndNegative, settings: dialogSettings);
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
               await host.ShowMessageAsync("Failed to delete file", ex.ToString(), MessageDialogStyle.Affirmative, settings: dialogSettings);
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
