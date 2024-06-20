using System;

namespace SRTools.Depend
{
    // 全局状态管理器
    public class DownloadManager
    {
        public static double CurrentProgress { get; set; }
        public static string CurrentSpeed { get; set; }
        public static string CurrentSize { get; set; }
        public static bool isDownloading { get; set; } = false;
        public static bool isPaused { get; set; } = true;
        public static bool isFinished { get; set; } = false;
        public static event Action<double, string, string> DownloadProgressChanged;

        public static void UpdateProgress(double progress, string speed, string size)
        {
            CurrentProgress = progress;
            CurrentSpeed = speed;
            CurrentSize = size;
            DownloadProgressChanged?.Invoke(progress, speed, size);
        }
    }
}
