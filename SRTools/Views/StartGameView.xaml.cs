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
using Windows.ApplicationModel;
using Windows.Foundation.Metadata;
using SRTools.Depend;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartGameView : Page
    {
        private Timer timer;
        private DispatcherQueueTimer dispatcherTimer;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public StartGameView()
        {
            this.InitializeComponent();
            // 获取UI线程的DispatcherQueue
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // 创建定时器，并设置回调函数和时间间隔
            dispatcherTimer = dispatcherQueue.CreateTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer.Tick += CheckProcess;
            dispatcherTimer.Start();
            if (localSettings.Values.ContainsKey("SRTools_Config_GamePath"))
            {
                var value = localSettings.Values["SRTools_Config_GamePath"] as string;
                if (!string.IsNullOrEmpty(value) && value.Contains("Null"))
                {
                    UpdateUIElementsVisibility(0);
                }
                else
                {
                    UpdateUIElementsVisibility(1);
                }
            }
            else
            {
                UpdateUIElementsVisibility(1);
            }

            if (localSettings.Values.ContainsKey("SRTools_Config_UnlockFPS"))
            {
                var value = localSettings.Values["SRTools_Config_UnlockFPS"] as string;
                if (value == "1")
                {
                    unlockFPS.IsChecked = true;
                }
                else if (value == "0")
                {
                    unlockFPS.IsChecked = false;
                }
            }
            else
            {
                unlockFPS.IsChecked = false;
            }


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
                localSettings.Values["SRTools_Config_GamePath"] = @file.Path;
                UpdateUIElementsVisibility(1);
            }
            else
            {
                ValidGameFile.Subtitle = "选择正确的StarRail.exe\n通常位于[游戏根目录\\Game\\StarRail.exe]";
                ValidGameFile.IsOpen = true;
            }
        }

        private async void RMGameLocation(object sender, RoutedEventArgs e)
        {
            localSettings.Values["SRTools_Config_GamePath"] = @"Null";
            UpdateUIElementsVisibility(0);
        }
        //启动游戏
        private async void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (unlockFPS.IsChecked ?? false)
            {
                ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe","/FPS 120");
                StartGame(null, null);
            }
            else 
            {
                ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/FPS 60");
                StartGame(null, null);
            }
        }

        private void UnlockFPS_Click(object sender, RoutedEventArgs e) 
        {
            if (unlockFPS.IsChecked ?? false)
            {
                localSettings.Values["SRTools_Config_UnlockFPS"] = "1";
            }
            else
            {
                localSettings.Values["SRTools_Config_UnlockFPS"] = "0";
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
            }
            else
            {
                selectGame.IsEnabled = false;
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                rmGame.IsEnabled = true;
                startGame.IsEnabled = true;
            }
        }

        private void StartGame(TeachingTip sender, object args)
        {
            string gamePath = localSettings.Values["SRTools_Config_GamePath"] as string;
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
