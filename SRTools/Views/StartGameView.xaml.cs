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
using System.Threading;
using Microsoft.UI.Dispatching;
using Windows.Security.EnterpriseData;
using Windows.Security.Authorization.AppCapabilityAccess;
using SRTools.Depend;
using System.Windows.Input;

namespace SRTools.Views
{

    public sealed partial class StartGameView : Page
    {
        private DispatcherQueueTimer dispatcherTimer;
        private SReg sReg;
        public StartGameView()
        {
            Console.WriteLine("Toggle to StartGameView");
            this.InitializeComponent();
            // 获取UI线程的DispatcherQueue
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // 创建定时器，并设置回调函数和时间间隔
            dispatcherTimer = dispatcherQueue.CreateTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer.Tick += CheckProcess;
            dispatcherTimer.Start();
            sReg = new SReg();
            String GamePathValue = sReg.ReadReg("SRTools_Config_GamePath");
            String UnlockFPSValue = sReg.ReadReg("SRTools_Config_UnlockFPS");
            if (string.IsNullOrEmpty(GamePathValue) && GamePathValue.Contains("Null"))
                UpdateUIElementsVisibility(0);
            else 
                UpdateUIElementsVisibility(1);
            if (UnlockFPSValue == "1") unlockFPS.IsChecked = true;

            else unlockFPS.IsChecked = false;
        }
        private async void SelectGame(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file != null && file.Name == "StarRail.exe")
            {
                string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
                string valueGamePath = "SRTools_Config_GamePath";
                string folderPath = @file.Path;
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))
                {
                    key.SetValue(valueGamePath, folderPath, RegistryValueKind.String);
                }
                WrongGameFile.IsOpen = false;
                UpdateUIElementsVisibility(1);
            }
            else
            {
                WrongGameFile.IsOpen = true;
            }
        }

        private void RMGameLocation(object sender, RoutedEventArgs e)
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueGamePath = "SRTools_Config_GamePath";
            string folderPath = @"Null";
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))
            {
                key.SetValue(valueGamePath, folderPath, RegistryValueKind.String);
            }
            UpdateUIElementsVisibility(0);
        }
        //启动游戏
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (unlockFPS.IsChecked ?? false)
            {
                string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
                string valueName = "GraphicsSettings_Model_h2986158309";
                RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                byte[] valueBytes = (byte[])key.GetValue(valueName);
                if (key == null || valueBytes == null)
                {
                    NoGraphicsTip.IsOpen = true;
                    return;
                }
                string valueString = Encoding.UTF8.GetString(valueBytes);
                if (!valueString.Contains("\"FPS\":60"))
                {
                    key.Close();
                    StartGame(null, null);
                    return;
                }
                string newValueString = valueString.Replace("{\"FPS\":60", "{\"FPS\":120");
                byte[] newValueBytes = Encoding.UTF8.GetBytes(newValueString);
                key.SetValue(valueName, newValueBytes, RegistryValueKind.Binary);
                key.Close();
                StartGame(null, null);
            }
            else 
            {
                StartGame(null, null);
            }
        }

        private void UnlockFPS_Click(object sender, RoutedEventArgs e) 
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueUnlockFPS = "SRTools_Config_UnlockFPS";
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            RegistryKey key = baseKey.OpenSubKey(keyPath, true);
            if (unlockFPS.IsChecked ?? false) 
            {
                key.SetValue(valueUnlockFPS, 1, RegistryValueKind.String);
            }
            else
            {
                key.SetValue(valueUnlockFPS, 0, RegistryValueKind.String);
            }
        }

        private void UpdateUIElementsVisibility(int status)
        {
            if (status == 0) 
            {
                selectGame.IsEnabled = true;
                selectGame.Visibility = Visibility.Visible;
                rmGame.Visibility = Visibility.Collapsed;
                rmGame.IsEnabled = false;
                startGame.IsEnabled = false;
                unlockFPS.IsEnabled = false;
            }
            else
            {
                selectGame.IsEnabled = false;
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                rmGame.IsEnabled = true;
                startGame.IsEnabled = true;
                unlockFPS.IsEnabled = true;
            }
        }

        private void StartGame(TeachingTip sender, object args)
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueGamePath = "SRTools_Config_GamePath";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath);
            string gamePath = (string)key.GetValue(valueGamePath);
            key.Close();
            var processInfo = new ProcessStartInfo(gamePath);
            
            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; 
            Process.Start(processInfo);
        }

        // 定时器回调函数，检查进程是否正在运行
        private void CheckProcess(DispatcherQueueTimer timer, object e)
        {
            if (Process.GetProcessesByName("StarRail").Length > 0)
            {
                // 进程正在运行
                startGame.Visibility = Visibility.Collapsed;
                gameRunning.Visibility = Visibility.Visible;
            }
            else
            {
                // 进程未运行
                startGame.Visibility = Visibility.Visible;
                gameRunning.Visibility = Visibility.Collapsed;
            }
        }

        // 在窗口关闭时停止定时器
        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= CheckProcess;
        }


    }
}
