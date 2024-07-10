﻿// Copyright (c) 2021-2024, JamXi JSG-LLC.
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

using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    public class InstallerHelper
    {
        private static readonly string BaseInstallerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "Installer");
        private static readonly string InstallerFileName = "SRToolsInstaller.exe";
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
                Arguments = args,
                UseShellExecute = true, // 使用Shell以提升权限
                Verb = "runas" // 请求提升权限
            };

            try
            {
                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();

                    // 检查退出代码
                    if (process.ExitCode != 0)
                    {
                        Logging.Write($"安装程序退出代码: {process.ExitCode}", 2);
                    }

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
