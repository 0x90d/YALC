using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace YetAnotherLosslessCutter
{
    static class FfmpegStatics
    {
        public static Regex ProgressRegex = new Regex(@"frame=\s*[^\s]+\s+fps=\s*[^\s]+\s+q=\s*[^\s]+\s+(?:size|Lsize)=\s*[^\s]+\s+time=\s*([^\s]+)\s+", RegexOptions.Compiled | RegexOptions.Singleline);
        public static string FfmpegPath;

        static FfmpegStatics()
        {
            var currentDir = Path.GetDirectoryName(typeof(FfmpegUtil).Assembly.Location);
            var pathsEnv = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);

            if (File.Exists(currentDir + "\\bin\\ffmpeg.exe"))
                FfmpegPath = currentDir + "\\bin\\ffmpeg";
            else if (File.Exists(currentDir + "\\ffmpeg.exe"))
                FfmpegPath = currentDir + "\\ffmpeg";

            if (pathsEnv == null || !string.IsNullOrEmpty(FfmpegPath)) return;
            foreach (var path in pathsEnv)
            {
                if (!Directory.Exists(path))
                {
                    continue;
                }
                try
                {
                    var files = new DirectoryInfo(path).GetFiles();

                    if (string.IsNullOrEmpty(FfmpegPath))
                        FfmpegPath = files.FirstOrDefault(x => x.Name.StartsWith("ffmpeg", true, CultureInfo.InvariantCulture))
                                         ?.FullName ?? string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (string.IsNullOrEmpty(FfmpegPath))
                MessageBox.Show("FFmpeg is missing", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    public sealed class ProgressEventArgs : EventArgs
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

        public async Task Cut(VideoSegment segment)
        {
            var commandLine = BuildCommandLine(segment);

            using var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = commandLine,
                    FileName = FfmpegStatics.FfmpegPath,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };

            ffmpegProcess.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data == null) return;
                var c = FfmpegStatics.ProgressRegex.Matches(e.Data);
                if (c.Count == 0 || c[0].Groups.Count < 2) return;
                if (!TimeSpan.TryParse(c[0].Groups[1].Value, out TimeSpan progress)) return;

                Progress?.Invoke(new ProgressEventArgs(progress / segment.CutDuration));
            };

            await ffmpegProcess.WaitForExitAsync(null);

            if (ffmpegProcess.ExitCode != 0)
                throw new Exception("Failed to cut. Uncheck 'Include all streams' and try again. Otherwise run ffmpeg yourself and see what reports to you");
        }

        public static async Task Merge(string outputName, List<VideoSegment> files)
        {
            var sb = new StringBuilder();
            foreach (var file in files)
            {
                if (File.Exists(file.OutputFile))
                    sb.AppendLine($"file '{file.OutputFile.Replace("'", @"\'")}'");
            }

            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, sb.ToString());

            using var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = $" -f concat -safe 0 -i \"{tempFile}\" -c copy -map_metadata 0 -y \"{outputName}\"",
                    FileName = FfmpegStatics.FfmpegPath,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
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

            try
            {
                File.Delete(tempFile);
            }
            catch { }

            if (ffmpegProcess != null && ffmpegProcess.ExitCode != 0)
                throw new Exception("Failed to merge.");
        }
        public static async Task<BitmapSource> GetThumbnail(string source, TimeSpan position)
        {
            using var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = $" -hide_banner -loglevel panic -y -ss {position} -i \"{source}\" -t 1 -f mjpeg -vframes 1 -vf scale=120:-1 \"-\"",
                    FileName = FfmpegStatics.FfmpegPath,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            try
            {
                await ffmpegProcess.WaitForExitAsync(null);
                await using var ms = new MemoryStream();
                ffmpegProcess.StandardOutput.BaseStream.CopyTo(ms);
                return (BitmapSource)new ImageSourceConverter().ConvertFrom(ms.ToArray());
            }
            catch (Exception)
            {
                try
                {
                    if (ffmpegProcess.HasExited == false)
                        ffmpegProcess.Kill();
                }
                catch { }
                return null;
            }
        }
        public static async Task CreateGIF(string source, string target, TimeSpan start, TimeSpan end, int width)
        {
            using var ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    Arguments = $" -hide_banner -loglevel panic -y -ss {start} -i \"{source}\" -t {end - start} -vf scale={width}:-1 \"{target}\"",
                    FileName = FfmpegStatics.FfmpegPath,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            };
            try
            {
                await ffmpegProcess.WaitForExitAsync(null);
            }
            catch (Exception)
            {
                try
                {
                    if (ffmpegProcess.HasExited == false)
                        ffmpegProcess.Kill();
                }
                catch { }
            }
        }




        static string BuildCommandLine(VideoSegment segment)
        {
            var sb = new StringBuilder();
            sb.Append(" -hide_banner");
            sb.Append($" -ss {segment.CutFrom}");
            sb.Append($" -i \"{segment.SourceFile}\"");
            sb.Append($" -t {segment.CutDuration}");
            sb.Append(" -avoid_negative_ts make_zero");

            if (Settings.Instance.RemoveAudio)
                sb.Append($" -an {segment.CutFrom}");
            else
                sb.Append(" -acodec copy");

            sb.Append(" -vcodec copy");
            sb.Append(" -scodec copy");

            if (!Settings.Instance.IncludeAllStreams)
                sb.Append(" -map 0");

            sb.Append(" -map_metadata 0");
            sb.Append(" -ignore_unknown");

            sb.Append($" -y \"{segment.OutputFile}\"");

            return sb.ToString();
        }


    }
}