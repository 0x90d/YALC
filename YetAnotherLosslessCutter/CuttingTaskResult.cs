using System;

namespace YetAnotherLosslessCutter
{
    sealed class CuttingTaskResult
    {
        public readonly bool Success;
        public readonly Exception Error;
        public FfmpegUtil Ffmpeg;

        public CuttingTaskResult(bool success, Exception error, FfmpegUtil ffmpeg)
        {
            Success = success;
            Error = error;
            Ffmpeg = ffmpeg;
        }
    }
}
