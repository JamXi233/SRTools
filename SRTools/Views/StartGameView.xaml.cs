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
using SRTools.Views.SGViews;
using System.Net.Http;
using System.Threading.Tasks;
using static SRTools.App;
using Windows.Foundation;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace SRTools.Views
{
    public sealed partial class StartGameView : Page
    {
        public static string GameRegion = null;
        private DispatcherQueue dispatcherQueue;
        private DispatcherQueueTimer dispatcherTimer_Game;
        public static string GS = null;
        public static bool isUserTap = true;
        bool isGameUpdateRequire = false;

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        public StartGameView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to StartGameView", 0);
            this.Loaded += StartGameView_Loaded;
            this.Unloaded += OnUnloaded;

            GameUpdate.OnLoadData += LoadDataAsync;

            InitializeDispatcherQueue();
            InitializeTimers();
        }

        private void InitializeDispatcherQueue()
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }

        private void InitializeTimers()
        {
            DownloadHelpers.DownloadProgressChanged += UpdateProgressChanged;
            dispatcherTimer_Game = CreateTimer(TimeSpan.FromSeconds(0.2), CheckProcess_Game);
            dispatcherTimer_Game.Start();
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
            await LoadDataAsync();
            await GetPromptAsync();
        }

        public async Task LoadDataAsync(string mode = null)
        {
            if (AppDataController.GetGamePath() != null)
            {
                string GamePath = AppDataController.GetGamePath();
                Logging.Write("GamePath: " + GamePath, 0);
                if (!string.IsNullOrEmpty(GamePath) && GamePath.Contains("Null"))
                {
                    UpdateUIElementsVisibility(0);
                    serverSelect.Visibility = Visibility.Collapsed;
                }
                else
                {
                    await Depend.Region.GetRegion(false);
                    UpdateUIElementsVisibility(1);
                    if (mode == null)
                    {
                        InitializeComboBoxSelection();
                        CheckProcess_Account();
                        CheckProcess_Graphics();
                    }
                    else if (mode == "Graphics") CheckProcess_Graphics();
                    else if (mode == "Account") CheckProcess_Account();
                    // 检查游戏更新
                    if (GameRegion == "CN" || GameRegion == "Bili") 
                    {
                        serverSelect.Visibility = Visibility.Visible;
                        await GameUpdate.ExtractGameInfo();
                        isGameUpdateRequire = await GameUpdate.CheckAndUpdateGame();
                        UpdateUI();
                        if (GameRegion == "Bili")
                        {
                            Frame_AccountView_Disable.Visibility = Visibility.Visible;
                            Frame_AccountView_Disable_Title.Text = "B服暂不支持账号切换";
                            Frame_AccountView_Disable_Subtitle.Text = "将于之后的版本推出";
                        }
                        else Frame_AccountView_Disable.Visibility = Visibility.Collapsed;
                    }
                    
                }
            }
            else
            {
                UpdateUIElementsVisibility(0);
            }
        }

        private void InitializeComboBoxSelection()
        {
            isUserTap = false;
            string gamePath = AppDataController.GetGamePathWithoutGameName();
            string configFilePath = Path.Combine(gamePath, "config.ini");

            if (File.Exists(configFilePath))
            {
                string[] configLines = File.ReadAllLines(configFilePath);
                string channel = null;
                string cps = null;
                string subChannel = null;

                foreach (var line in configLines)
                {
                    if (line.StartsWith("channel="))
                    {
                        channel = line.Split('=')[1];
                    }
                    else if (line.StartsWith("cps="))
                    {
                        cps = line.Split('=')[1];
                    }
                    else if (line.StartsWith("sub_channel="))
                    {
                        subChannel = line.Split('=')[1];
                    }
                }

                if (channel == "1" && cps == "gw_PC" && subChannel == "1")
                {
                    serverSelect.SelectedIndex = 0; // 官服
                }
                else if (channel == "14" && cps == "bilibili_PC" && subChannel == "0")
                {
                    serverSelect.SelectedIndex = 1; // B服
                }
            }
            isUserTap = true;
        }

        private void ServerSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;
            if (!isUserTap) return;
            if (serverSelect.SelectedItem is ComboBoxItem selectedItem)
            {
                string extrasPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "Extras", "ServerChange");
                string dllFilePath = Path.Combine(extrasPath, "PCGameSDK.dll");
                string sdkFilePath = Path.Combine(extrasPath, "sdk_pkg_version");
                if (isShiftPressed) DialogManager.RaiseDialog(XamlRoot, "遇到问题了？", "重新额外的文件", true, "下载", async () => await DownloadHelpers.DownloadSDK(extrasPath));
                if (File.Exists(dllFilePath))
                {
                    string gamePath = AppDataController.GetGamePathWithoutGameName();
                    string configFilePath = Path.Combine(gamePath, "config.ini");
                    string[] configLines = File.ReadAllLines(configFilePath);

                    if (selectedItem.Content.ToString() == "官服")
                    {
                        UpdateConfig(configLines, "1", "gw_PC", "1");
                        NotificationManager.RaiseNotification("切换服务器完成", "已切换到官服", InfoBarSeverity.Success, false, 1);
                    }
                    else if (selectedItem.Content.ToString() == "B服")
                    {
                        UpdateConfig(configLines, "14", "bilibili_PC", "0");
                        CopyDllToPluginsFolder(dllFilePath, gamePath);
                        CopySDKToGameFolder(sdkFilePath, gamePath);
                        NotificationManager.RaiseNotification("切换服务器完成", "已切换到B服", InfoBarSeverity.Success, false, 1);
                    }

                    File.WriteAllLines(configFilePath, configLines);
                    LoadDataAsync();
                }
                else
                {
                    ReloadFrame(null,null);
                    DialogManager.RaiseDialog(XamlRoot,"未找到Extra文件","您需要下载额外的文件来使用服务器切换",true,"下载", async () => await DownloadHelpers.DownloadSDK(extrasPath));
                }
            }
        }

        private void UpdateConfig(string[] configLines, string channel, string cps, string subChannel)
        {
            for (int i = 0; i < configLines.Length; i++)
            {
                if (configLines[i].StartsWith("channel="))
                {
                    configLines[i] = $"channel={channel}";
                }
                else if (configLines[i].StartsWith("cps="))
                {
                    configLines[i] = $"cps={cps}";
                }
                else if (configLines[i].StartsWith("sub_channel="))
                {
                    configLines[i] = $"sub_channel={subChannel}";
                }
            }
        }

        private void CopyDllToPluginsFolder(string sourceDllPath, string gamePath)
        {
            string pluginsPath = Path.Combine(gamePath, "StarRail_Data", "Plugins");
            Directory.CreateDirectory(pluginsPath);
            string destinationDllPath = Path.Combine(pluginsPath, "PCGameSDK.dll");

            File.Copy(sourceDllPath, destinationDllPath, true);
        }

        private void CopySDKToGameFolder(string sourceDllPath, string gamePath)
        {
            string pluginsPath = Path.Combine(gamePath);
            Directory.CreateDirectory(pluginsPath);
            string destinationDllPath = Path.Combine(pluginsPath, "sdk_pkg_version");

            File.Copy(sourceDllPath, destinationDllPath, true);
        }

        private async void SelectGame(object sender, RoutedEventArgs e)
        {
            await AnsiConsole.Status().StartAsync("等待选择文件...", async ctx =>
            {
                string filePath = await CommonHelpers.FileHelpers.OpenFile(".exe");
                if (filePath != null && filePath.Contains("StarRail.exe"))
                {
                    AppDataController.SetGamePath(filePath);
                    LoadDataAsync();
                    await Depend.Region.GetRegion(true);
                }
                else
                {
                    ValidGameFile.Subtitle = "选择正确的StarRail.exe\n通常位于[游戏根目录\\StarRail.exe]";
                    ValidGameFile.IsOpen = true;
                }
            });
        }

        public void RMGameLocation(object sender, RoutedEventArgs e)
        {
            AppDataController.RMGamePath();
            LoadDataAsync();
        }

        private void ReloadFrame(object sender, RoutedEventArgs e)
        {
            StartGameView_Loaded(sender, e);
        }

        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            StartGame();
        }

        private async void StartUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (!DownloadHelpers.isDownloading && !DownloadHelpers.isPaused)
            {
                // 开始新的下载任务
                await StartDownloadAndCheckStatus();
            }
            else if (DownloadHelpers.isPaused)
            {
                // 恢复下载任务
                GameUpdate.ResumeDownload(UpdateProgressChanged);
                DownloadHelpers.isPaused = false;
                UpdateUI();
            }
        }

        private void PauseUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (DownloadHelpers.isDownloading)
            {
                GameUpdate.PauseDownload();
                DownloadHelpers.isPaused = true;
                CommonHelpers.TaskbarHelper.SetProgressState(TaskbarProgressBarState.Paused);
                UpdateUI();
            }
        }

        private async Task StartDownloadAndCheckStatus()
        {
            UpdateUI();
            bool updateSuccessful = await GameUpdate.StartDownload(UpdateProgressChanged);
            if (updateSuccessful)
            {
                await LoadDataAsync(); // 下载和解压完成后加载数据
            }
            await CheckDownloadStatus();
        }

        private void UpdateProgressChanged(double progress, string speed, string size)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                downloadProgressBar.Value = progress;
                updateRunning_Speed.Text = speed;
                updateRunning_Size.Text = size;
                updateRunning_Percent.Text = $"{progress:F0}%";
            });
        }

        private async Task CheckDownloadStatus()
        {
            while (DownloadHelpers.isDownloading)
            {
                UpdateUI();
                await Task.Delay(100);
            }

            // 下载结束后的处理
            DownloadHelpers.DownloadProgressChanged -= UpdateProgressChanged;
            startUpdate.Visibility = Visibility.Collapsed;
            updateRunning.Visibility = Visibility.Collapsed;
            startGame.Visibility = Visibility.Visible;
            await LoadDataAsync(); // 确保异步方法被正确等待
        }

        private void UpdateUI()
        {
            if (isGameUpdateRequire)
            {
                startGame_Panel.Visibility = Visibility.Collapsed;
                if (DownloadHelpers.isDownloading)
                {
                    if (DownloadHelpers.isPaused)
                    {
                        startUpdate.Visibility = Visibility.Visible;
                        updateRunning.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        startUpdate.Visibility = Visibility.Collapsed;
                        updateRunning.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    startUpdate.Visibility = Visibility.Visible;
                    updateRunning.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                startUpdate.Visibility = Visibility.Collapsed;
                updateRunning.Visibility = Visibility.Collapsed;
                startGame_Panel.Visibility = Visibility.Visible;
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
                SGFrame.Visibility = Visibility.Collapsed;
            }
            else
            {
                selectGame.IsEnabled = false;
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                rmGame.IsEnabled = true;
                startGame.IsEnabled = true;
                SGFrame.Visibility = Visibility.Visible;
            }
        }

        public void StartGame()
        {
            GameStartUtil gameStartUtil = new GameStartUtil();
            gameStartUtil.StartGame();
        }

        private void CheckProcess_Game(DispatcherQueueTimer timer, object e)
        {
            if (Process.GetProcessesByName("StarRail").Length > 0)
            {
                // 进程正在运行
                startGame.Visibility = Visibility.Collapsed;
                gameRunning.Visibility = Visibility.Visible;
                Frame_GraphicSettingView_Launched_Disable.Visibility = Visibility.Visible;
                Frame_GraphicSettingView_Launched_Disable_Title.Text = "崩坏：星穹铁道正在运行";
                Frame_GraphicSettingView_Launched_Disable_Subtitle.Text = "游戏运行时无法修改画质";
            }
            else
            {
                // 进程未运行
                startGame.Visibility = Visibility.Visible;
                gameRunning.Visibility = Visibility.Collapsed;
                Frame_GraphicSettingView_Launched_Disable.Visibility = Visibility.Collapsed;
            }
        }

        private async Task CheckProcess_Graphics()
        {
            Frame_GraphicSettingView_Loading.Visibility = Visibility.Visible;
            Frame_GraphicSettingView.Content = null;

            if (IsSRToolsHelperRequireUpdate)
            {
                Frame_GraphicSettingView_Disable.Visibility = Visibility.Visible;
                Frame_GraphicSettingView_Disable_Title.Text = "SRToolsHelper需要更新";
                Frame_GraphicSettingView_Disable_Subtitle.Text = "请更新后再使用";
            }
            else
            {
                try
                {
                    string GSValue = await ProcessRun.SRToolsHelperAsync($"/GetReg {GameRegion}");
                    if (!GSValue.Contains("FPS"))
                    {
                        GraphicSelect.IsEnabled = false;
                        GraphicSelect.IsSelected = false;
                        Frame_GraphicSettingView_Loading.Visibility = Visibility.Collapsed;
                        Frame_GraphicSettingView_Disable.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        GS = GSValue;
                        GraphicSelect.IsEnabled = true;
                        GraphicSelect.IsSelected = true;
                        Frame_GraphicSettingView_Loading.Visibility = Visibility.Collapsed;
                        Frame_GraphicSettingView.Visibility = Visibility.Visible;
                        Frame_GraphicSettingView_Disable.Visibility = Visibility.Collapsed;
                        Frame_GraphicSettingView.Navigate(typeof(GraphicSettingView));
                    }
                }
                catch (Exception ex)
                {
                    // 处理异常，记录日志或者显示错误信息
                    Logging.Write($"Exception in CheckProcess_Graphics: {ex.Message}", 3, "CheckProcess_Graphics");
                }
            }
        }

        private async void CheckProcess_Account()
        {
            if (IsSRToolsHelperRequireUpdate)
            {
                Frame_AccountView_Disable.Visibility = Visibility.Visible;
                Frame_AccountView_Disable_Title.Text = "SRToolsHelper需要更新";
                Frame_AccountView_Disable_Subtitle.Text = "请更新后再使用";
            }
            else
            {
                AccountSelect.IsEnabled = true;
                AccountSelect.IsSelected = true;
                Frame_AccountView_Loading.Visibility = Visibility.Collapsed;
                Frame_AccountView.Visibility = Visibility.Visible;
                Frame_AccountView.Navigate(typeof(AccountView));
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
                Logging.Write($"Error fetching prompt: {ex.Message}", 2);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer_Game.Stop();
            DownloadHelpers.DownloadProgressChanged -= UpdateProgressChanged;
            Logging.Write("Timer Stopped", 0);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if(DownloadHelpers.isDownloading == true) UpdateProgressChanged(DownloadHelpers.CurrentProgress, DownloadHelpers.CurrentSpeed, DownloadHelpers.CurrentSize);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            DownloadHelpers.DownloadProgressChanged -= UpdateProgressChanged;
        }
    }
}