using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
                    if (!DownloadManager.isDownloading)
                    {
                        Logging.Write("发现新版本游戏，请更新.");
                        NotificationManager.RaiseNotification("发现新版本游戏", "当前版本:" + localGameVersion + "\n最新版本:" + GameUpdateModel.LatestVersion + "\n请及时更新游戏本体", InfoBarSeverity.Warning, true, 3);
                    }
                    return true;
                }
                else
                {
                    Logging.Write("游戏已是最新版本.");
                    return false;
                }
            }
            else
            {
                Logging.Write("无法获取本地游戏版本.");
                return false;
            }
        }

        public static async Task<JsonDocument> GetGameInfo()
        {
            using (var response = await client.GetAsync(ApiUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonDocument.Parse(jsonResponse);
                }
                return null;
            }
        }

        public static async Task ExtractGameInfo()
        {
            JsonDocument gameData = await GetGameInfo();
            if (gameData != null)
            {
                JsonElement root = gameData.RootElement;
                JsonElement gamePackages = root.GetProperty("data").GetProperty("game_packages");

                foreach (JsonElement gamePackage in gamePackages.EnumerateArray())
                {
                    JsonElement game = gamePackage.GetProperty("game");
                    if (game.GetProperty("biz").GetString() == "hkrpg_cn")
                    {
                        JsonElement main = gamePackage.GetProperty("main");
                        JsonElement major = main.GetProperty("major");

                        GameUpdateModel.LatestVersion = major.GetProperty("version").GetString();
                        JsonElement gamePkg = major.GetProperty("game_pkgs")[0];
                        GameUpdateModel.LatestPath = gamePkg.GetProperty("url").GetString();
                        GameUpdateModel.LatestSize = gamePkg.GetProperty("size").GetString();
                        GameUpdateModel.LatestMD5 = gamePkg.GetProperty("md5").GetString();

                        JsonElement audioPkgs = major.GetProperty("audio_pkgs");
                        foreach (JsonElement audioPkg in audioPkgs.EnumerateArray())
                        {
                            if (audioPkg.GetProperty("language").GetString() == "zh-cn")
                            {
                                GameUpdateModel.LatestVoicePacksPath = audioPkg.GetProperty("url").GetString();
                                GameUpdateModel.LatestVoicePacksSize = audioPkg.GetProperty("size").GetString();
                                GameUpdateModel.LatestVoicePacksMD5 = audioPkg.GetProperty("md5").GetString();
                                break;
                            }
                        }

                        // 获取本地版本号
                        string localVersion = await GetLocalGameVersion();
                        JsonElement patches = main.GetProperty("patches");
                        foreach (JsonElement patch in patches.EnumerateArray())
                        {
                            if (patch.GetProperty("version").GetString() == localVersion)
                            {
                                JsonElement patchPkg = patch.GetProperty("game_pkgs")[0];
                                GameUpdateModel.DiffsPath = patchPkg.GetProperty("url").GetString();
                                GameUpdateModel.DiffsSize = patchPkg.GetProperty("size").GetString();
                                GameUpdateModel.DiffsMD5 = patchPkg.GetProperty("md5").GetString();

                                JsonElement patchAudioPkgs = patch.GetProperty("audio_pkgs");
                                foreach (JsonElement patchAudioPkg in patchAudioPkgs.EnumerateArray())
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
                    string configFilePath = Path.Combine(gameDirectory, "config.ini");

                    if (File.Exists(configFilePath))
                    {
                        string[] lines = await File.ReadAllLinesAsync(configFilePath);

                        foreach (string line in lines)
                        {
                            if (line.StartsWith("game_version="))
                            {
                                Match match = Regex.Match(line, @"\d+(\.\d+)+");
                                if (match.Success)
                                {
                                    Logging.Write($"StarRail Current Version: {match.Value}");
                                    return match.Value;
                                }
                            }
                        }
                    }
                    else
                    {
                        Logging.Write("config.ini 文件不存在.");
                    }
                }
                else
                {
                    Logging.Write("无法获取游戏目录.");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"获取游戏版本时出现错误: {ex.Message}");
            }

            return null;
        }

        private static async Task<string> GetGameDirectory()
        {
            return AppDataController.GetGamePathWithoutGameName();
        }

        public static async Task<bool> StartDownload(Action<double, string, string> progressChanged)
        {
            DownloadManager.isDownloading = true;
            DownloadManager.isPaused = false;
            string downloadUrl = GameUpdateModel.DiffsPath ?? GameUpdateModel.LatestPath;
            string filePath = AppDataController.GetGamePathWithoutGameName() + "game.zip";
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                long existingLength = 0;
                if (File.Exists(filePath))
                {
                    existingLength = new FileInfo(filePath).Length;
                }

                using (var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl))
                {
                    request.Headers.Range = new RangeHeaderValue(existingLength, null);
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationTokenSource.Token))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
                            {
                                return await DownloadFinishedAsync();
                            }
                            else
                            {
                                Logging.Write("下载失败，状态码：" + response.StatusCode);
                                return false;
                            }
                        }

                        DateTime downloadStartTime = DateTime.Now;
                        long initialDownloadedBytes = existingLength;
                        var totalBytes = (response.Content.Headers.ContentLength ?? 0) + existingLength;
                        var totalRead = existingLength;
                        var buffer = new byte[81920];
                        var isMoreToRead = true;

                        stopwatch.Start();
                        using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 81920, true))
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            DateTime lastUpdateTime = DateTime.Now;
                            do
                            {
                                var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationTokenSource.Token);
                                if (read == 0)
                                {
                                    isMoreToRead = false;
                                }
                                else
                                {
                                    await fileStream.WriteAsync(buffer, 0, read, cancellationTokenSource.Token);
                                    totalRead += read;

                                    var totalElapsedTime = (DateTime.Now - downloadStartTime).TotalSeconds;
                                    var speed = (totalRead - initialDownloadedBytes) / totalElapsedTime;
                                    var speedMb = speed / (1024 * 1024);
                                    var sizeMb = totalRead / (1024 * 1024);
                                    var totalSizeMb = totalBytes / (1024 * 1024);

                                    if ((DateTime.Now - lastUpdateTime).TotalSeconds >= 0.05)
                                    {
                                        var percentage = (double)totalRead / totalBytes * 100;
                                        progressChanged?.Invoke(percentage, $"{speedMb:F2}MB/s", $"{sizeMb:F2}MB/{totalSizeMb:F2}MB");
                                        DownloadManager.UpdateProgress(percentage, $"{speedMb:F2}MB/s", $"{sizeMb:F2}MB/{totalSizeMb:F2}MB");
                                        lastUpdateTime = DateTime.Now;
                                    }
                                }
                            } while (isMoreToRead);
                        }
                        stopwatch.Stop();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logging.Write("下载已暂停");
            }
            catch (Exception ex)
            {
                Logging.Write("下载异常：" + ex.Message);
            }
            return false;
        }

        public static void PauseDownload()
        {
            cancellationTokenSource?.Cancel();
            DownloadManager.isDownloading = true;
            DownloadManager.isPaused = true;
        }

        public static void ResumeDownload(Action<double, string, string> progressChanged)
        {
            StartDownload(progressChanged);
        }

        public static async Task<bool> DownloadFinishedAsync()
        {
            string zipPath = Path.Combine(AppDataController.GetGamePathWithoutGameName(), "game.zip");
            string extractPath = AppDataController.GetGamePathWithoutGameName();
            try
            {
                NotificationManager.RaiseNotification("下载完成", "", InfoBarSeverity.Success, true, 2);
                DownloadManager.isPaused = true;
                var progress = new Progress<int>(async percent =>
                {
                    WaitOverlayManager.RaiseWaitOverlay(true, "正在校验MD5", "请稍等", true, percent);
                    await Task.Delay(100);
                });

                bool md5Verified = await Task.Run(() => VerifyFileMD5Async(zipPath, GameUpdateModel.DiffsMD5 ?? GameUpdateModel.LatestMD5, progress));
                if (!md5Verified)
                {
                    throw new Exception("MD5 校验失败，文件可能已损坏。");
                }

                cancellationTokenSource?.Cancel();

                progress = new Progress<int>(async percent =>
                {
                    WaitOverlayManager.RaiseWaitOverlay(true, "正在解压", "请稍等", true, percent);
                    await Task.Delay(100);
                });

                await Task.Run(() => ExtractZipWithProgress(zipPath, extractPath, progress));

                WaitOverlayManager.RaiseWaitOverlay(false);
                NotificationManager.RaiseNotification("解压完成", "游戏文件已成功解压。", InfoBarSeverity.Success, true, 2);

                await UpdateConfigVersion(GameUpdateModel.LatestVersion);
                NotificationManager.RaiseNotification("更新完成", $"游戏已更新到{GameUpdateModel.LatestVersion}。", InfoBarSeverity.Success, true, 2);

                // 重置下载管理器的变量
                ResetDownloadManagerVariables();
                DownloadManager.isDownloading = false;
                
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

        private static void ExtractZipWithProgress(string zipPath, string extractPath, IProgress<int> progress)
        {
            using (var archive = ZipFile.OpenRead(zipPath))
            {
                int totalEntries = archive.Entries.Count;
                int processedEntries = 0;

                foreach (var entry in archive.Entries)
                {
                    string destinationPath = Path.Combine(extractPath, entry.FullName);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        entry.ExtractToFile(destinationPath, overwrite: true);
                    }

                    processedEntries++;
                    int percentComplete = (int)((double)processedEntries / totalEntries * 100);
                    progress.Report(percentComplete);
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
                    string configFilePath = Path.Combine(gameDirectory, "config.ini");

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
                        Logging.Write("Config版本号已更新.");
                    }
                    else
                    {
                        Logging.Write("Config文件不存在.");
                    }
                }
                else
                {
                    Logging.Write("无法获取游戏目录.");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"更新游戏版本时出现错误: {ex.Message}");
            }
        }

        private static async Task<bool> VerifyFileMD5Async(string filePath, string expectedMD5, IProgress<int> progress)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    long fileLength = stream.Length;
                    long totalRead = 0;
                    var buffer = new byte[81920];
                    int read;

                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        md5.TransformBlock(buffer, 0, read, buffer, 0);
                        totalRead += read;
                        int percentComplete = (int)((double)totalRead / fileLength * 100);
                        progress.Report(percentComplete);
                    }
                    md5.TransformFinalBlock(buffer, 0, 0);

                    var hash = md5.Hash;
                    var md5String = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    return md5String == expectedMD5;
                }
            }
        }

        private static void ResetDownloadManagerVariables()
        {
            DownloadManager.CurrentProgress = 0;
            DownloadManager.CurrentSpeed = string.Empty;
            DownloadManager.CurrentSize = string.Empty;
            DownloadManager.isDownloading = false;
            DownloadManager.isPaused = true;
            DownloadManager.isFinished = false;
        }
    }
}
