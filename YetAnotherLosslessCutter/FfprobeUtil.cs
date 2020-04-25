using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using YetAnotherLosslessCutter.FFProbe;

namespace YetAnotherLosslessCutter
{
   sealed class FfprobeUtil
    {
        readonly string FfprobePath;
        public FfprobeUtil() => GetFfprobePath(ref FfprobePath);

        public MediaInfo GetInfos(string inputFile)
        {
            var ms = new MemoryStream();
            Process FFProbeProcess = null;
            try
            {
                var arguments = $" -hide_banner -loglevel error -print_format json -sexagesimal -show_format -show_streams  \"{inputFile}\"";
                var processStartInfo =
                    new ProcessStartInfo(FfprobePath, arguments)
                    {
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(FfprobePath),
                        RedirectStandardInput = false,
                        RedirectStandardOutput = true,
                    };

                FFProbeProcess = Process.Start(processStartInfo);
                if (FFProbeProcess == null)
                {
                    return null;
                }

                //start reading here, otherwise the streams fill up and ffmpeg will block forever
                var imgDataTask = FFProbeProcess.StandardOutput.BaseStream.CopyToAsync(ms);

                if (!FFProbeProcess.HasExited)
                {
                    //Wait for process to exit
                    var numberOfRetries = 0;
                    while (!FFProbeProcess.WaitForExit(5000) && numberOfRetries < 5)
                    {
                        numberOfRetries++;
                    }

                    if (numberOfRetries == 5)
                    {
                        if (FFProbeProcess != null && !FFProbeProcess.HasExited)
                            try
                            {
                                FFProbeProcess.Kill();
                            }
                            catch { }
                        return null;
                    }
                }

                if (!imgDataTask.Wait(5000))
                {
                    throw new TimeoutException($"\'{inputFile}\' ffprobe timed out on retrieving infos");
                }

                var result = FFProbeJsonReader.Read(ms.ToArray(), inputFile);
                return result;

            }
            catch (Exception)
            {
                if (FFProbeProcess != null && !FFProbeProcess.HasExited)
                    try
                    {
                        FFProbeProcess.Kill();
                    }
                    catch { }
                return null;
            }
            finally
            {
                FFProbeProcess?.Close();
            }
        }


        static void GetFfprobePath(ref string ffmpegPath)
        {
            var currentDir = Path.GetDirectoryName(typeof(FfmpegUtil).Assembly.Location);
            var pathsEnv = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);

            if (File.Exists(currentDir + "\\bin\\ffprobe.exe"))
                ffmpegPath = currentDir + "\\bin\\ffprobe";
            else if (File.Exists(currentDir + "\\ffprobe.exe"))
                ffmpegPath = currentDir + "\\ffprobe";

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
                        ffmpegPath = files.FirstOrDefault(x => x.Name.StartsWith("ffprobe", true, CultureInfo.InvariantCulture))
                                         ?.FullName ?? string.Empty;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            if (string.IsNullOrEmpty(ffmpegPath))
                MessageBox.Show("Ffprobe.exe is missing. Please download and put it into the same folder as this application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
