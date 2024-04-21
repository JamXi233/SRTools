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

namespace SRTools.Depend
{
    class ProcessRun
    {
        public static async Task<string> SRToolsHelperAsync(string args)
        {
            return await Task.Run(() =>
            {
                using (Process process = new Process())
                {
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\JSG-LLC\SRTools\Depends\SRToolsHelper\SRToolsHelper.exe";
                    process.StartInfo.Arguments = args;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    Logging.Write(output.Trim(), 3, "SRToolsHelper");
                    return output.Trim();
                }
            });
        }

        public async static Task RestartApp()
        {
            // 获取当前应用程序的进程 ID 和文件路径
            var processId = Process.GetCurrentProcess().Id;
            var fileName = Process.GetCurrentProcess().MainModule.FileName;

            // 启动一个新的应用程序实例
            ProcessStartInfo info = new ProcessStartInfo(fileName)
            {
                UseShellExecute = true,
            };
            Process.Start(info);

            // 给新的实例一些时间来启动
            await Task.Delay(100); // 延迟时间可能需要根据应用程序的启动时间来调整

            // 关闭当前应用程序实例
            Process.GetProcessById(processId).Kill();
        }

    }
}
