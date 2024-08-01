// Copyright (c) 2021-2024, JamXi JSG-LLC.
// All rights reserved.

// This file is part of SRTools.

// SRTools is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// SRTools is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with SRTools.  If not, see <http://www.gnu.org/licenses/>.

// For more information, please refer to <https://www.gnu.org/licenses/gpl-3.0.html>

using System;
using static SRTools.App;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;

namespace SRTools.Depend
{
    // 全局状态管理器
    public class DownloadHelpers
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
        public static async Task DownloadSDK(string extrasPath)
        {
            WaitOverlayManager.RaiseWaitOverlay(true, "正在下载额外文件", "请耐心等待", true, 0);
            string url = "https://ds.jamsg.cn/d/Release/SRTools/Extras/PCGameSDK.zip";
            string zipFilePath = Path.Combine(extrasPath, "PCGameSDK.zip");

            Directory.CreateDirectory(extrasPath);

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long totalReadBytes = 0L;
                        int readBytes;

                        while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, readBytes);
                            totalReadBytes += readBytes;
                            int progress = totalBytes != -1L ? (int)((totalReadBytes * 100) / totalBytes) : -1;
                            WaitOverlayManager.RaiseWaitOverlay(true, "正在下载额外文件", "请耐心等待", true, progress);
                        }
                    }
                }
            }

            // 解压缩文件，覆盖已有文件
            ZipFile.ExtractToDirectory(zipFilePath, extrasPath, true);
            File.Delete(zipFilePath);
            WaitOverlayManager.RaiseWaitOverlay(false, "", "", false, 0);
        }

    }
}
