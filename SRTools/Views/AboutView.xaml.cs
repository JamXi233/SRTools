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
using Microsoft.UI.Xaml.Media;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using System.IO.Compression;
using Windows.Storage.Pickers;
using Newtonsoft.Json;
using System.Net.Http;
using static SRTools.App;

namespace SRTools.Views
{
    public sealed partial class AboutView : Page
    {
        private readonly GetGiteeLatest _getGiteeLatest = new GetGiteeLatest();
        private readonly GetJSGLatest _getJSGLatest = new GetJSGLatest();

        private bool isProgrammaticChange = false;

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        private static int Notification_Test_Count = 0;

        public AboutView()
        {
            InitializeComponent();
            Logging.Write("Switch to AboutView", 0);

            this.Loaded += AboutView_Loaded;
        }

        private async void AboutView_Loaded(object sender, RoutedEventArgs e)
        {
            isProgrammaticChange = true;
            bool isDebug = Debugger.IsAttached || App.SDebugMode;
            consoleToggle.IsEnabled = !isDebug;
            debug_Mode.Visibility = isDebug ? Visibility.Visible : Visibility.Collapsed;
            debug_Message.Text = App.SDebugMode ? "您现在处于手动Debug模式" : "";
            appVersion.Text = $"SRTools {Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
            GetVersionButton();
            CheckFont();
            LoadSettings();
            await Task.Delay(200);
            isProgrammaticChange = false;
        }

        public void LoadSettings()
        {
            consoleToggle.IsChecked = AppDataController.GetConsoleMode() == 1;
            terminalToggle.IsChecked = AppDataController.GetTerminalMode() == 1;
            autoCheckUpdateToggle.IsChecked = AppDataController.GetAutoCheckUpdate() == 1;
            adminModeToggle.IsChecked = AppDataController.GetAdminMode() == 1;
            userviceRadio.SelectedIndex = new[] { 1, 2, 0 }[AppDataController.GetUpdateService()];
            themeRadio.SelectedIndex = AppDataController.GetDayNight();
        }

        private void CheckFont()
        {
            var fontsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            installSFF.IsEnabled = !File.Exists(Path.Combine(fontsFolderPath, "SegoeIcons.ttf")) || !File.Exists(Path.Combine(fontsFolderPath, "Segoe Fluent Icons.ttf"));
            installSFF.Content = installSFF.IsEnabled ? "安装图标字体" : "图标字体正常";
        }


        private async void GetVersionButton()
        {
            var response = await new HttpClient().GetAsync("https://api.jamsg.cn/version");
            if (response.IsSuccessStatusCode)
            {
                var data = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
                apiVersion.Text = "ArrowAPI " + data.arrow_ver;
                antiCatVersion.Text = "AntiCat " + data.anticat_ver;
            }
        }


        private void Console_Toggle(object sender, RoutedEventArgs e)
        {
            if (consoleToggle.IsChecked ?? false) TerminalMode.ShowConsole(); else TerminalMode.HideConsole();
            AppDataController.SetConsoleMode(consoleToggle.IsChecked == true ? 1 : 0);
        }


        private void TerminalMode_Toggle(object sender, RoutedEventArgs e)
        {
            TerminalTip.IsOpen = terminalToggle.IsChecked ?? false;
            AppDataController.SetTerminalMode(terminalToggle.IsChecked == true ? 1 : 0);
        }

        private void Auto_Check_Update_Toggle(object sender, RoutedEventArgs e)
        {
            AppDataController.SetAutoCheckUpdate(autoCheckUpdateToggle.IsChecked == true ? 1 : 0);
        }

        private void Admin_Mode_Toggle(object sender, RoutedEventArgs e)
        {
            AppDataController.SetAdminMode(adminModeToggle.IsChecked == true ? 1 : 0);
        }

        public void Clear_AllData_TipShow(object sender, RoutedEventArgs e)
        {
            ClearAllDataTip.IsOpen = true;
        }

        public void Clear_AllData(TeachingTip sender, object args)
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\", "1");
        }

        private void Clear_AllData_NoClose(object sender, RoutedEventArgs e, string Close = "0")
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DeleteFolder(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\", Close);
        }

