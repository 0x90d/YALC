using System;
using System.Windows;
using System.Windows.Input;

namespace YetAnotherLosslessCutter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        readonly MainWindowVM vm;
        public MainWindow()
        {
            vm = new MainWindowVM(this);
            DataContext = vm;
            InitializeComponent();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
    }
}
