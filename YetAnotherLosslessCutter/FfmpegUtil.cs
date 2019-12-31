using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace YetAnotherLosslessCutter
{
    static class FfmpegRegex
    {
        public static Regex ProgressRegex = new Regex(@"frame=\s*[^\s]+\s+fps=\s*[^\s]+\s+q=\s*[^\s]+\s+(?:size|Lsize)=\s*[^\s]+\s+time=\s*([^\s]+)\s+", RegexOptions.Compiled | RegexOptions.Singleline);
    }
    public class ProgressEventArgs : EventArgs
    {
        internal ProgressEventArgs(double progress)
        {
            Progress = progress;
        }

        public double Progress { get; }
    }
    sealed class FfmpegUtil
    {
        public event Action<ProgressEventArgs> Progress;
        readonly string FfmpegPath;
        public ProjectSettings settings;
        public FfmpegUtil() => GetFfmpegPath(ref FfmpegPath);

        public void NewProject(string sourceFile)
        {
            settings = new ProjectSettings { SourceFile = sourceFile };
        }

        public async Task Cut()
        {
            var commandLine = BuildCommandLine(settings);

            using var ffmpegProcess = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = commandLine,
                    FileName = FfmpegPath,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            ffmpegProcess.ErrorDataReceived += FfmpegProcess_ErrorDataReceived;

            Task<int> task = null;
            try
            {
                task = ffmpegProcess.WaitForExitAsync(null);
                await task;
            }
            catch (Exception)
            {
                if (task?.IsCanceled == true)
                {
                    throw new TaskCanceledException(task);
                }
                throw;
            }
            if (ffmpegProcess != null && ffmpegProcess.ExitCode != 0)
                throw new Exception("Failed to cut. Uncheck 'Include all streams' and try again. Otherwise run ffmpeg yourself and see what reports to you");
        }

        private void FfmpegProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            var c = FfmpegRegex.ProgressRegex.Matches(e.Data);
            if (c.Count == 0 || c[0].Groups.Count < 2) return;
            if (!TimeSpan.TryParse(c[0].Groups[1].Value, out TimeSpan progress)) return;

            Progress?.Invoke(new ProgressEventArgs(progress / settings.CutDuration));
        }


        static string BuildCommandLine(ProjectSettings settings)
        {
            var sb = new StringBuilder();
            sb.Append(" -hide_banner");
            sb.Append($" -ss {settings.CutFrom}");
            sb.Append($" -i \"{settings.SourceFile}\"");
            sb.Append($" -t {settings.CutDuration}");
            sb.Append($" -avoid_negative_ts make_zero");

            if (settings.RemoveAudio)
                sb.Append($" -an {settings.CutFrom}");
            else
                sb.Append(" -acodec copy");

            sb.Append(" -vcodec copy");
            sb.Append(" -scodec copy");

            if (!settings.IncludeAllStreams)
                sb.Append(" -map 0");

            sb.Append(" -map_metadata 0");
            sb.Append(" -ignore_unknown");

            sb.Append($" -y \"{settings.OutputFile}\"");

            return sb.ToString();
        }

        static void GetFfmpegPath(ref string ffmpegPath)
        {
            var currentDir = Path.GetDirectoryName(typeof(FfmpegUtil).Assembly.Location);
            var pathsEnv = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);

            if (File.Exists(currentDir + "\\bin\\ffmpeg.exe"))
                ffmpegPath = currentDir + "\\bin\\ffmpeg";
            else if (File.Exists(currentDir + "\\ffmpeg.exe"))
                ffmpegPath = currentDir + "\\ffmpeg";

            if (pathsEnv == null || !string.IsNullOrEmpty(ffmpegPath)) return;
            foreach (var path in pathsEnv)
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }
                try
                {
                    var files = new DirectoryInfo(path).GetFiles();

                    if (string.IsNullOrEmpty(ffmpegPath))
                        ffmpegPath = files.FirstOrDefault(x => x.Name.StartsWith("ffmpeg", true, CultureInfo.InvariantCulture))
                                         ?.FullName ?? string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (string.IsNullOrEmpty(ffmpegPath))
                MessageBox.Show("FFmpeg is missing", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}