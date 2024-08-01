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

using Microsoft.UI.Xaml.Controls;
using Microsoft.WindowsAPICodePack.Taskbar;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static SRTools.App;

namespace SRTools.Depend
{
    public static class GameUpdate
    {
        public delegate Task LoadDataDelegate(string mode = "?");
        public static event LoadDataDelegate OnLoadData;
        public static HttpClient client = new HttpClient();
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private const string ApiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=osvnlOc0S8&game_ids[]=64kMb5iAWu&game_ids[]=1Z8W5NHUQb&launcher_id=jGHBHlcOq1";

        public static async Task<bool> CheckAndUpdateGame()
        {
            var localGameVersion = await GetLocalGameVersion();
            if (localGameVersion != null)
            {
                if (GameUpdateModel.LatestVersion != localGameVersion)
                {
                    Logging.Write("发现新版本游戏，请更新.", 3, "GameUpdate");
                    NotificationManager.RaiseNotification("发现新版本游戏", $"当前版本: {localGameVersion}\n最新版本: {GameUpdateModel.LatestVersion}\n请及时更新游戏本体", InfoBarSeverity.Warning, true, 3);
                    return true;
                }
                Logging.Write("游戏已是最新版本.", 3, "GameUpdate");
                await CheckResidualFiles();
                return false;
            }
            Logging.Write("无法获取本地游戏版本.", 3, "GameUpdate");
            return false;
        }

        public static async Task<JsonDocument> GetGameInfo()
        {
            var response = await client.GetAsync(ApiUrl);
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(jsonResponse);
            }
            return null;
        }

