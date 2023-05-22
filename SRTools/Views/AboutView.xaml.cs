using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    public sealed partial class AboutView : Page
    {
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        public AboutView()
        {
            InitializeComponent();
        }

        private void Console_Toggle(object sender, RoutedEventArgs e)
        {
            // 判断是否需要打开控制台
            if (consoleToggle.IsChecked ?? false)
            {
                // 调用 AllocConsole 函数以打开控制台
                AllocConsole();
            }
            else
            {
                // 调用 FreeConsole 函数以关闭控制台
                FreeConsole();
            }
        }

        private void Clear_AllData(object sender, RoutedEventArgs e)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\");
        }

        private void DeleteFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                try { Directory.Delete(folderPath, true); ClearLocalDataAsync(); }
                catch (IOException ex) { ClearLocalDataAsync(); }
            }
            else
            {
                ClearLocalDataAsync();
            }
        }

        public async Task ClearLocalDataAsync()
        {
            // 获取 LocalData 文件夹的引用
            var localFolder = ApplicationData.Current.LocalFolder;

            // 删除 LocalData 文件夹中的所有子文件夹和文件
            await DeleteFilesAndSubfoldersAsync(localFolder);

            // 需要重新创建删除的 LocalData 文件夹
            await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);
        }

        private async Task DeleteFilesAndSubfoldersAsync(StorageFolder folder)
        {
            // 获取文件夹中的所有文件和子文件夹
            var items = await folder.GetItemsAsync();

            // 遍历所有项目
            foreach (var item in items)
            {
                // 如果项目是文件，则删除它
                if (item is StorageFile file)
                {
                    await file.DeleteAsync();
                }
                // 如果项目是文件夹，则递归删除其中所有文件和子文件夹
                else if (item is StorageFolder subfolder)
                {
                    await DeleteFilesAndSubfoldersAsync(subfolder);

                    // 删除子文件夹本身
                    await subfolder.DeleteAsync();
                }
            }
        }

    }
}