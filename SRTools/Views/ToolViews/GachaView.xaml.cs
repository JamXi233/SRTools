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

using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Fiddler;
using Microsoft.UI.Dispatching;
using Windows.ApplicationModel.DataTransfer;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Collections.Generic;
using Spectre.Console;
using System.Net.Http;
using Microsoft.UI.Xaml.Media;
using static SRTools.App;
using Microsoft.UI.Xaml.Input;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;
using WinRT.Interop;
using SRTools.Views.GachaViews;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Windows.Graphics;


namespace SRTools.Views.ToolViews
{

    public sealed partial class GachaView : Page
    {

        public bool isProxyRunning;
        public static String selectedUid;
        public static int selectedCardPoolId;
        public String GachaLink_String;
        public String GachaLinkCache_String;
        public bool isClearGachaSaved;
        public static GachaModel.CardPoolInfo cardPoolInfo;

        private bool isUserInteraction = false;
        private static string latestUpdatedUID = null;


        private FiddlerCoreStartupSettings startupSettings;
        private Proxy oProxy;

        BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
        private DispatcherQueueTimer dispatcherTimer;

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public GachaView()
        {
            InitializeComponent();
            Logging.Write("Switch to GachaView", 0);
            this.Loaded += GachaView_Loaded;
            InitTimer();
        }

        private void InitTimer()
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            dispatcherTimer = dispatcherQueue.CreateTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer.Tick += CheckProcess;
        }

        private async void GachaView_Loaded(object sender, RoutedEventArgs e)
        {

            if (AppDataController.GetGamePath() == "Null") { ProxyButton.IsEnabled = false; }
            // 获取卡池信息
            cardPoolInfo = await GetCardPoolInfo();

            if (cardPoolInfo == null || cardPoolInfo.CardPools == null)
            {
                Console.WriteLine("无法获取卡池信息或卡池列表为空");
                return;
            }
            await LoadUIDs();
        }

