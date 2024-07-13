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
using System.Threading.Tasks;
using SRTools.Depend;
using Microsoft.UI.Xaml;
using System;
using Windows.Storage.Pickers;
using System.IO;
using System.IO.Compression;
using Microsoft.UI.Xaml.Media;

namespace SRTools.Views.FirstRunViews
{
    public sealed partial class FirstRunInit : Page
    {
        public FirstRunInit()
        {
            this.InitializeComponent();
            Logging.Write("Switch to FirstRunInit", 0);
            AppDataController.SetFirstRunStatus(1);
        }

        private void NextPage(object sender, RoutedEventArgs e)
        {
            Frame parentFrame = GetParentFrame(this);
            if (parentFrame != null)
            {
                // 前往下载依赖页面
                parentFrame.Navigate(typeof(FirstRunTheme));
            }
        }

        private async void Restore_Data(object sender, RoutedEventArgs e)
        {
            string filePath = await CommonHelpers.FileHelpers.OpenFile(".SRToolsBackup");

            if (filePath != null)
            {
                string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\", "0");
                Task.Run(() => ZipFile.ExtractToDirectory(filePath, userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\")).Wait();
                Frame parentFrame = GetParentFrame(this);
                if (parentFrame != null)
                {
                    // 前往下载依赖页面
                    parentFrame.Navigate(typeof(FirstRunTheme));
                }
            }
        }

        private void DeleteFolder(string folderPath, String Close)
        {
            if (Directory.Exists(folderPath))
            {
                try { Directory.Delete(folderPath, true); }
                catch (IOException) { }
            }
        }

        private async Task DeleteFilesAndSubfoldersAsync(string directoryPath, string close)
        {
            // 检查目录是否存在
            if (Directory.Exists(directoryPath))
            {
                // 获取所有文件和递归删除所有子目录
                var subDirectories = Directory.GetDirectories(directoryPath);
                foreach (var dir in subDirectories)
                {
                    await DeleteFilesAndSubfoldersAsync(dir, close);
                    Directory.Delete(dir);
                }

                // 删除所有文件
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                // 删除当前目录
                Directory.Delete(directoryPath, true); // true 表示即使目录非空也强制删除
            }

            // 根据传入的参数决定是否关闭应用
            if (close == "1")
            {
                Application.Current.Exit();
            }
        }


        private Frame GetParentFrame(FrameworkElement child)
        {

            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is Frame))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as Frame;
        }

    }

}