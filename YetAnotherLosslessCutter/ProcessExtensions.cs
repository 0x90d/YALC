using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace YetAnotherLosslessCutter
{
    public static class ProcessExtensions
    {
        public static Task<int> WaitForExitAsync(this Process process, Action<int> onException)
        {
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                    onException?.Invoke(process.ExitCode);
                tcs.TrySetResult(process.ExitCode);
            };

            var started = process.Start();
            if (!started)
                tcs.TrySetException(new InvalidOperationException($"Could not start process {process}"));

            process.BeginErrorReadLine();

            return tcs.Task;
        }
    }
}