        private void DeleteFolder(string folderPath, String Close)
        {
            if (Directory.Exists(folderPath))
            {
                try { Directory.Delete(folderPath, true); }
                catch (IOException) { }
            }
            _ = ClearLocalDataAsync(Close);
        }

        public async Task ClearLocalDataAsync(String Close)
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            await DeleteFilesAndSubfoldersAsync(localFolder, Close);
            await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);
        }

        private async Task DeleteFilesAndSubfoldersAsync(StorageFolder folder, String Close)
        {
            var items = await folder.GetItemsAsync();
            foreach (var item in items)
            {
                if (item is StorageFile file)
                {
                    await file.DeleteAsync();
                }
                else if (item is StorageFolder subfolder)
                {
                    await DeleteFilesAndSubfoldersAsync(subfolder, Close);
                    await subfolder.DeleteAsync();
                }
            }
            if (Close == "1")
            {
                Application.Current.Exit();
            }
        }

        private async void Check_Update(object sender, RoutedEventArgs e)
        {
            UpdateTip.IsOpen = false;
            var result = await GetUpdate.GetSRToolsUpdate();
            var status = result.Status;
            UpdateTip.Target = checkUpdate;
            UpdateTip.ActionButtonClick -= StartDependForceUpdate;
            UpdateTip.ActionButtonClick -= StartForceUpdate;
            UpdateTip.ActionButtonClick -= DisplayUpdateInfo;
            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            UpdateTip.Title = isShiftPressed ? "遇到麻烦了吗" : status == 0 ? "无可用更新" : status == 1 ? "有可用更新" : "网络连接失败，可能是请求次数过多";
            UpdateTip.Subtitle = isShiftPressed ? "尝试重装SRTools" : status == 1 ? "新版本:" + result.Version : null;
            UpdateTip.ActionButtonContent = isShiftPressed ? "强制重装" : status == 1 ? "查看详情" : null;
            UpdateTip.CloseButtonContent = "关闭";

            if (isShiftPressed) UpdateTip.ActionButtonClick += StartForceUpdate;
            if (status == 1) UpdateTip.ActionButtonClick += DisplayUpdateInfo;
            UpdateTip.IsOpen = true;
        }

        private async void Check_Depend_Update(object sender, RoutedEventArgs e)
        {
            UpdateTip.IsOpen = false;
            var result = await GetUpdate.GetDependUpdate();
            var status = result.Status;
            UpdateTip.Target = checkDependUpdate;
            UpdateTip.ActionButtonClick -= StartDependForceUpdate;
            UpdateTip.ActionButtonClick -= StartForceUpdate;
            UpdateTip.ActionButtonClick -= DisplayUpdateInfo;
            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            UpdateTip.Title = isShiftPressed ? "遇到麻烦了吗" : status == 0 ? "无可用更新" : status == 1 ? "有可用更新" : "网络连接失败，可能是请求次数过多";
            UpdateTip.Subtitle = isShiftPressed ? "尝试重装SRToolsHelper" : status == 1 ? "新版本:" + result.Version : null;
            UpdateTip.ActionButtonContent = isShiftPressed ? "强制重装" : status == 1 ? "查看详情" : null;
            UpdateTip.CloseButtonContent = "关闭";

            if (isShiftPressed) UpdateTip.ActionButtonClick += StartDependForceUpdate;
            else if (status == 1) UpdateTip.ActionButtonClick += DisplayUpdateInfo;

            UpdateTip.IsOpen = true;
        }

        public async void DisplayUpdateInfo(TeachingTip sender, object args)
        {
            bool isSRTools = UpdateTip.Target != checkDependUpdate;
            UpdateResult updateinfo = isSRTools ? await GetUpdate.GetSRToolsUpdate() : await GetUpdate.GetDependUpdate();
            UpdateTip.IsOpen = false;
            ContentDialog updateDialog = new ContentDialog
            {
                Title = (isSRTools ? "SRTools" : "Helper") + ":" + updateinfo.Version + "版本可用",
                Content = "更新日志:\n" + updateinfo.Changelog,
                CloseButtonText = "关闭",
                PrimaryButtonText = "立即更新",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = sender.XamlRoot,
            };
            if (await updateDialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (isSRTools) StartUpdate(); else StartDependUpdate();
            }
        }


        public async void StartUpdate()
        {
            UpdateTip.IsOpen = false;
            WaitOverlayManager.RaiseWaitOverlay(true, "正在更新", "请稍等片刻", true, 0);
            await InstallerHelper.GetInstaller();
            string channelArgument = GetChannelArgument();
            if (InstallerHelper.RunInstaller(channelArgument) != 0)
            {
                NotificationManager.RaiseNotification("更新失败", "", InfoBarSeverity.Error, true, 3);
            }
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        public async void StartForceUpdate(TeachingTip sender, object args)
        {
            UpdateTip.IsOpen = false;
            WaitOverlayManager.RaiseWaitOverlay(true, "正在强制重装SRTools", "请稍等片刻", true, 0);
            await InstallerHelper.GetInstaller();
            string channelArgument = GetChannelArgument();
            if (InstallerHelper.RunInstaller($"/force {channelArgument}") != 0)
            {
                NotificationManager.RaiseNotification("更新失败", "", InfoBarSeverity.Error, true, 3);
            }
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        public async void StartDependUpdate()
        {
            UpdateTip.IsOpen = false;
            WaitOverlayManager.RaiseWaitOverlay(true, "正在更新依赖", "请稍等片刻", true, 0);
            await InstallerHelper.GetInstaller();
            string channelArgument = GetChannelArgument();
            InstallerHelper.RunInstaller($"/depend {channelArgument}");
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        public async void StartDependForceUpdate(TeachingTip sender, object args)
        {
            UpdateTip.IsOpen = false;
            WaitOverlayManager.RaiseWaitOverlay(true, "正在强制重装依赖", "请稍等片刻", true, 0);
            await InstallerHelper.GetInstaller();
            string channelArgument = GetChannelArgument();
            if (InstallerHelper.RunInstaller($"/depend /force {channelArgument}") != 0)
            {
                NotificationManager.RaiseNotification("强制重装依赖失败", "", InfoBarSeverity.Error, true, 3);
            }
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        private void Create_Desktop_Shortcut(object sender, RoutedEventArgs e)
        {
            CreateShortcut.CreateDesktopShortcut();
        }

        private string GetChannelArgument()
        {
            int channel = AppDataController.GetUpdateService();
            return channel switch
            {
                0 => "/channel github",
                2 => "/channel ds",
                _ => string.Empty
            };
        }

        // 选择主题开始
        private void ThemeRadio_Follow(object sender, RoutedEventArgs e)
        {
            if (!isProgrammaticChange) { ThemeTip.IsOpen = true; AppDataController.SetDayNight(0); }
        }

        private void ThemeRadio_Light(object sender, RoutedEventArgs e)
        {
            if (!isProgrammaticChange) { ThemeTip.IsOpen = true; AppDataController.SetDayNight(1); }
        }

        private void ThemeRadio_Dark(object sender, RoutedEventArgs e)
        {
            if (!isProgrammaticChange) { ThemeTip.IsOpen = true; AppDataController.SetDayNight(2); }
        }

        // 选择下载渠道开始
        private void uservice_Github_Choose(object sender, RoutedEventArgs e)
        {
            if (!isProgrammaticChange) { AppDataController.SetUpdateService(0); }
        }

        private void uservice_Gitee_Choose(object sender, RoutedEventArgs e)
        {
            if (!isProgrammaticChange) { AppDataController.SetUpdateService(1); }
        }

        private void uservice_JSG_Choose(object sender, RoutedEventArgs e)
        {
            if (!isProgrammaticChange) { AppDataController.SetUpdateService(2); }
        }


        private async void Backup_Data(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy_MM_dd_HH_mm_ss");
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var suggestFileName = "SRTools_Backup_" + formattedDate;
            var fileTypeChoices = new Dictionary<string, List<string>>
            {
                { "Zip Archive", new List<string> { ".json" } }
            };
            var defaultExtension = ".SRToolsBackup";

            string filePath = await CommonHelpers.FileHelpers.SaveFile(suggestFileName, fileTypeChoices, defaultExtension);

            if (filePath != null)
            {
                string startPath = userDocumentsFolderPath + @"\JSG-LLC\SRTools";
                string zipPath = filePath;
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                ZipFile.CreateFromDirectory(startPath, zipPath);
                NotificationManager.RaiseNotification("备份完成", null, InfoBarSeverity.Success, true, 1);
            }
        }

        private void Restore_Data_Click(object sender, RoutedEventArgs e)
        {
            DialogManager.RaiseDialog(this.XamlRoot, "是否要还原数据？", "还原数据将会清空当前所有数据\n还原成功后会进入首次设置", true, "选择文件", Restore_Data);
        }

        private async void Restore_Data()
        {
            WaitOverlayManager.RaiseWaitOverlay(true, "等待选择还原文件", null, false);

            string filePath = await CommonHelpers.FileHelpers.OpenFile(".SRToolsBackup");

            if (filePath != null)
            {
                string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Task.Run(() => Clear_AllData_NoClose(null, null)).Wait();
                Task.Run(() => ZipFile.ExtractToDirectory(filePath, userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\")).Wait();
                await ProcessRun.RestartApp();
            }
            else WaitOverlayManager.RaiseWaitOverlay(false);
        }

        private async void Install_Font_Click(object sender, RoutedEventArgs e)
        {
            installSFF.IsEnabled = false;
            installSFF_Progress.Visibility = Visibility.Visible;
            var progress = new Progress<double>();

            await InstallFont.InstallSegoeFluentFontAsync(progress);
            installSFF.Content = "安装字体后重启SRTools即生效";
            installSFF_Progress.Visibility = Visibility.Collapsed;
        }

        private async void Restart_App(TeachingTip sender, object args)
        {
            Logging.Write("Restarting application", 0);
            await ProcessRun.RestartApp();
        }

        // Debug_Clicks
        private void Debug_Panic_Click(object sender, RoutedEventArgs e)
        {
            Logging.Write("Triggering global exception handler test", 0);
            throw new Exception("全局异常处理测试");
        }

        private void Debug_Notification_Test(object sender, RoutedEventArgs e)
        {
            Notification_Test_Count++;
            Logging.Write("Triggering notification test", 0);
            NotificationManager.RaiseNotification("测试通知", $"这是一条测试通知{Notification_Test_Count}", InfoBarSeverity.Success, false, 1);
        }

        private async void Debug_WaitOverlayManager_Test(object sender, RoutedEventArgs e)
        {
            Logging.Write("Triggering WaitOverlayManager test", 0);
            WaitOverlayManager.RaiseWaitOverlay(true, "全局等待测试", "如果您看到了这个界面，则全局等待测试已成功", true, 0, true, "退出测试", Debug_KillWaitOverlay);
            await Task.Delay(1000);
            Debug_KillWaitOverlay();
        }

        private void Debug_KillWaitOverlay()
        {
            Logging.Write("Killing WaitOverlay", 0);
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        private void Debug_ShowDialog_Test(object sender, RoutedEventArgs e)
        {
            Logging.Write("Triggering ShowDialog test", 0);
            DialogManager.RaiseDialog(XamlRoot);
        }

        // Debug_Disable_NavBtns
        private void Debug_Disable_NavBtns(object sender, RoutedEventArgs e)
        {
            Logging.Write("Toggling navigation buttons", 0);
            NavigationView parentNavigationView = GetParentNavigationView(this);
            if (debug_DisableNavBtns.IsChecked == true)
            {
                if (parentNavigationView != null)
                {
                    foreach (var menuItem in parentNavigationView.MenuItems)
                    {
                        if (menuItem is NavigationViewItem navViewItem)
                        {
                            navViewItem.IsEnabled = false;
                        }
                    }
                    foreach (var menuItem in parentNavigationView.FooterMenuItems)
                    {
                        if (menuItem is NavigationViewItem navViewItem)
                        {
                            navViewItem.IsEnabled = false;
                        }
                    }
                }
            }
            else
            {
                if (parentNavigationView != null)
                {
                    foreach (var menuItem in parentNavigationView.MenuItems)
                    {
                        if (menuItem is NavigationViewItem navViewItem)
                        {
                            navViewItem.IsEnabled = true;
                        }
                    }
                    foreach (var menuItem in parentNavigationView.FooterMenuItems)
                    {
                        if (menuItem is NavigationViewItem navViewItem)
                        {
                            navViewItem.IsEnabled = true;
                        }
                    }
                }
            }
        }

        private NavigationView GetParentNavigationView(FrameworkElement child)
        {
            Logging.Write("Getting parent NavigationView", 0);
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is NavigationView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as NavigationView;
        }

    }
}