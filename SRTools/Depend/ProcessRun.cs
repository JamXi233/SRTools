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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static SRTools.App;

namespace SRTools.Depend
{
    class ProcessRun
    {
        public static async Task<string> SRToolsHelperAsync(string args)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (Process process = new Process())
                    {
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardError = true; // 捕获标准错误输出
                        process.StartInfo.FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"JSG-LLC\SRTools\Depends\SRToolsHelper\SRToolsHelper.exe");
                        process.StartInfo.Arguments = args;

                        Logging.Write($"Starting process: {process.StartInfo.FileName} with arguments: {args}", 0, "SRToolsHelper");

                        process.Start();


                        // 同时读取标准输出和标准错误
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        process.WaitForExit();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Logging.Write($"Error: {error}", 3, "SRToolsHelper");
                        }

                        Logging.Write(output.Trim(), 3, "SRToolsHelper");
                        return output.Trim();
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write($"Exception in SRToolsHelperAsync: {ex.Message}", 3, "SRToolsHelper");
                    throw;
                }
            });
        }

        public static void StopSRToolsHelperProcess()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("SRToolsHelper"))
                {
                    process.Kill();
                }
                NotificationManager.RaiseNotification("SRToolsHelper", "已停止依赖运行", InfoBarSeverity.Warning, true, 3);
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("错误", "停止SRToolsHelper失败" + ex.ToString(), InfoBarSeverity.Error, true, 3);
            }
        }

        public static void StopSRProcess()
        {
            foreach (var process in Process.GetProcessesByName("Star Rail"))
            {
                process.Kill();
            }
        }

        public async static Task RestartApp()
        {
            Logging.Write("Restart SRTools Requested", 2);
            var processId = Process.GetCurrentProcess().Id;
            var fileName = Process.GetCurrentProcess().MainModule.FileName;
            ProcessStartInfo info = new ProcessStartInfo(fileName)
            {
                UseShellExecute = true,
            };
            Process.Start(info);
            await Task.Delay(100);
            Process.GetProcessById(processId).Kill();
        }
    }
}
