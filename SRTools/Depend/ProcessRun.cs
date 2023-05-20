using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    class ProcessRun
    {
        public static String RunProcess_Message(string exePath, string arguments)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                using (StreamReader outputReader = process.StandardOutput)
                using (StreamReader errorReader = process.StandardError)
                {
                    string output = outputReader.ReadToEnd();
                    string error = errorReader.ReadToEnd();

                    if (!string.IsNullOrEmpty(output))
                    {
                        return output;
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        return error;
                    }

                    return null;
                }

                process.WaitForExit();
            }
        }
    }
}
