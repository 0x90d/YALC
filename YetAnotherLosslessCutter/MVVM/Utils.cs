using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace YetAnotherLosslessCutter.MVVM
{
    static class Utils
    {
        internal const string FfmpegVersionRequired = "5.1 GPL SHARED";
        static readonly object LockObject = new();
        public static void SaveQueue(this MainWindowVM vm, bool clearSavedFiles = true)
        {
            lock (LockObject)
            {
                var dirInfo = new DirectoryInfo(Path.Combine(Settings.CurrentFolder, "Queue"));

                if (clearSavedFiles && dirInfo.Exists)
                {
                    try
                    {
                        dirInfo.Delete(true);
                    }
                    catch { }
                }

                dirInfo.Create();

                //Save unfinished items
                if (vm.ProcessingQueueList.Count > 0)
                {

                    for (int i = 0; i < vm.ProcessingQueueList.Count; i++)
                    {
                        if (vm.ProcessingQueueList[i].Status == ProgressStatus.Finished) continue;
                        File.WriteAllText(Path.Combine(dirInfo.FullName, Path.GetFileNameWithoutExtension(vm.ProcessingQueueList[i].OutputFile) + ".json"),
                            JsonSerializer.Serialize(vm.ProcessingQueueList[i]));
                    }
                }
            }

        }

        static readonly IList<string> Units = new List<string>(){
            "B", "KB", "MB", "GB", "TB"
        };
        /// <summary>
        /// Formats the value as a filesize in bytes (KB, MB, etc.)
        /// </summary>
        /// <param name="bytes">This value.</param>
        /// <returns>Filesize and quantifier formatted as a string.</returns>
        public static string ToBytes(long bytes)
        {
            double pow = Math.Floor((bytes > 0 ? Math.Log(bytes) : 0) / Math.Log(1024));
            pow = Math.Min(pow, Units.Count - 1);
            double value = (double)bytes / Math.Pow(1024, pow);
            return value.ToString(pow == 0 ? "F0" : "F1") + " " + Units[(int)pow];
        }
    }
}
