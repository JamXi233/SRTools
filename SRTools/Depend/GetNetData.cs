using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    class GetNetData
    {
        public async Task<bool> DownloadFileWithProgressAsync(string fileUrl, string localFilePath, IProgress<double> progress)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(localFilePath);
                Directory.CreateDirectory(directoryPath);
                using (var httpClient = new HttpClient())
                {
                    using (var response = await httpClient.GetAsync(fileUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            long totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();

                            byte[] buffer = new byte[8192];
                            int bytesRead;
                            long bytesDownloaded = 0;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);

                                bytesDownloaded += bytesRead;
                                double progressPercentage = (double)bytesDownloaded / totalBytes * 100;
                                progress.Report(progressPercentage);
                                
                            }
                        }
                    }
                }
                return true; // 下载成功
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false; // 下载失败
            }
        }
    }
}
