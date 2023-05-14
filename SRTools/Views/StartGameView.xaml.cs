using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartGameView : Page
    {
        public StartGameView()
        {
            this.InitializeComponent();
            BitmapImage image = new BitmapImage(new Uri("https://beta.jamsg.cn/starrail.png"));
            image.ImageOpened += (sender, e) =>
            {
                backgroundBrush.ImageSource = image;
            };
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("GameLocation.ini").AsTask().GetAwaiter().GetResult();
            var filePath = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            if (filePath.Contains("StarRail.exe"))
            {
                UpdateUIElementsVisibility(1);
            }
            else
            {
                UpdateUIElementsVisibility(0);
            }
            
        }
        //选择游戏
        private async void SelectGame(object sender, RoutedEventArgs e)
        {
            var window = new Microsoft.UI.Xaml.Window();
            // 创建文件选择器
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // 显示文件选择器并等待用户选择文件
            var file = await picker.PickSingleFileAsync();
            if (file != null && file.Name == "StarRail.exe")
            {
                // 将文件路径保存到 settings.ini 文件中
                var folder = KnownFolders.DocumentsLibrary;
                var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);
                var settingsFile = await srtoolsFolder.CreateFileAsync("GameLocation.ini", CreationCollisionOption.OpenIfExists);
                await FileIO.WriteTextAsync(settingsFile, file.Path);
                UpdateUIElementsVisibility(1);
            }
        }
        //清除路径 
        private async void RMGameLocation(object sender, RoutedEventArgs e)
        {
            // 将文件路径保存到 GameLocation.ini 文件中
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);
            var settingsFile = await srtoolsFolder.CreateFileAsync("GameLocation.ini", CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(settingsFile, "");
            UpdateUIElementsVisibility(0);
        }
        //启动游戏
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            //修改注册表
            // 指定注册表键路径和值名称
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueName = "GraphicsSettings_Model_h2986158309";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            byte[] valueBytes = (byte[])key.GetValue(valueName);
            if (valueBytes != null) 
            {
                string valueString = Encoding.UTF8.GetString(valueBytes);
                if (valueString.Contains("FPS"))
                {
                    // 修改值
                    string newValueString = valueString.Replace("{\"FPS\":60", "{\"FPS\":120");
                    // 将新的字符串值转换为二进制
                    byte[] newValueBytes = Encoding.UTF8.GetBytes(newValueString);
                    // 写回注册表键
                    key.SetValue(valueName, newValueBytes, RegistryValueKind.Binary);
                    // 关闭注册表键
                    key.Close();
                    StartGame(null, null);
                }
            }
            else
            {
                NoGraphicsTip.IsOpen = true;
            }
        }

        private void UpdateUIElementsVisibility(int status)
        {
            if (status == 0) 
            {
                selectGame.Visibility = Visibility.Visible;
                rmGame.Visibility = Visibility.Collapsed;
                startGame.Visibility = Visibility.Collapsed;
            }
            else
            {
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                startGame.Visibility = Visibility.Visible;
            }
        }

        private void StartGame(TeachingTip sender, object args)
        {
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("GameLocation.ini").AsTask().GetAwaiter().GetResult();
            var filePath = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            var processInfo = new ProcessStartInfo(filePath);
            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; // this will prompt the user for admin privileges
            Process.Start(processInfo);
        }

    }
}