        public static async Task ExtractGameInfo()
        {
            var gameData = await GetGameInfo();
            if (gameData != null)
            {
                var root = gameData.RootElement;
                var gamePackages = root.GetProperty("data").GetProperty("game_packages");

                foreach (var gamePackage in gamePackages.EnumerateArray())
                {
                    var game = gamePackage.GetProperty("game");
                    if (game.GetProperty("biz").GetString() == "hkrpg_cn")
                    {
                        var main = gamePackage.GetProperty("main");
                        var major = main.GetProperty("major");

                        GameUpdateModel.LatestVersion = major.GetProperty("version").GetString();
                        GameUpdateModel.LatestPaths = major.GetProperty("game_pkgs").EnumerateArray().Select(pkg => pkg.GetProperty("url").GetString()).ToList();
                        GameUpdateModel.LatestSizes = major.GetProperty("game_pkgs").EnumerateArray().Select(pkg => pkg.GetProperty("size").GetString()).ToList();
                        GameUpdateModel.LatestMD5s = major.GetProperty("game_pkgs").EnumerateArray().Select(pkg => pkg.GetProperty("md5").GetString()).ToList();

                        var audioPkgs = major.GetProperty("audio_pkgs");
                        foreach (var audioPkg in audioPkgs.EnumerateArray())
                        {
                            if (audioPkg.GetProperty("language").GetString() == "zh-cn")
                            {
                                GameUpdateModel.LatestVoicePacksPath = audioPkg.GetProperty("url").GetString();
                                GameUpdateModel.LatestVoicePacksSize = audioPkg.GetProperty("size").GetString();
                                GameUpdateModel.LatestVoicePacksMD5 = audioPkg.GetProperty("md5").GetString();
                                break;
                            }
                        }

                        var localVersion = await GetLocalGameVersion();
                        var patches = main.GetProperty("patches");
                        foreach (var patch in patches.EnumerateArray())
                        {
                            GameUpdateModel.DiffsVersion = patch.GetProperty("version").GetString();
                            var patchPkg = patch.GetProperty("game_pkgs")[0];
                            GameUpdateModel.DiffsPath = patchPkg.GetProperty("url").GetString();
                            GameUpdateModel.DiffsSize = patchPkg.GetProperty("size").GetString();
                            GameUpdateModel.DiffsMD5 = patchPkg.GetProperty("md5").GetString();

                            var patchAudioPkgs = patch.GetProperty("audio_pkgs");
                            foreach (var patchAudioPkg in patchAudioPkgs.EnumerateArray())
                            {
                                if (patchAudioPkg.GetProperty("language").GetString() == "zh-cn")
                                {
                                    GameUpdateModel.DiffsVoicePacksPath = patchAudioPkg.GetProperty("url").GetString();
                                    GameUpdateModel.DiffsVoicePacksSize = patchAudioPkg.GetProperty("size").GetString();
                                    GameUpdateModel.DiffsVoicePacksMD5 = patchAudioPkg.GetProperty("md5").GetString();
                                    GameUpdateModel.isGetUpdateInfo = true;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                Logging.Write("Failed to retrieve game data.");
            }
        }

        private static async Task<string> GetLocalGameVersion()
        {
            try
            {
                var gameDirectory = await GetGameDirectory();
                if (gameDirectory != null)
                {
                    var configFilePath = Path.Combine(gameDirectory, "config.ini");
                    if (File.Exists(configFilePath))
                    {
                        var lines = await File.ReadAllLinesAsync(configFilePath);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("game_version="))
                            {
                                var match = Regex.Match(line, @"\d+(\.\d+)+");
                                if (match.Success)
                                {
                                    Logging.Write($"ZenlessZoneZero Current Version: {match.Value}", 3, "GameUpdate");
                                    return match.Value;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logging.Write("config.ini 文件不存在.", 3, "GameUpdate");
                    }
                }
                else
                {
                    Logging.Write("无法获取游戏目录.", 3, "GameUpdate");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"获取游戏版本时出现错误: {ex.Message}");
            }
            return null;
        }

        private static async Task<bool> CheckResidualFiles()
        {
            var gameDirectory = await GetGameDirectory();
            if (gameDirectory != null)
            {
                var fileNames = new List<string>();
                fileNames.AddRange(GameUpdateModel.LatestPaths.Select(Path.GetFileName));
                if (!string.IsNullOrEmpty(GameUpdateModel.LatestVoicePacksPath))
                {
                    fileNames.Add(Path.GetFileName(GameUpdateModel.LatestVoicePacksPath));
                }
                if (!string.IsNullOrEmpty(GameUpdateModel.DiffsPath))
                {
                    fileNames.Add(Path.GetFileName(GameUpdateModel.DiffsPath));
                }
                foreach (var fileName in fileNames)
                {
                    var filePath = Path.Combine(gameDirectory, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Logging.Write($"删除残留文件: {filePath}", 3, "GameUpdate");
                    }
                }

                // 删除 deletefiles.txt 和 hdifffiles.txt
                var deleteFilesPath = Path.Combine(gameDirectory, "deletefiles.txt");
                var hdiffFilesPath = Path.Combine(gameDirectory, "hdifffiles.txt");

                if (File.Exists(deleteFilesPath))
                {
                    File.Delete(deleteFilesPath);
                    Logging.Write($"删除文件: {deleteFilesPath}", 3, "GameUpdate");
                }

                if (File.Exists(hdiffFilesPath))
                {
                    File.Delete(hdiffFilesPath);
                    Logging.Write($"删除文件: {hdiffFilesPath}", 3, "GameUpdate");
                }

                await DeleteFiles();
                return true;
            }
            return false;
        }

        private static async Task<bool> DeleteFiles()
        {
            var gameDirectory = await GetGameDirectory();
            if (gameDirectory != null)
            {
                var deleteFilesPath = Path.Combine(gameDirectory, "deletefiles.txt");
                if (File.Exists(deleteFilesPath))
                {
                    var filesToDelete = await File.ReadAllLinesAsync(deleteFilesPath);
                    foreach (var file in filesToDelete)
                    {
                        var filePath = Path.Combine(gameDirectory, file);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private static async Task<string> GetGameDirectory()
        {
            return AppDataController.GetGamePathWithoutGameName();
        }

        public static async Task<bool> StartDownload(Action<double, string, string> progressChanged)
        {
            DownloadHelpers.isDownloading = true;
            DownloadHelpers.isPaused = false;
            var downloadUrls = GameUpdateModel.LatestPaths;
            if (GameUpdateModel.DiffsVersion == await GetLocalGameVersion())
            {
                downloadUrls = new List<string> { GameUpdateModel.DiffsPath };
            }
            var filePaths = downloadUrls.Select(url => Path.Combine(AppDataController.GetGamePathWithoutGameName(), Path.GetFileName(url))).ToList();
            var stopwatch = new Stopwatch();

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                var totalSize = downloadUrls.Select(url => GetFileSize(url)).Sum(); // 动态获取总大小
                var totalRead = 0L;

                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        totalRead += new FileInfo(filePath).Length;
                    }
                }

                foreach (var (downloadUrl, filePath) in downloadUrls.Zip(filePaths, Tuple.Create))
                {
                    if (DownloadHelpers.isPaused) break;

                    var existingLength = File.Exists(filePath) ? new FileInfo(filePath).Length : 0;

                    using (var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
                    {
                        request.Headers.Range = new RangeHeaderValue(existingLength, null);
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                                {
                                    continue;
                                }
                                Logging.Write("下载失败，状态码：" + response.StatusCode);
                                return false;
                            }

                            var totalBytes = (response.Content.Headers.ContentLength ?? 0) + existingLength;
                            var buffer = new byte[81920];
                            var isMoreToRead = true;
                            var initialTotalRead = totalRead;
                            stopwatch.Start();

                            using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, buffer.Length, true))
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                var lastUpdateTime = DateTime.Now;
                                var downloadStartTime = DateTime.Now;
                                var initialDownloadedBytes = existingLength;
                                var totalBytesWritten = 0L;
                                const long chunkSize = 100 * 1024 * 1024;

                                do
                                {
                                    if (DownloadHelpers.isPaused) break;

                                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                                    if (read == 0)
                                    {
                                        isMoreToRead = false;
                                    }
                                    else
                                    {
                                        await fileStream.WriteAsync(buffer, 0, read, cancellationTokenSource.Token);
                                        totalRead += read;
                                        totalBytesWritten += read;

                                        if (totalBytesWritten >= chunkSize)
                                        {
                                            await fileStream.FlushAsync(cancellationTokenSource.Token);
                                            totalBytesWritten = 0;
                                        }

                                        var totalElapsedTime = (DateTime.Now - downloadStartTime).TotalSeconds;
                                        var speed = (totalRead - initialTotalRead) / totalElapsedTime;
                                        var speedMb = speed / (1024 * 1024);
                                        var sizeMb = totalRead / (1024 * 1024);
                                        var totalSizeMb = totalSize / (1024 * 1024);

                                        if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 1)
                                        {
                                            var percentage = (double)totalRead / totalSize * 100;
                                            progressChanged?.Invoke(percentage, $"{speedMb:F2}MB/s", $"{sizeMb:F2}MB/{totalSizeMb:F2}MB");
                                            DownloadHelpers.UpdateProgress(percentage, $"{speedMb:F2}MB/s", $"{sizeMb:F2}MB/{totalSizeMb:F2}MB");
                                            lastUpdateTime = DateTime.Now;
                                            CommonHelpers.TaskbarHelper.SetProgressState(TaskbarProgressBarState.Normal);
                                            CommonHelpers.TaskbarHelper.SetProgressValue((int)percentage, 100);
                                        }
                                    }
                                } while (isMoreToRead);

                                await fileStream.FlushAsync(cancellationTokenSource.Token);
                            }
                            stopwatch.Stop();
                        }
                    }
                }

                if (!DownloadHelpers.isPaused)
                {
                    return await DownloadFinishedAsync();
                }
            }
            catch (OperationCanceledException)
            {
                Logging.Write("下载已暂停", 3, "GameUpdate");
            }
            catch (Exception ex)
            {
                Logging.Write("下载异常：" + ex.Message);
            }
            return false;
        }

        private static long GetFileSize(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = client.SendAsync(request).Result;
            return response.Content.Headers.ContentLength ?? 0;
        }

        public static void PauseDownload()
        {
            Logging.Write("暂停下载", 3, "GameUpdate");
            cancellationTokenSource?.Cancel();
            DownloadHelpers.isDownloading = false;
            DownloadHelpers.isPaused = true;
        }

        public static void ResumeDownload(Action<double, string, string> progressChanged)
        {
            Logging.Write("恢复下载", 3, "GameUpdate");
            StartDownload(progressChanged);
        }

        public static async Task<bool> DownloadFinishedAsync()
        {
            List<string> filePaths = new List<string>();
            List<string> md5s = new List<string>();
            if (GameUpdateModel.DiffsPath != null)
            {
                // 处理增量更新包路径
                string fileName = Path.GetFileName(GameUpdateModel.DiffsPath);
                string localPath = Path.Combine(AppDataController.GetGamePathWithoutGameName(), fileName);
                filePaths.Add(localPath);
                md5s.Add(GameUpdateModel.DiffsMD5);
            }
            else
            {
                // 处理最新包路径
                foreach (var url in GameUpdateModel.LatestPaths)
                {
                    string fileName = Path.GetFileName(url);
                    string localPath = Path.Combine(AppDataController.GetGamePathWithoutGameName(), fileName);
                    filePaths.Add(localPath);
                }
                md5s = GameUpdateModel.LatestMD5s;
            }

            string extractPath = AppDataController.GetGamePathWithoutGameName();

            try
            {
                NotificationManager.RaiseNotification("下载完成", "", InfoBarSeverity.Success, true, 2);
                DownloadHelpers.isPaused = true;

                for (int i = 0; i < filePaths.Count; i++)
                {
                    var progress = new Progress<int>(percent =>
                    {
                        WaitOverlayManager.RaiseWaitOverlay(true, "正在校验MD5", $"分卷{i + 1}/{filePaths.Count}", true, percent);
                        CommonHelpers.TaskbarHelper.SetProgressValue(percent, 100);
                        CommonHelpers.TaskbarHelper.SetProgressState(TaskbarProgressBarState.Normal);
                    });
                    var md5Verified = await VerifyFileMD5Async(filePaths[i], md5s[i], progress);
                    if (!md5Verified)
                    {
                        throw new Exception($"MD5 校验失败，文件可能已损坏。 文件: {filePaths[i]}");
                    }
                }

                cancellationTokenSource?.Cancel();

                var extractProgress = new Progress<int>(percent =>
                {
                    WaitOverlayManager.RaiseWaitOverlay(true, "正在解压", "请稍等", true, percent);
                    CommonHelpers.TaskbarHelper.SetProgressValue(percent, 100);
                    CommonHelpers.TaskbarHelper.SetProgressState(TaskbarProgressBarState.Normal);
                });

                // 确定是单个ZIP文件还是分卷ZIP文件
                if (filePaths.Count == 1)
                {
                    await ExtractSingleArchiveAsync(filePaths[0], extractPath, extractProgress);
                }
                else
                {
                    await ExtractSplitArchiveAsync(filePaths[0], extractPath, extractProgress);
                }

                WaitOverlayManager.RaiseWaitOverlay(false);
                NotificationManager.RaiseNotification("解压完成", "游戏文件已成功解压。", InfoBarSeverity.Success, true, 2);

                await UpdateConfigVersion(GameUpdateModel.LatestVersion);
                NotificationManager.RaiseNotification("更新完成", $"游戏已更新到{GameUpdateModel.LatestVersion}。", InfoBarSeverity.Success, true, 2);
                CommonHelpers.TaskbarHelper.SetProgressState(TaskbarProgressBarState.NoProgress);

                ResetDownloadHelpersVariables();
                if (OnLoadData != null)
                {
                    await OnLoadData.Invoke();
                }
                return true;
            }
            catch (OperationCanceledException)
            {
                WaitOverlayManager.RaiseWaitOverlay(false);
                NotificationManager.RaiseNotification("操作被取消", "解压过程已被手动停止。", InfoBarSeverity.Warning);
            }
            catch (Exception ex)
            {
                WaitOverlayManager.RaiseWaitOverlay(false);
                NotificationManager.RaiseNotification("解压失败", $"发生错误：{ex.Message}", InfoBarSeverity.Error);
            }
            return false;
        }


        private static async Task<bool> VerifyFileMD5Async(string filePath, string expectedMD5, IProgress<int> progress)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var fileLength = stream.Length;
                    var totalRead = 0L;
                    var buffer = new byte[81920];
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        md5.TransformBlock(buffer, 0, read, buffer, 0);
                        totalRead += read;
                        var percentComplete = (int)((double)totalRead / fileLength * 100);
                        progress.Report(percentComplete);
                    }
                    md5.TransformFinalBlock(buffer, 0, 0);

                    var hash = md5.Hash;
                    var md5String = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return md5String == expectedMD5.ToLowerInvariant();
                }
            }
        }

        private static async Task UpdateConfigVersion(string newVersion)
        {
            try
            {
                var gameDirectory = await GetGameDirectory();
                if (gameDirectory != null)
                {
                    var configFilePath = Path.Combine(gameDirectory, "config.ini");
                    if (File.Exists(configFilePath))
                    {
                        var lines = await File.ReadAllLinesAsync(configFilePath);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].StartsWith("game_version="))
                            {
                                lines[i] = "game_version=" + newVersion;
                                break;
                            }
                        }
                        await File.WriteAllLinesAsync(configFilePath, lines);
                        Logging.Write("Config版本号已更新.", 3, "GameUpdate");
                    }
                    else
                    {
                        Logging.Write("Config文件不存在.", 3, "GameUpdate");
                    }
                }
                else
                {
                    Logging.Write("无法获取游戏目录.", 3, "GameUpdate");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"更新游戏版本时出现错误: {ex.Message}");
            }
        }

        private static void ResetDownloadHelpersVariables()
        {
            DownloadHelpers.CurrentProgress = 0;
            DownloadHelpers.CurrentSpeed = string.Empty;
            DownloadHelpers.CurrentSize = string.Empty;
            DownloadHelpers.isDownloading = false;
            DownloadHelpers.isPaused = true;
            DownloadHelpers.isFinished = false;
        }

        public static async Task ExtractSingleArchiveAsync(string filePath, string destPath, IProgress<int> progress)
        {
            await Task.Run(() =>
            {
                using (var stream = File.OpenRead(filePath))
                using (var archive = ArchiveFactory.Open(stream))
                {
                    var totalEntries = archive.Entries.Count();
                    int processedEntries = 0;

                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            entry.WriteToDirectory(destPath, new ExtractionOptions()
                            {
                                ExtractFullPath = true,
                                Overwrite = true
                            });
                        }

                        processedEntries++;
                        int percentComplete = (int)((double)processedEntries / totalEntries * 100);
                        progress.Report(percentComplete);
                    }
                }
            });
        }



        public static async Task ExtractSplitArchiveAsync(string firstVolumePath, string destPath, IProgress<int> progress)
        {
            await Task.Run(() =>
            {
                var archive = ArchiveFactory.Open(firstVolumePath);
                var totalEntries = archive.Entries.Count();
                var processedEntries = 0;

                foreach (var entry in archive.Entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(destPath, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }

                    processedEntries++;
                    var percentComplete = (int)((double)processedEntries / totalEntries * 100);
                    progress.Report(percentComplete);
                }
            });
        }


        public class MultiVolumeStream : Stream
        {
            private readonly List<Stream> _streams;
            private int _currentStreamIndex;
            private long _position;

            public MultiVolumeStream(List<string> filePaths)
            {
                _streams = filePaths.Select(path => (Stream)File.OpenRead(path)).ToList();
                _currentStreamIndex = 0;
                _position = 0;
            }

            public override bool CanRead => true;
            public override bool CanSeek => true;
            public override bool CanWrite => false;
            public override long Length => _streams.Sum(s => s.Length);
            public override long Position
            {
                get => _position;
                set => Seek(value, SeekOrigin.Begin);
            }

            public override void Flush() => _streams[_currentStreamIndex].Flush();

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRead = 0;
                while (count > 0 && _currentStreamIndex < _streams.Count)
                {
                    int read = _streams[_currentStreamIndex].Read(buffer, offset, count);
                    if (read == 0)
                    {
                        _currentStreamIndex++;
                    }
                    else
                    {
                        bytesRead += read;
                        offset += read;
                        count -= read;
                        _position += read;
                    }
                }
                return bytesRead;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                if (origin == SeekOrigin.Begin)
                {
                    _position = offset;
                }
                else if (origin == SeekOrigin.Current)
                {
                    _position += offset;
                }
                else if (origin == SeekOrigin.End)
                {
                    _position = Length + offset;
                }

                long remainingOffset = _position;
                for (int i = 0; i < _streams.Count; i++)
                {
                    if (remainingOffset < _streams[i].Length)
                    {
                        _currentStreamIndex = i;
                        _streams[i].Seek(remainingOffset, SeekOrigin.Begin);
                        return _position;
                    }
                    remainingOffset -= _streams[i].Length;
                }

                throw new ArgumentOutOfRangeException();
            }

            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }





    }
}