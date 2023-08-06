using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MahApps.Metro.Controls.Dialogs;
using YetAnotherLosslessCutter.MVVM;

namespace YetAnotherLosslessCutter
{
    sealed class MainWindowVM : ViewModelBase
    {
        //Media player
        public FlyleafLib.MediaPlayer.Player HostPlayer { get; set; }
        public FlyleafLib.Config Config { get; set; }

        readonly MainWindow host;

        Track TimeLineTrack => host.TimelineSlider.Template.FindName("PART_Track", host.TimelineSlider) as Track;

        private readonly MetroDialogSettings dialogSettings = new MetroDialogSettings
        {
            AnimateHide = false,
            AnimateShow = false
        };
        public string Title => $"YALC - {YALCConstants.ASSEMBLY_INFORMATIONAL_VERSION}";
        TimeSpan SourceDuration;
        float SourceFrameRate;

        public MainWindowVM(MainWindow mainWindow)
        {
            //Media player
            FlyleafLib.Engine.Start(new FlyleafLib.EngineConfig()
            {
                //PluginsPath = ":Plugins",
                FFmpegPath = ":FFmpeg",

                // Use UIRefresh to update Stats/BufferDuration (and CurTime more frequently than a second)
                UIRefresh = true,
                UIRefreshInterval = 100,
                UICurTimePerSecond = false // If set to true it updates when the actual timestamps second change rather than a fixed interval
            });

            host = mainWindow;
            host.InputBindings.Add(new KeyBinding(Jump1FrameForward, new KeyGesture(Key.Right, ModifierKeys.Shift | ModifierKeys.Control)));
            host.InputBindings.Add(new KeyBinding(JumpXSecondForward, new KeyGesture(Key.Right, ModifierKeys.Control)) { CommandParameter = 1 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondForward, new KeyGesture(Key.Right, ModifierKeys.Shift)) { CommandParameter = 10 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondForward, new KeyGesture(Key.Right, ModifierKeys.None)) { CommandParameter = 60 });

            host.InputBindings.Add(new KeyBinding(Jump1FrameBackward, new KeyGesture(Key.Left, ModifierKeys.Shift | ModifierKeys.Control)));
            host.InputBindings.Add(new KeyBinding(JumpXSecondBackward, new KeyGesture(Key.Left, ModifierKeys.Control)) { CommandParameter = 1 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondBackward, new KeyGesture(Key.Left, ModifierKeys.Shift)) { CommandParameter = 1 });
            host.InputBindings.Add(new KeyBinding(JumpXSecondBackward, new KeyGesture(Key.Left, ModifierKeys.None)) { CommandParameter = 60 });

            LoadVideoFileFromCommandline();
        }

