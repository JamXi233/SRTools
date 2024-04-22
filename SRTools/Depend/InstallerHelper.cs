using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    public class InstallerHelper
    {
        private static readonly string BaseInstallerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "Installer");
        private static readonly string InstallerFileName = "SRToolsInstaller_1.0.0.0.exe";
        private static readonly string InstallerFullPath = Path.Combine(BaseInstallerPath, InstallerFileName);
        private static readonly string InstallerInfoUrl = "https://api.jamsg.cn/release/getversion?package=cn.jamsg.srtoolsinstaller";

        public static bool CheckInstaller()
        {
            // 检查 SRToolsInstaller 是否存在
            return File.Exists(InstallerFullPath);
        }

        // 检查 Installer 是否存在
        public static async Task GetInstaller()
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    // 从 API 获取安装程序信息
                    string json = await httpClient.GetStringAsync(InstallerInfoUrl);
                    dynamic installerInfo = JsonConvert.DeserializeObject(json);
                    string downloadLink = installerInfo.link;

                    // 确保安装程序目录存在
                    if (!Directory.Exists(BaseInstallerPath))
                    {
                        Directory.CreateDirectory(BaseInstallerPath);
                    }

                    // 下载安装程序
                    using (var response = await httpClient.GetAsync(downloadLink))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            using (var fs = new FileStream(InstallerFullPath, FileMode.Create))
                            {
                                await response.Content.CopyToAsync(fs);
                            }
                        }
                        else
                        {
                            throw new Exception("无法下载安装程序");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write($"下载安装程序时出错: {ex.Message}",3);
                }
            }
        }

        // 开始运行 Installer
        public static int RunInstaller(string args = "")
        {
            if (!File.Exists(InstallerFullPath))
            {
                Logging.Write("安装程序不存在，请先下载。", 1);
                return -1;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = InstallerFullPath,
                Arguments = args
            };

            if (args.Contains("depend"))
            {
                // 需要管理员权限
                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";
            }
            else
            {
                // 不需要管理员权限，尝试重定向输出
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
            }

            try
            {
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    if (!startInfo.UseShellExecute)
                    {
                        // 读取输出
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        Logging.Write($"Output: {output}", 0);
                        Logging.Write($"Error: {error}", 2);
                    }

                    process.WaitForExit();
                    return process.ExitCode;
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"运行安装程序时出错: {ex.Message}", 2);
                return -2;
            }
        }


    }
}
