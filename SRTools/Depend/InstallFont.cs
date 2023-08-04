using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace SRTools.Depend
{
    class InstallFont
    {

        // Import the AddFontResource function from gdi32.dll
        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        public static extern int AddFontResource([In] [MarshalAs(UnmanagedType.LPWStr)]
                                     string lpFileName);

        // Import the SendMessage function from user32.dll
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, int Msg, IntPtr wParam,
            IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        public static async Task<int> SegoeFluentFontAsync()
        {
            GetNetData getNetData;
            string UpdateFileFolder = "\\JSG-LLC\\Fonts\\";
            string UpdateFileName = "SegoeFluentIcons.zip";
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string localFilePath = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, UpdateFileName);
            var progress = new Progress<double>();
            getNetData = new GetNetData();
            try
            {
                await getNetData.DownloadFileWithProgressAsync("https://aka.ms/SegoeFluentIcons", localFilePath, progress);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            string fontZipFile = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, UpdateFileName);
            string tempFolder = Path.Combine(userDocumentsFolderPath + UpdateFileFolder, "temp");
            // 创建临时文件夹
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            Directory.Delete(tempFolder, true);
            // 解压 zip 文件到临时文件夹中
            ZipFile.ExtractToDirectory(fontZipFile, tempFolder);

            // 获取字体文件路径
            StorageFile fontFile = await StorageFile.GetFileFromPathAsync(tempFolder + "\\Segoe Fluent Icons.ttf");

            if (fontFile != null)
            {
                // 启动 Windows 默认的字体查看器
                Process.Start("fontview", fontFile.Path);

                // 字体已成功安装
                return 0;
            }
            else { return 1; }
        }
    }
}
