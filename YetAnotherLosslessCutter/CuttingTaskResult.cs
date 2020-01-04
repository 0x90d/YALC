using System;

namespace YetAnotherLosslessCutter
{
    sealed class CuttingTaskResult
    {
        public readonly bool Success;
        public readonly Exception Error;

        public CuttingTaskResult(bool success, Exception error)
        {
            Success = success;
            Error = error;
        }
    }
}
