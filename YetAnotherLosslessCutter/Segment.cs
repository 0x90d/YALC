using System;
using YetAnotherLosslessCutter.MVVM;

namespace YetAnotherLosslessCutter
{
    public enum ProgressStatus
    {
        Idle,
        Waiting,
        Running,
        Merging,
        Failed,
        Finished
    }
    class Segment : ViewModelBase
    {
        
        double _Progress;
        public double Progress
        {
            get => _Progress;
            set
            {
                if (value < 0d || value > 1d) return;
                if (!Set( ref _Progress, value)) return;
                OnPropertyChanged(nameof(ProgressText));
            } 
        }
        public string ProgressText => $"{Math.Round(Progress * 100d)}%";

        ProgressStatus _Status;
        public ProgressStatus Status
        {
            get => _Status;
            set
            {
                if (!Set(ref _Status, value)) return;
                OnPropertyChanged(nameof(IsEnabled));
            } 
        }

        public bool IsEnabled => Status == ProgressStatus.Idle || Status == ProgressStatus.Finished || Status == ProgressStatus.Failed;

    }
}
