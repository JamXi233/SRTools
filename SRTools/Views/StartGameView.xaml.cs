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


using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage.Pickers;
using System.Diagnostics;
using Microsoft.UI.Dispatching;
using SRTools.Depend;
using Spectre.Console;
using Microsoft.Win32;
using SRTools.Views.SGViews;
using System.Net.Http;
using System.Threading.Tasks;
using static SRTools.App;
using Windows.Foundation;
using SRTools.Views.GachaViews;

namespace SRTools.Views
{
    public sealed partial class StartGameView : Page
    {
        private DispatcherQueue dispatcherQueue;
        private DispatcherQueueTimer dispatcherTimer_Launcher;
        private DispatcherQueueTimer dispatcherTimer_Game;
        private DispatcherQueueTimer dispatcherTimer_Graphics;

        public StartGameView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to StartGameView",0);
            this.InitializeComponent();
            this.Loaded += StartGameView_Loaded;
            this.Unloaded += OnUnloaded;

            // 获取UI线程的DispatcherQueue
            InitializeDispatcherQueue();
            // 初始化并启动定时器
            InitializeTimers();

            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueName = "GraphicsSettings_Model_h2986158309";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            byte[] valueBytes;
            try { valueBytes = (byte[])key.GetValue(valueName); } catch { valueBytes = null; }
            if (key == null || valueBytes == null)
            {
                NotificationManager.RaiseNotification("未检测到当前画质的注册表", "有可能未手动设置过画质\n请进入游戏手动设置一次画质后再试", InfoBarSeverity.Warning);
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

            if (AppDataController.GetGamePath() != null)
            {
                string GamePath = AppDataController.GetGamePath();
                Logging.Write("GamePath: "+ GamePath,0);
                if (!string.IsNullOrEmpty(GamePath) && GamePath.Contains("Null"))
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

        private void InitializeDispatcherQueue()
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        private void InitializeTimers()
        {
            // 创建并配置Launcher检查定时器
            dispatcherTimer_Launcher = CreateTimer(TimeSpan.FromSeconds(2), CheckProcess_Launcher);

            // 创建并配置Game检查定时器
            dispatcherTimer_Game = CreateTimer(TimeSpan.FromSeconds(2), CheckProcess_Game);

            // 创建并配置Graphics检查定时器
            dispatcherTimer_Graphics = CreateTimer(TimeSpan.FromSeconds(1), CheckProcess_Graphics);
        }

        private DispatcherQueueTimer CreateTimer(TimeSpan interval, TypedEventHandler<DispatcherQueueTimer, object> tickHandler)
        {
            var timer = dispatcherQueue.CreateTimer();
            timer.Interval = interval;
            timer.Tick += tickHandler;
            timer.Start();
            return timer;
        }

        private async void StartGameView_Loaded(object sender, RoutedEventArgs e)
        {
            // 异步调用 GetPromptAsync
            await GetPromptAsync();
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
                    //更新为新的存储管理机制
                    AppDataController.SetGamePath(@file.Path);
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
            AppDataController.RMGamePath();
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
            GameStartUtil gameStartUtil = new GameStartUtil();
            gameStartUtil.StartGame();
        }

        public void StartLauncher(TeachingTip sender, object args)
        {
            GameStartUtil gameStartUtil = new GameStartUtil();
            gameStartUtil.StartLauncher();
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
                AccountSelect.IsEnabled = true;
                GraphicSelect.IsEnabled = false;
                AccountSelect.IsSelected = true;
            }
            else
            {
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

        private void SGNavView_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            switch (currentSelectedIndex)
            {
                case 0:
                    SGFrame.Navigate(typeof(GraphicSettingView));
                    break;
                case 1:
                    SGFrame.Navigate(typeof(AccountView));
                    break;
            }
        }

        private async Task GetPromptAsync()
        {
            try
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
            catch (Exception ex)
            {
                // 处理异常，例如记录错误或显示错误消息
                Logging.Write($"Error fetching prompt: {ex.Message}", 2);
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
