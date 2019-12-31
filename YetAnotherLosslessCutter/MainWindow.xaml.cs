using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Input;

namespace YetAnotherLosslessCutter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        readonly MainWindowVM vm;
        public MainWindow()
        {
            vm = new MainWindowVM(this);
            DataContext = vm;
            InitializeComponent();
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
            vm.CurrentPosition = TimeSpan.FromMilliseconds(TimelineSlider.Value); 
        }
        void MediaElement1_MediaOpened(object sender, RoutedEventArgs e)
        {
            TimelineSlider.Maximum = MediaElement1.NaturalDuration.TimeSpan.TotalMilliseconds;
            TimelineSlider.Value = 0;
        }
    }
}
