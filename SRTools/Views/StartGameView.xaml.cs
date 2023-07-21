using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using SRTools.Depend;
using Spectre.Console;
using Microsoft.Win32;
using Windows.ApplicationModel;
using SRTools.Views.NotifyViews;
using SRTools.Views.SGViews;
using Newtonsoft.Json;
using System.Net.Http;

namespace SRTools.Views
{
    public sealed partial class StartGameView : Page
    {
        private DispatcherQueueTimer dispatcherTimer_Launcher;
        private DispatcherQueueTimer dispatcherTimer_Game;
        private DispatcherQueueTimer dispatcherTimer_Graphics;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public StartGameView()
        {
            //Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            this.InitializeComponent();
            Logging.Write("Switch to StartGameView",0);
            // 获取UI线程的DispatcherQueue
            var dispatcherQueue_Launcher = DispatcherQueue.GetForCurrentThread();
            var dispatcherQueue_Game = DispatcherQueue.GetForCurrentThread();
            var dispatcherQueue_Graphics = DispatcherQueue.GetForCurrentThread();
            this.Unloaded += OnUnloaded;

            // 创建定时器，并设置回调函数和时间间隔
            dispatcherTimer_Launcher = dispatcherQueue_Launcher.CreateTimer();
            dispatcherTimer_Launcher.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer_Launcher.Tick += CheckProcess_Launcher;
            dispatcherTimer_Launcher.Start();
            dispatcherTimer_Game = dispatcherQueue_Game.CreateTimer();
            dispatcherTimer_Game.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer_Game.Tick += CheckProcess_Game;
            dispatcherTimer_Game.Start();
            dispatcherTimer_Graphics = dispatcherQueue_Graphics.CreateTimer();
            dispatcherTimer_Graphics.Interval = TimeSpan.FromSeconds(1);
            dispatcherTimer_Graphics.Tick += CheckProcess_Graphics;
            dispatcherTimer_Graphics.Start();
            GetPromptButton_Click(null, null);

            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueName = "GraphicsSettings_Model_h2986158309";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            byte[] valueBytes;
            try { valueBytes = (byte[])key.GetValue(valueName); } catch { valueBytes = null; }
            if (key == null || valueBytes == null)
            {
                infoBar.IsOpen = true;
                infoBar.Title = "未检测到当前画质的注册表";
                infoBar.Message = "有可能未手动设置过画质，请进入游戏手动设置一次画质后再试";
                AccountSelect.IsEnabled = true;
                GraphicSelect.IsEnabled = false;
                AccountSelect.IsSelected = true;
            }
            else
            {
                AccountSelect.IsEnabled = true;
                GraphicSelect.IsEnabled = true;
                GraphicSelect.IsSelected = true;
            }

            if (localSettings.Values.ContainsKey("Config_GamePath"))
            {
                var value = localSettings.Values["Config_GamePath"] as string;
                Logging.Write("GamePath: "+ value,0);
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
                UpdateUIElementsVisibility(0);
            }
        }
        private async void SelectGame(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            await AnsiConsole.Status().StartAsync("等待选择文件...", async ctx =>
            {
                var file = await picker.PickSingleFileAsync();
                if (file != null && file.Name == "StarRail.exe")
                {
                    localSettings.Values["Config_GamePath"] = @file.Path;
                    UpdateUIElementsVisibility(1);
                }
                else
                {
                    ValidGameFile.Subtitle = "选择正确的StarRail.exe\n通常位于[游戏根目录\\Game\\StarRail.exe]";
                    ValidGameFile.IsOpen = true;
                }
            });
        }

        public void RMGameLocation(object sender, RoutedEventArgs e)
        {
            localSettings.Values["Config_GamePath"] = @"Null";
            UpdateUIElementsVisibility(0);
        }
        //启动游戏
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            StartGame(null, null);
        }
        private void StartLauncher_Click(object sender, RoutedEventArgs e)
        {
            StartLauncher(null, null);
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
                startLauncher.IsEnabled = false;
                SGNav.Visibility = Visibility.Collapsed;
            }
            else
            {
                selectGame.IsEnabled = false;
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                rmGame.IsEnabled = true;
                startGame.IsEnabled = true;
                startLauncher.IsEnabled = true;
                
                SGNav.Visibility = Visibility.Visible;
            }
        }

        public void StartGame(TeachingTip sender, object args)
        {
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            var processInfo = new ProcessStartInfo(gamePath);
            
            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; 
            Process.Start(processInfo);
        }

        public void StartLauncher(TeachingTip sender, object args)
        {
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            var processInfo = new ProcessStartInfo(gamePath.Replace("StarRail.exe","..\\launcher.exe"));

            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas";
            Process.Start(processInfo);
        }

        // 定时器回调函数，检查进程是否正在运行
        private void CheckProcess_Game(DispatcherQueueTimer timer, object e)
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

        private void CheckProcess_Graphics(DispatcherQueueTimer timer, object e)
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueName = "GraphicsSettings_Model_h2986158309";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            byte[] valueBytes;
            try { valueBytes = (byte[])key.GetValue(valueName); } catch { valueBytes = null; }
            if (key == null || valueBytes == null)
            {
                infoBar.Title = "未检测到当前画质的注册表";
                infoBar.Message = "有可能未手动设置过画质，请进入游戏手动设置一次画质后再试";
                AccountSelect.IsEnabled = true;
                GraphicSelect.IsEnabled = false;
                AccountSelect.IsSelected = true;
            }
            else
            {
                infoBar.IsOpen = false;
                AccountSelect.IsEnabled = true;
                GraphicSelect.IsEnabled = true;
            }
        }

        private void CheckProcess_Launcher(DispatcherQueueTimer timer, object e)
        {
            if (Process.GetProcessesByName("launcher").Length > 0)
            {
                // 进程正在运行
                startLauncher.Visibility = Visibility.Collapsed;
                launcherRunning.Visibility = Visibility.Visible;
            }
            else
            {
                // 进程未运行
                startLauncher.Visibility = Visibility.Visible;
                launcherRunning.Visibility = Visibility.Collapsed;
            }
        }

        private void SGNavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 处理设置菜单项单击事件
            }
            else if (args.SelectedItemContainer != null)
            {
                switch (args.SelectedItemContainer.Tag.ToString())
                {
                    case "SG_Graphics":
                        // 导航到画质调节
                        SGFrame.Navigate(typeof(GraphicSettingView));
                        break;
                    case "SG_Account":
                        // 导航到画质调节
                        SGFrame.Navigate(typeof(AccountView));
                        break;
                }
            }
        }

        private async void GetPromptButton_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client = new HttpClient();
            string url = "https://srtools.jamsg.cn/SRTools_Prompt";
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonData = await response.Content.ReadAsStringAsync();
                prompt.Text = jsonData;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer_Graphics.Stop();
            dispatcherTimer_Game.Stop();
            dispatcherTimer_Launcher.Stop();
            Logging.Write("Timer Stopped", 0);
        }

    }
}