        private async Task<GachaModel.CardPoolInfo> GetCardPoolInfo()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync("https://srtools.jamsg.cn/api/cardPoolRule");
                    return JsonConvert.DeserializeObject<GachaModel.CardPoolInfo>(response);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("获取卡池信息时发生错误", "", InfoBarSeverity.Error, false, 5);
                Logging.Write($"获取卡池信息时发生错误: {ex.Message}", 2);
                throw;
            }
        }

        private void ReloadGachaView()
        {
            GachaView_Loaded(null, null);
        }

        private void ComboBox_Click(object sender, object e)
        {
            isUserInteraction = true;
        }

        private async Task LoadUIDs()
        {
            GachaRecordsUID.ItemsSource = null;
            try
            {
                string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords");

                HashSet<string> uidSet = new HashSet<string>();

                if (Directory.Exists(recordsDirectory))
                {
                    var recordFiles = Directory.GetFiles(recordsDirectory, "*.json");
                    foreach (var file in recordFiles)
                    {
                        uidSet.Add(Path.GetFileNameWithoutExtension(file));
                    }

                    var directories = Directory.GetDirectories(recordsDirectory);
                    foreach (var dir in directories)
                    {
                        var dirName = Path.GetFileName(dir);
                        uidSet.Add(dirName);
                    }
                }

                if (uidSet.Count == 0)
                {
                    gachaView.Visibility = Visibility.Collapsed;
                    loadGachaProgress.Visibility = Visibility.Collapsed;
                    noGachaFound.Visibility = Visibility.Visible;
                    ExportUIGF.IsEnabled = false;
                    CreateCapture.IsEnabled = false;
                    return;
                }

                GachaRecordsUID.ItemsSource = uidSet.ToList();
                if (uidSet.Count > 0)
                {
                    GachaRecordsUID.SelectedIndex = 0;
                    loadGachaProgress.Visibility = Visibility.Collapsed;
                    noGachaFound.Visibility = Visibility.Collapsed;
                    gachaView.Visibility = Visibility.Visible;
                    ExportUIGF.IsEnabled = true;
                    ClearGacha.IsEnabled = true;
                    CreateCapture.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification($"抽卡分析", "加载UID时出现错误", InfoBarSeverity.Error);
            }
        }


        private void GachaRecordsUID_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GachaRecordsUID.SelectedItem != null)
            {
                selectedUid = GachaRecordsUID.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedUid))
                {
                    if (isUserInteraction)
                    {
                        LoadGachaRecords(selectedUid);
                    }
                    else
                    {
                        Logging.Write("GachaUID:" + selectedUid);
                        LoadGachaRecords(selectedUid);
                        isUserInteraction = true;
                    }
                }
                else
                {
                    Logging.Write("UID为空");
                }
            }
        }

        private async void LoadGachaRecords(string uid)
        {
            try
            {
                Logging.Write("Load GachaRecords...");
                string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords");
                string filePath = Path.Combine(recordsDirectory, $"{uid}.json");
                string uidDirectory = Path.Combine(recordsDirectory, uid);

                try
                {
                    // 检查是否存在老版本的 ini 文件
                    var iniFiles = Directory.GetFiles(uidDirectory, "GachaRecords*.ini");
                    if (iniFiles.Length > 0)
                    {
                        DialogManager.RaiseDialog(XamlRoot, "检测到老版本记录", "检测到老版本的跃迁记录文件。请升级或删除这些文件。", true, "升级", async () => await UpdateGachaRecord(uid));
                        return;
                    }
                }
                catch (Exception ex) { }
                

                if (!File.Exists(filePath))
                {
                    if (isUserInteraction)
                    {
                        return;
                    }

                    // 使用for循环遍历所有选项
                    bool recordFound = false;
                    isUserInteraction = false;
                    GachaRecordsUID.SelectionChanged -= GachaRecordsUID_SelectionChanged;
                    for (int i = 0; i < GachaRecordsUID.Items.Count; i++)
                    {
                        GachaRecordsUID.SelectedIndex = i;
                        var newUid = GachaRecordsUID.SelectedItem.ToString();
                        string newFilePath = Path.Combine(recordsDirectory, $"{newUid}.json");

                        if (File.Exists(newFilePath))
                        {
                            recordFound = true;
                            break;
                        }
                    }

                    if (!recordFound)
                    {
                        GachaRecordsUID.SelectedIndex = 0;
                        NotificationManager.RaiseNotification("无可用的跃迁记录", "", InfoBarSeverity.Warning);
                        gachaNav.Visibility = Visibility.Collapsed;
                        gachaFrame.Visibility = Visibility.Collapsed;
                    }
                    GachaRecordsUID.SelectionChanged += GachaRecordsUID_SelectionChanged;
                    return;
                }

                var jsonContent = await File.ReadAllTextAsync(filePath);
                var gachaData = JsonConvert.DeserializeObject<GachaModel.GachaData>(jsonContent);

                DisplayGachaData(gachaData);
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("加载跃迁记录时发生错误", $"{ex.Message}", InfoBarSeverity.Error);
            }
        }


        private void DisplayGachaData(GachaModel.GachaData gachaData)
        {
            Logging.Write("Display GachaData...");
            gachaNav.Items.Clear();

            if (gachaData?.list == null || gachaData.list.Count == 0)
            {
                return;
            }

            SelectorBarItem firstEnabledItem = null;

            foreach (var pool in gachaData.list)
            {
                var item = new SelectorBarItem
                {
                    Text = pool.cardPoolType,
                    Tag = pool.cardPoolId.ToString(),
                    IsEnabled = pool.records != null && pool.records.Count > 0
                };

                if (item.IsEnabled && firstEnabledItem == null)
                {
                    firstEnabledItem = item;
                }

                item.Tapped += SelectorBarItem_Tapped;
                gachaNav.Items.Add(item);
            }

            if (firstEnabledItem != null)
            {
                gachaNav.Visibility = Visibility.Visible;
                gachaFrame.Visibility = Visibility.Visible;
                firstEnabledItem.IsSelected = true;
                LoadGachaPoolData(firstEnabledItem.Tag.ToString());
            }
        }

        private void SelectorBarItem_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is SelectorBarItem item)
            {
                string tag = item.Tag.ToString();
                Console.WriteLine($"Selected Card Pool: {tag}");
                selectedCardPoolId = int.Parse(tag);
                LoadGachaPoolData(tag);
            }
        }

        private void LoadGachaPoolData(string cardPoolId)
        {
            // 切换到新选择的池子时重新加载frame
            selectedCardPoolId = int.Parse(cardPoolId);
            if (gachaFrame != null)
            {
                gachaFrame.Navigate(typeof(TempGachaView), selectedUid);
            }
        }


        private async void ExportUIGF_Click(object sender, RoutedEventArgs e)
        {
            string recordsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"JSG-LLC\SRTools\GachaRecords");
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy_MM_dd_HH_mm_ss");

            var suggestFileName = $"SRTools_Gacha_Export_{selectedUid}_{formattedDate}_UIGF4";
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "Uniformed Interchangeable GachaLog Format standard v4.0", new List<string> { ".json" } }
            };
            var defaultExtension = ".json";

            string filePath = await CommonHelpers.FileHelpers.SaveFile(suggestFileName, fileTypeChoices, defaultExtension);
            if (filePath != null)
            {
                await ExportGacha.ExportAsync($"{recordsBasePath}\\{selectedUid}.json", filePath);
            }
        }

        private async void ImportUIGF_Click(object sender, RoutedEventArgs e)
        {
            string filePath = await CommonHelpers.FileHelpers.OpenFile(".json");

            if (filePath != null)
            {
                string recordsBasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"JSG-LLC\SRTools\GachaRecords");
                if (await ImportGacha.Import(filePath) == 2)
                {
                    SharedDatas.UpdateSRGF.UpdateSRGFFilePath = filePath;
                    DialogManager.RaiseDialog(XamlRoot, "检测到旧版本SRGF文件", "您的文件需要转换为新格式并导入",true,"转换",async () => await UpdateGachaRecord_Run(null,"srgf_update"));
                    return;
                }
            }
            ReloadGachaView();
        }

        private async void ClearGacha_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.RaiseDialog(XamlRoot, "清空跃迁记录", $"确定要清空UID:{selectedUid}的跃迁记录吗？", true, "确认", ClearGacha_Run);
        }

        private void ClearGacha_Run()
        {
            try
            {
                string recordsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords");
                string filePath = Path.Combine(recordsDirectory, $"{selectedUid}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logging.Write($"Deleted Gacha Record File: {filePath}", 0);
                }

                NotificationManager.RaiseNotification("操作成功", "已成功清除跃迁记录", InfoBarSeverity.Success, true, 2);
                isUserInteraction = false;
                latestUpdatedUID = null;
                selectedUid = null;
                ReloadGachaView();
            }
            catch (Exception ex)
            {
                Logging.Write($"Error clearing Gacha records: {ex.Message}", 2);
                NotificationManager.RaiseNotification("操作失败", "清除跃迁记录时出现错误", InfoBarSeverity.Error, true, 3);
            }
        }


        public Border CreateDetailBorder()
        {
            return new Border
            {
                Padding = new Thickness(3),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8)
            };
        }

        public async Task StartAsync()
        {
            await Task.Run(() =>
            {
                startupSettings = new FiddlerCoreStartupSettingsBuilder()
                    .ListenOnPort(8888)
                    .RegisterAsSystemProxy()
                    .DecryptSSL()
                    .Build();

                FiddlerApplication.BeforeRequest += OnBeforeRequest;
                FiddlerApplication.Startup(startupSettings);
                Logging.Write("Starting Proxy...", 0);
                oProxy = FiddlerApplication.CreateProxyEndpoint(8888, true, "127.0.0.1");
                Logging.Write("Started Proxy", 0);
            });
        }

        public void Stop()
        {
            Logging.Write("Stopping Proxy...", 0);
            FiddlerApplication.Shutdown();
            if (oProxy != null)
            {
                oProxy.Dispose();
                Logging.Write("Stopped Proxy", 0);
                gacha_status.Text = "等待操作";
                Enable_NavBtns();
            }
        }


        private async void OnBeforeRequest(Session session)
        {
            await Task.Run(() =>
            {
                if (session.fullUrl.Contains("getGachaLog"))
                {
                    GachaLink_String = session.fullUrl;
                }
            });
        }

        private async void ProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProxyButton.IsChecked ?? false)
            {
                if (Process.GetProcessesByName("StarRail").Length > 0)
                {
                    // 进程正在运行
                    try
                    {
                        ProxyButton.IsChecked = true;
                        CertMaker.oCertProvider = certProvider;
                        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC\\SRTools");
                        string filePath = Path.Combine(folderPath, "RootCertificate.p12");
                        string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw0rd";
                        if (!Directory.Exists(folderPath))
                        {
                            Logging.Write("Creating Proxy Cert...", 0);
                            Directory.CreateDirectory(folderPath);
                        }
                        if (!File.Exists(filePath))
                        {
                            Logging.Write("Creating Proxy Cert...", 0);
                            certProvider.CreateRootCertificate();
                            certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(filePath, rootCertificatePassword);
                        }
                        certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(filePath, rootCertificatePassword);
                        if (!CertMaker.rootCertIsTrusted())
                        {
                            Logging.Write("Trust Proxy Cert...", 0);
                            CertMaker.trustRootCert();
                        }
                        isProxyRunning = true;
                        await StartAsync();
                        Disable_NavBtns();
                        gacha_status.Text = "正在等待打开抽卡历史记录...";
                        WaitOverlayManager.RaiseWaitOverlay(true, "正在等待跃迁记录链接", "请前往游戏打开跃迁记录", true, 0, true, "取消获取", StopProxy);
                        ProxyButton.IsEnabled = true;
                        dispatcherTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        NotificationManager.RaiseNotification("出现错误", ex.Message, InfoBarSeverity.Error);
                        AnsiConsole.WriteException(ex);
                    }
                }
                else
                {
                    // 进程未运行
                    ProxyButton.IsChecked = false;
                    NotificationManager.RaiseNotification("未运行游戏", "先到启动游戏页面启动游戏后再启动代理", InfoBarSeverity.Warning);
                }
            }
            else { Stop(); isProxyRunning = false; }
        }

        private void StopProxy()
        {
            Stop();
            ProxyButton.IsChecked = false;
            isProxyRunning = false;

            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        // 定时器回调函数，检查进程是否正在运行
        private async void CheckProcess(DispatcherQueueTimer timer, object e)
        {
            if (isProxyRunning || GachaLinkCache_String != null)
            {
                ProxyButton.IsChecked = true;
                if (GachaLink_String != null && GachaLink_String.Contains("getGachaLog"))
                {
                    // 进程正在运行
                    gacha_status.Text = GachaLink_String;
                    ProxyButton.IsEnabled = false;
                    Stop();
                    Logging.Write("Get GachaLink Finish!", 0);
                    WaitOverlayManager.RaiseWaitOverlay(true, "正在获取API信息,请不要退出", "请稍等片刻", true, 0);
                    ProxyButton.IsChecked = false;
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.SetText(GachaLink_String);
                    Logging.Write("Writing GachaLink in Clipboard...", 0);
                    Clipboard.SetContent(dataPackage);
                    dispatcherTimer.Stop();
                    Logging.Write("Loading Gacha Data...", 0);
                    await GachaRecords.GetGachaAsync(GachaLink_String);
                    NotificationManager.RaiseNotification("获取跃迁记录完成", "", InfoBarSeverity.Success, true, 2);
                    GachaLink_String = null;
                    WaitOverlayManager.RaiseWaitOverlay(false);
                    ProxyButton.IsEnabled = true;
                    ReloadGachaView();
                }
            }
            else
            {
                ProxyButton.IsChecked = false;
            }
        }


        public async void CreateCapture_Click(object sender, RoutedEventArgs e)
        {
            // 创建对话框内容
            var content = new StackPanel { Spacing = 4 }; // 设置 StackPanel 的 Spacing 为 4

            var checkBoxScreenShotSelf = new CheckBox { Content = "是否自动截图", IsChecked = true };
            var checkBoxShowRecords = new CheckBox { Content = "是否显示跃迁记录", IsChecked = true };
            var checkBoxHideUID = new CheckBox { Content = "是否隐藏UID", IsChecked = true };

            content.Children.Add(new TextBlock { Text = "通用设置", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
            content.Children.Add(checkBoxScreenShotSelf);
            content.Children.Add(new StackPanel { Height = 1, Background = new SolidColorBrush(Colors.Gray) });

            content.Children.Add(new TextBlock { Text = "截图设置", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold });
            content.Children.Add(checkBoxShowRecords);
            content.Children.Add(checkBoxHideUID);

            DialogManager.RaiseDialog(XamlRoot, "创建截图", content, true, "确认", () => CreateCapture_Run((bool)checkBoxShowRecords.IsChecked, (bool)checkBoxScreenShotSelf.IsChecked, (bool)checkBoxHideUID.IsChecked));
        }

        private async void CreateCapture_Run(bool isShowRecords, bool isScreenShotSelf, bool isHideUID)
        {
            WaitOverlayManager.RaiseWaitOverlay(true, "等待...", "");
            await Task.Delay(100);
            var tcs = new TaskCompletionSource<bool>();

            // 设置静态属性
            ScreenShotGacha.isShowGachaRecords = isShowRecords;
            ScreenShotGacha.isScreenShotSelf = isScreenShotSelf;
            ScreenShotGacha.isHideUID = isHideUID;
            var screenShotGacha = new ScreenShotGacha
            {
                TaskCompletionSource = tcs
            };

            var window = new Window
            {
                Content = screenShotGacha,
                Title = selectedUid + "_ScreenShotGachaView"
            };

            screenShotGacha.CurrentWindow = window; // 设置窗口实例属性

            IntPtr hWnd = WindowNative.GetWindowHandle(window);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = false;
                presenter.IsMaximizable = false;
            }


            if (isShowRecords) appWindow.Resize(new SizeInt32(904, 580));
            else appWindow.Resize(new SizeInt32(520, 580));
            window.Activate();
            if (ScreenShotGacha.isScreenShotSelf) appWindow.Hide();

            window.Closed += (s, args) =>
            {
                CreateCapture.IsEnabled = true;
                WaitOverlayManager.RaiseWaitOverlay(false);
                tcs.TrySetResult(ScreenShotGacha.isFinished);
                // 重置静态属性
                ScreenShotGacha.isFinished = false;
                ScreenShotGacha.isShowGachaRecords = false;
                ScreenShotGacha.isScreenShotSelf = false;
            };

            CreateCapture.IsEnabled = false;

            bool isScreenShotFinished = await tcs.Task;

            if (isScreenShotFinished)
            {
                WaitOverlayManager.RaiseWaitOverlay(false, "", "");
                NotificationManager.RaiseNotification("截图完成", "截图已保存到\n" + SharedDatas.ScreenShotData.ScreenShotPath, InfoBarSeverity.Success, false, 3, () => CommonHelpers.FileHelpers.OpenFileLocation(SharedDatas.ScreenShotData.ScreenShotPath), "打开文件夹");
            }
        }

        private void Disable_NavBtns()
        {
            NavigationView parentNavigationView = GetParentNavigationView(this);
            if (parentNavigationView != null)
            {
                var selectedItem = parentNavigationView.SelectedItem;
                var excludeTags = new HashSet<string> { "account_status", "event", "account", "jsg_account" };  // 需要排除的标签

                foreach (var menuItem in parentNavigationView.MenuItems.Concat(parentNavigationView.FooterMenuItems))
                {
                    if (menuItem is NavigationViewItem navViewItem && navViewItem != selectedItem && !excludeTags.Contains(navViewItem.Tag as string))
                    {
                        navViewItem.IsEnabled = false;
                    }
                }
                // 特别处理设置按钮
                if (parentNavigationView.SettingsItem is NavigationViewItem settingsItem && settingsItem != selectedItem)
                {
                    settingsItem.IsEnabled = false;
                }
            }
        }

        private void Enable_NavBtns()
        {
            NavigationView parentNavigationView = GetParentNavigationView(this);
            if (parentNavigationView != null)
            {
                var selectedItem = parentNavigationView.SelectedItem;
                var excludeTags = new HashSet<string> { "account_status", "event", "account", "jsg_account" };  // 需要排除的标签

                foreach (var menuItem in parentNavigationView.MenuItems.Concat(parentNavigationView.FooterMenuItems))
                {
                    if (menuItem is NavigationViewItem navViewItem && navViewItem != selectedItem && !excludeTags.Contains(navViewItem.Tag as string))
                    {
                        navViewItem.IsEnabled = true;
                    }
                }
                // 特别处理设置按钮
                if (parentNavigationView.SettingsItem is NavigationViewItem settingsItem && settingsItem != selectedItem)
                {
                    settingsItem.IsEnabled = true;
                }
            }
        }

        private void SetButtonsEnabledState(DependencyObject parent, bool isEnabled)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Button button)
                {
                    button.IsEnabled = isEnabled;
                }
                else
                {
                    SetButtonsEnabledState(child, isEnabled);
                }
            }
        }

        private NavigationView GetParentNavigationView(FrameworkElement child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is NavigationView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as NavigationView;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= CheckProcess;
        }

        public async Task UpdateGachaRecord(string uid)
        {
            await UpdateGachaRecord_Run(uid);
            ReloadGachaView();
        }

        public async Task UpdateGachaRecord_Run(string uid, string mode = "?")
        {
            string baseDir;
            string newFilePath;
            if (mode == "?" || mode == "srgf_merge")
            {
                if (mode == "srgf_merge")
                {
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords", "tmp", uid);
                    newFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords", "tmp", $"{uid}_Converted.json");
                }
                else
                {
                    baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords", uid);
                    newFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords", $"{uid}.json");
                }
                string[] oldFiles = { "GachaRecords_Character.ini", "GachaRecords_LightCone.ini", "GachaRecords_Newbie.ini", "GachaRecords_Regular.ini" };
                string[] oldFilePaths = oldFiles.Select(f => Path.Combine(baseDir, f)).ToArray();

                var cardPoolInfo = await GetCardPoolInfoAsync();

                var allRecords = new List<GachaCommon.SourceGachaRecord>();

                foreach (var oldFilePath in oldFilePaths)
                {
                    if (File.Exists(oldFilePath))
                    {
                        var oldRecords = JsonConvert.DeserializeObject<List<OGachaCommon.OItem>>(await File.ReadAllTextAsync(oldFilePath));
                        allRecords.AddRange(oldRecords.Select(r => new GachaCommon.SourceGachaRecord
                        {
                            itemId = r.ItemId,
                            count = r.Count,
                            time = r.Time,
                            name = r.Name,
                            itemType = r.ItemType,
                            rankType = r.RankType,
                            id = r.Id,
                            gachaType = r.GachaType,
                            gachaId = r.GachaId
                        }));
                    }
                }

                var newRecords = allRecords
                    .GroupBy(r => r.gachaType)
                    .Select(g => new GachaCommon.SourceRecord
                    {
                        cardPoolId = cardPoolInfo.CardPoolIds[g.Key],
                        cardPoolType = cardPoolInfo.CardPoolTypes[g.Key],
                        records = g.OrderByDescending(r => r.id).ToList()
                    })
                    .OrderBy(r => GetSortOrder(r.cardPoolId, cardPoolInfo))
                    .ToList();

                var newData = new GachaCommon.SourceData
                {
                    info = new GachaCommon.SourceInfo { uid = uid },
                    list = newRecords
                };

                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var newJson = JsonConvert.SerializeObject(newData, settings);
                await File.WriteAllTextAsync(newFilePath, newJson);

                // 删除旧文件
                foreach (var oldFilePath in oldFilePaths)
                {
                    if (File.Exists(oldFilePath))
                    {
                        File.Delete(oldFilePath);
                    }
                }
                Directory.Delete(baseDir);
                if (mode == "srgf_merge")
                {
                    NotificationManager.RaiseNotification("转换完成", "旧版本抽卡记录已转换为新版本", InfoBarSeverity.Success,true,3);
                    string exportConvertedFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords", "tmp", $"{uid}_Converted_Export.json");
                    await ExportGacha.ExportAsync(newFilePath, exportConvertedFilePath);
                    await ImportGacha.Import(exportConvertedFilePath);
                    Directory.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "GachaRecords", "tmp"),true);
                    ReloadGachaView();
                }
                latestUpdatedUID = uid;
                Logging.Write($"Updated Gacha records for UID: {uid}");
            }
            else if (mode == "srgf_update")
            {
                if (uid == null)
                {
                    OGachaCommon oGachaCommon = new OGachaCommon();
                    await oGachaCommon.ImportSRGF();
                    await UpdateGachaRecord_Run(SharedDatas.UpdateSRGF.UpdateSRGFUID, "srgf_merge");
                }
            }
        }

        private static int GetSortOrder(int cardPoolId, CardPoolInfo cardPoolInfo)
        {
            return cardPoolId switch
            {
                var id when id == cardPoolInfo.CardPoolIds.GetValueOrDefault("11", -1) => 1,
                var id when id == cardPoolInfo.CardPoolIds.GetValueOrDefault("12", -1) => 2,
                var id when id == cardPoolInfo.CardPoolIds.GetValueOrDefault("2", -1) => 3,
                var id when id == cardPoolInfo.CardPoolIds.GetValueOrDefault("1", -1) => 4,
                _ => 100 // 默认排序顺序
            };
        }

        private static async Task<CardPoolInfo> GetCardPoolInfoAsync()
        {
            using var client = new HttpClient();
            var response = await client.GetStringAsync("https://srtools.jamsg.cn/api/cardPoolInfo");
            return JsonConvert.DeserializeObject<CardPoolInfo>(response);
        }

        
    }
}