using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using YetAnotherLosslessCutter.MVVM;

namespace YetAnotherLosslessCutter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        bool loadedItems;
        readonly MainWindowVM vm;
        public MainWindow()
        {
            vm = new MainWindowVM(this);
            DataContext = vm;
            InitializeComponent();
            ((MainWindowVM)DataContext).MainWindowLoaded();
            Closing += MainWindow_Closing;
            Loaded += MainWindow_Loaded;

        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (loadedItems) return;
            loadedItems = true;

            var dirInfo = new DirectoryInfo(Path.Combine(Settings.CurrentFolder, "Queue"));
            if (!dirInfo.Exists) return;

            foreach (var segmentFile in dirInfo.GetFiles("*.json").OrderBy(x => x.LastWriteTime))
            {
                var segment = JsonSerializer.Deserialize<VideoSegment>(File.ReadAllText(segmentFile.FullName));
                segment.Status = ProgressStatus.Failed;
                segment.Progress = 0d;
                segment.initialized = true;
                vm.ProcessingQueueList.Add(segment);
            }
            while (MediaHost.Surface == null)
            {
                await Task.Delay(1000);
            }

            MediaHost.Surface.AllowDrop = true;
            MediaHost.Surface.DragOver += mediaPlayerElement_DragOver;
            MediaHost.Surface.Drop += mediaPlayerElement_Drop;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            vm.SaveQueue();
            Settings.SaveSettings();
        }

        private void mediaPlayerElement_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length >= 1)
                    vm.LoadSourceFile(files[0]);
            }
        }

        private void mediaPlayerElement_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Copy;
        }


        private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (vm.SelectedSegment != null)
                vm.SelectedSegment.CurrentPosition = TimeSpan.FromMilliseconds(TimelineSlider.Value);
        }


        internal bool autoPreview;
        private void Button_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!autoPreview)
                RunAutoPreview(ButtonJumpMinuteForward);
            else
                autoPreview = false;
        }
        private void ButtonJumpTenSecondsForward_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!autoPreview)
                RunAutoPreview(ButtonJumpTenSecondsForward);
            else
                autoPreview = false;
        }
        async void RunAutoPreview(Button button)
        {
            autoPreview = true;
            ButtonAutomationPeer peer = new ButtonAutomationPeer(button);
            IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
            while (autoPreview)
            {
                Application.Current.Invoke(() =>
                {
                    invokeProv.Invoke();
                });
                await Task.Delay(Settings.Instance.AutoPreviewValue);
                if (TimelineSlider.Value >= TimelineSlider.Maximum)
                    autoPreview = false;
            }
        }

        private void ButtonJumpTenSecondsForward_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                return;
            e.Handled = true;
            ButtonJumpTenSecondsForward_PreviewMouseRightButtonDown(sender, e);
        }

        private void ButtonJumpMinuteForward_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftCtrl))
                return;
            e.Handled = true;
            Button_PreviewMouseRightButtonDown(sender, e);
        }
    }
}