        internal void MainWindowLoaded()
        {
            Config = new FlyleafLib.Config();
            Config.Player.AutoPlay = false;
            HostPlayer = new(Config);
            OnPropertyChanged(nameof(HostPlayer));

            //Show first frame instead of black screen
            HostPlayer.OpenCompleted += (o, e) =>
            {
                if (e.Success)
                {
                    HostPlayer.Seek(0);
                }
            };
            HostPlayer.OpenCompleted += HostPlayer_OpenCompleted;
        }
        private void HostPlayer_OpenCompleted(object sender, FlyleafLib.MediaPlayer.OpenCompletedArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    HostPlayer.Audio.Volume = 0;
                    WaitForDurationInfo();
                }
                else if (e.Error != "Cancelled")
                {
                        SourceFile = null;
                }
            });
        }
        async void LoadVideoFileFromCommandline()
        {
            foreach (var s in Environment.GetCommandLineArgs())
            {
                var file = s.Trim('\"');
                if (file.EndsWith(".dll") || file.EndsWith(".exe")) continue;
                if (File.Exists(file))
                {
                    await Task.Delay(1000);
                    SourceFile = file;
                    break;
                }
            }
        }


        readonly ConcurrentQueue<VideoSegment> ProcessingQueue = new ConcurrentQueue<VideoSegment>();

        public ObservableCollection<VideoSegment> ProcessingQueueList { get; } =
            new ObservableCollection<VideoSegment>();

        public ObservableCollection<VideoSegment> ProjectSegmentList { get; } =
            new ObservableCollection<VideoSegment>();

        VideoSegment _SelectedSegment;

        public VideoSegment SelectedSegment
        {
            get => _SelectedSegment;
            set
            {
                if (!Set(ref _SelectedSegment, value)) return;
                if (value == null)
                {
                    if (ProjectSegmentList.Count == 0)
                    {
                        //Clear everything
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
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                    new Action(() => { })).Wait();
                MarkerTimeline(0);
                host.TimelineSlider.Value = value.CutTo.TotalMilliseconds;
                Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                    new Action(() => { })).Wait();
                MarkerTimeline(1);
            }
        }


        string _SourceFileName;

        public string SourceFileName
        {
            get => _SourceFileName;
            private set => Set(ref _SourceFileName, value);
        }

        string _SourceFile;

        public string SourceFile
        {
            get => _SourceFile;
            set
            {
                if (!Set(ref _SourceFile, value)) return;
                if (string.IsNullOrEmpty(value))
                {
                    HostPlayer.Stop();
                    ProjectSegmentList.Clear();
                    SourceFileName = string.Empty;
                    host.CutMarker.X1 = 0d;
                    host.CutMarker.X2 = 0d;
                    CommandManager.InvalidateRequerySuggested();
                    return;
                }

                SourceFileName = $"{Path.GetFileName(_SourceFile)} ({Utils.ToBytes(new FileInfo(_SourceFile).Length)})";

                HostPlayer.OpenAsync(_SourceFile);
            }
        }
        void WaitForDurationInfo()
        {
            SourceFrameRate = (float)HostPlayer.Video.FPS;
            SourceDuration = TimeSpan.FromTicks(HostPlayer.Duration);
            var project = new VideoSegment(host) { SourceFile = _SourceFile, MaxDuration = SourceDuration };
            host.TimelineSlider.Maximum = SourceDuration.TotalMilliseconds;

            host.TimelineSlider.Value = 0;
            ProjectSegmentList.Add(project);
            SelectedSegment = project;
            OnPropertyChanged(nameof(Title));
            CommandManager.InvalidateRequerySuggested();
        }

        public DelegateCommand SetLeftPosition => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CutFrom = TimeSpan.FromTicks(HostPlayer.CurTime);
            MarkerTimeline(0);
        });

        public DelegateCommand SetRightPosition => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            SelectedSegment.CutTo = TimeSpan.FromTicks(HostPlayer.CurTime);
            MarkerTimeline(1);

        });

        public DelegateCommand AddNewSegment => new DelegateCommand(() =>
        {
            var project = new VideoSegment(host)
            {
                SourceFile = SourceFile,
                MaxDuration = SourceDuration,
                CutTo = SourceDuration,
                CutFrom = SelectedSegment?.CutTo ?? TimeSpan.Zero,
            };
            ProjectSegmentList.Add(project);
            SelectedSegment = project;
            host.TimelineSlider.Value = project.CutFrom.TotalMilliseconds;
        });

        public async void DeleteSegment(VideoSegment segment)
        {
            if (segment == null) return;
            if (Settings.Instance.ShowConfirmationPrompts)
            {
                host.MediaHost.Visibility = Visibility.Hidden;
                var result = await host.ShowMessageAsync("Confirmation", "Remove segment from list?",
                MessageDialogStyle.AffirmativeAndNegative, settings: dialogSettings);
                host.MediaHost.Visibility = Visibility.Visible;
                if (result != MessageDialogResult.Affirmative) return;
            }
            if (segment == SelectedSegment)
                SelectedSegment = null;
            ProjectSegmentList.Remove(segment);
        }
        public async void DeleteSegmentFromQueue(VideoSegment segment)
        {
            if (segment == null) return;
            if (Settings.Instance.ShowConfirmationPrompts)
            {
                host.MediaHost.Visibility = Visibility.Hidden;
                var result = await host.ShowMessageAsync("Confirmation", "Remove segment from queue?",
                MessageDialogStyle.AffirmativeAndNegative, settings: dialogSettings);
                host.MediaHost.Visibility = Visibility.Visible;
                if (result != MessageDialogResult.Affirmative) return;
            }
            segment.MarkedForDeletion = true;
            ProcessingQueueList.Remove(segment);
        }

        public DelegateCommand RemoveAllSegments => new DelegateCommand(async () =>
        {
            if (Settings.Instance.ShowConfirmationPrompts)
            {
                host.MediaHost.Visibility = Visibility.Hidden;
                var result = await host.ShowMessageAsync("Confirmation", "Clear segment list?",
                    MessageDialogStyle.AffirmativeAndNegative, settings: dialogSettings);
                host.MediaHost.Visibility = Visibility.Visible;
                if (result != MessageDialogResult.Affirmative) return;
            }
            SelectedSegment = null;
            ProjectSegmentList.Clear();
        });

        public DelegateCommand<int> JumpXSecondForward => new DelegateCommand<int>((i) =>
         {
             if (string.IsNullOrEmpty(SourceFile)) return;
             if (SelectedSegment.CurrentPosition + TimeSpan.FromSeconds(i) >
                 SourceDuration)
                 SelectedSegment.CurrentPosition = SourceDuration;
             else
                 SelectedSegment.CurrentPosition += TimeSpan.FromSeconds(i);
         });
        public DelegateCommand Jump1FrameForward => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition + TimeSpan.FromMilliseconds(SourceFrameRate) > SourceDuration)
                SelectedSegment.CurrentPosition = SourceDuration;
            else
                SelectedSegment.CurrentPosition += TimeSpan.FromMilliseconds(SourceFrameRate);
        });
        public DelegateCommand Jump1FrameBackward => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition - TimeSpan.FromMilliseconds(SourceFrameRate) < TimeSpan.Zero)
                SelectedSegment.CurrentPosition = TimeSpan.Zero;
            else
                SelectedSegment.CurrentPosition -= TimeSpan.FromMilliseconds(SourceFrameRate);
        });

        public DelegateCommand<int> JumpXSecondBackward => new DelegateCommand<int>((i) =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            if (SelectedSegment.CurrentPosition - TimeSpan.FromSeconds(i) < TimeSpan.Zero)
                SelectedSegment.CurrentPosition = TimeSpan.Zero;
            else
                SelectedSegment.CurrentPosition -= TimeSpan.FromSeconds(i);
        });

        public DelegateCommand PlayVideo => new DelegateCommand(() => HostPlayer.Play());
        public DelegateCommand PauseVideo => new DelegateCommand(() => HostPlayer.Pause());
        public DelegateCommand CreateGIF => new DelegateCommand(async () =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            var sfd = new System.Windows.Forms.SaveFileDialog { DefaultExt = "gif", AddExtension = true, Filter = "GIF|*.gif" };
            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            await FfmpegUtil.CreateGIF(SourceFile, sfd.FileName, SelectedSegment.CutFrom, SelectedSegment.CutTo, -1);
        });

        public DelegateCommand CheckForUpdate => new DelegateCommand(async () =>
        {
            var hasUpdate = await UpdateUtil.IsNewVersionAvailable();
            if (hasUpdate != true)
            {
                await host.ShowMessageAsync("Information",
                    hasUpdate == null ? "Failed to check for updates" : "You're using the latest version", settings: dialogSettings);
                return;
            }

            var result = await host.ShowMessageAsync("Information",
                "New version available. Do you want to visit the download site?",
                MessageDialogStyle.AffirmativeAndNegative, settings: dialogSettings);
            if (result == MessageDialogResult.Negative) return;
            Process.Start(
                new ProcessStartInfo("https://github.com/0x90d/YALC/releases/latest") { UseShellExecute = true });
        });

        public DelegateCommand CutVideo => new DelegateCommand(() =>
        {
            if (string.IsNullOrEmpty(SourceFile)) return;
            string file = new(SourceFile);
            while (ProjectSegmentList.Count > 0)
            {
                ProjectSegmentList[0].Status = ProgressStatus.Waiting;
                ProcessingQueueList.Add(ProjectSegmentList[0]);
                ProcessingQueue.Enqueue(ProjectSegmentList[0]);
                ProjectSegmentList.RemoveAt(0);
            }
            this.SaveQueue();
            if (Settings.Instance.AutoStartQueue)
                StartQueue.Execute();
        });


        //public DelegateCommand DeleteSource => new DelegateCommand(async () =>
        //{
        //    var result = await host.ShowMessageAsync("Confirmation", $"Delete {SourceFile}?",
        //        MessageDialogStyle.AffirmativeAndNegative);
        //    if (result != MessageDialogResult.Affirmative) return;
        //    host.MediaElement1.Stop();
        //    host.MediaElement1.Close();
        //    host.MediaElement1.Source = null;

        //});

        public DelegateCommand PickOutputDirectory => new DelegateCommand(() =>
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            Settings.Instance.OutputDirectory = dialog.SelectedPath;
        });

        public DelegateCommand ClearFinishedQueue => new DelegateCommand(() =>
        {
            for (int i = ProcessingQueueList.Count - 1; i >= 0; i--)
            {
                if (ProcessingQueueList[i].Status == ProgressStatus.Finished)
                    ProcessingQueueList.RemoveAt(i);
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
            if (string.IsNullOrEmpty(SourceFile) == false)
            {

                Point relativePoint = TimeLineTrack.Thumb.TransformToAncestor(host.TimelineGrid)
                    .Transform(new Point(0, 0));
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

        private bool queueIsBusy;
        public DelegateCommand StartQueue => new DelegateCommand(() =>
        {
            //Re-add failed items
            for (int i = 0; i < ProcessingQueueList.Count; i++)
            {
                if (ProcessingQueueList[i].Status != ProgressStatus.Failed) continue;
                ProcessingQueueList[i].Status = ProgressStatus.Waiting;
                ProcessingQueue.Enqueue(ProcessingQueueList[i]);
            }
            if (queueIsBusy)
                return;
            queueIsBusy = true;

            StartQueueInternal();
        });

        async void StartQueueInternal()
        {
            var fileList = new List<VideoSegment>();

            while (ProcessingQueue.TryDequeue(out var videoSegment))
            {
                if (videoSegment.MarkedForDeletion) continue;
                this.SaveQueue();

                host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                var result = await videoSegment.Cut();
                if (result.Success == false)
                {
                    host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Error;
                    host.MediaHost.Visibility = Visibility.Hidden;
                    await host.ShowMessageAsync("Error", result.Error.ToString(), settings: dialogSettings);
                    host.MediaHost.Visibility = Visibility.Visible;
                }
                else
                {
                    //See if this is a new source file
                    bool isNewSourceFile = ProcessingQueue.IsEmpty || ProcessingQueue.TryPeek(out var nextSegment) &&
                                           !videoSegment.SourceFile.Equals(nextSegment.SourceFile);

                    if (!Settings.Instance.MergeSegments && Settings.Instance.RemoveFinishedSegments)
                        ProcessingQueueList.Remove(videoSegment);
                    else if (Settings.Instance.MergeSegments && fileList.Count > 0)
                    {
                        if (isNewSourceFile)
                        {
                            //We cutted the last one, so lets merge
                            host.TaskbarInfo.ProgressState =
                                System.Windows.Shell.TaskbarItemProgressState.Indeterminate;

                            var ouputFilename = Settings.Instance.SaveToSourceFolder
                                ? Path.ChangeExtension(SourceFile, $"_merged{Path.GetExtension(SourceFile)}")
                                : Path.Combine(Settings.Instance.OutputDirectory,
                                    $"{Path.GetFileNameWithoutExtension(SourceFile)}_merged{Path.GetExtension(SourceFile)}");
                            await FfmpegUtil.Merge(ouputFilename, fileList);
                            foreach (var file in fileList)
                                try
                                {
                                    File.Delete(file.OutputFile);
                                }
                                catch
                                {
                                }

                            if (Settings.Instance.RemoveFinishedSegments)
                            {
                                for (int i = 0; i < fileList.Count; i++)
                                    ProcessingQueueList.Remove(fileList[i]);
                            }

                            fileList.Clear();
                        }

                    }

                    if (isNewSourceFile && Settings.Instance.DeleteSourceFileAfterDone)
                    {
                        try
                        {
                            var success = Do(() =>
                            {
                                File.Delete(videoSegment.SourceFile);
                                return true;
                            }, TimeSpan.FromSeconds(0.5));

                            if (!success)
                            {
                                try
                                {
                                    result.Ffmpeg.FfmpegProcess.Kill();
                                }
                                catch { }
                            }
                            await Task.Delay(1000);
                            success = Do(() =>
                            {
                                try
                                {
                                    File.Delete(videoSegment.SourceFile);
                                    return true;
                                }
                                catch (Exception)
                                {
                                    return false;
                                }
                            }, TimeSpan.FromSeconds(0.5));
                        }
                        catch (Exception ex)
                        {
                            host.MediaHost.Visibility = Visibility.Hidden;
                            await host.ShowMessageAsync("Failed to delete file", ex.ToString(), settings: dialogSettings);
                            host.MediaHost.Visibility = Visibility.Visible;
                        }
                    }
                    result.Ffmpeg.FfmpegProcess.Dispose();
                    result.Ffmpeg.FfmpegProcess = null;
                    result.Ffmpeg = null;

                    fileList.Add(videoSegment);
                }

            }
            host.TaskbarInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            queueIsBusy = false;
        }
    }
}
