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

        [DllImport("User32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        public AboutView()
        {
            InitializeComponent();
            Logging.Write("Switch to AboutView", 0);
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            switch (AppDataController.GetUpdateService())
            {
                case 0:
                    uservice_Github.IsChecked = true;
                    break;
                case 1:
                    uservice_Gitee.IsChecked = true;
                    break;
                case 2:
                    uservice_JSG.IsChecked = true;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid update service value: {localSettings.Values["Config_UpdateService"]}");
            }
            switch (AppDataController.GetConsoleMode())
            {
                case 0:
                    consoleToggle.IsChecked = false;
                    break;
                case 1:
                    consoleToggle.IsChecked = true;
                    break;
                default:
                    consoleToggle.IsChecked = false;
                    break;
            }

            if(TerminalMode.ConsoleStatus()) consoleToggle.IsChecked = true;
            else consoleToggle.IsChecked = false;

            switch (AppDataController.GetTerminalMode())
            {
                case 0:
                    terminalToggle.IsChecked = false;
                    break;
                case 1:
                    terminalToggle.IsChecked = true;
                    break;
                default:
                    terminalToggle.IsChecked = false;
                    break;
            }
            bool SDebugMode = App.SDebugMode;
            bool isDebug = false;
            #if DEBUG
            isDebug = true;
            #else
            #endif
            if (isDebug)
            {
                consoleToggle.IsEnabled = false;
                debug_Mode.Visibility = Visibility.Visible;
            }
            else if (SDebugMode)
            {
                consoleToggle.IsEnabled = false;
                debug_Mode.Visibility = Visibility.Visible;
                debug_Message.Text = "您现在处于手动Debug模式";
            }
            PackageVersion packageVersion = Package.Current.Id.Version;
            string version = $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
            appVersion.Text = "SRTools "+version;
            GetVersionButton_Click(null, null);
            CheckFont();
        }

        private void CheckFont() 
        {
            string fontsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
            string segoeIconsFilePath = Path.Combine(fontsFolderPath, "SegoeIcons.ttf");
            string segoeIconsFilePath2 = Path.Combine(fontsFolderPath, "Segoe Fluent Icons.ttf");
            if (Directory.Exists(fontsFolderPath) && (File.Exists(segoeIconsFilePath) || File.Exists(segoeIconsFilePath2)))
            {
                installSFF.IsEnabled = false;
                installSFF.Content = "图标字体正常";
            }
        }

        private async void GetVersionButton_Click(object sender, RoutedEventArgs e)
        {
            HttpClient client = new HttpClient();
            string url = "https://api.jamsg.cn/version";
            HttpResponseMessage response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string jsonData = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<dynamic>(jsonData);
                string arrowVer = data.arrow_ver;
                string anticatVer = data.anticat_ver;
                apiVersion.Text = "ArrowAPI " + arrowVer;
                antiCatVersion.Text = "AntiCat " + anticatVer;
            }
        }

        private void Console_Toggle(object sender, RoutedEventArgs e)
        {
            var currentProcess = Process.GetCurrentProcess();
            var hWnd = currentProcess.MainWindowHandle;
            // 判断是否需要打开控制台
            if (consoleToggle.IsChecked ?? false)
            {
                TerminalMode.ShowConsole();
                AppDataController.SetConsoleMode(1);
            }
            else
            {
                TerminalMode.HideConsole();
                AppDataController.SetConsoleMode(0);
            }
        }

        private void TerminalMode_Toggle(object sender, RoutedEventArgs e)
        {
            // 判断是否需要打开控制台
            if (terminalToggle.IsChecked ?? false)
            {
                TerminalTip.IsOpen = true;
                AppDataController.SetTerminalMode(1);
            }
            else
            {
                AppDataController.SetTerminalMode(0);
            }
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
            UpdateTip.ActionButtonClick -= DisplayUpdateInfo;

            if (status == 0)
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Title = "无可用更新";
                UpdateTip.Subtitle = null;
                UpdateTip.ActionButtonContent = null;
                UpdateTip.CloseButtonContent = "关闭";
            }
            else if (status == 1)
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Title = "有可用更新";
                UpdateTip.Subtitle = "新版本:"+result.Version;
                UpdateTip.ActionButtonContent = "查看详情";
                UpdateTip.CloseButtonContent = "关闭";
                UpdateTip.ActionButtonClick += DisplayUpdateInfo;
            }
            else
            {
                UpdateTip.IsOpen = true;
                UpdateTip.ActionButtonContent = null;
                UpdateTip.Subtitle = "网络连接失败，可能是请求次数过多";
            }
        }

        private async void Check_Depend_Update(object sender, RoutedEventArgs e)
        {
            UpdateTip.IsOpen = false;
            var result = await GetUpdate.GetDependUpdate();
            var status = result.Status;
            UpdateTip.Target = checkDependUpdate;
            UpdateTip.ActionButtonClick -= StartDependForceUpdate;
            UpdateTip.ActionButtonClick -= DisplayUpdateInfo;
            bool isShiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;

            if (isShiftPressed)
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Title = "遇到麻烦了吗";
                UpdateTip.Subtitle = "尝试重装SRToolsHelper";
                UpdateTip.ActionButtonContent = "强制重装";
                UpdateTip.CloseButtonContent = "关闭";
                UpdateTip.ActionButtonClick += StartDependForceUpdate;
            }
            else if(status == 0)
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Title = "无可用更新";
                UpdateTip.Subtitle = null;
                UpdateTip.ActionButtonContent = null;
                UpdateTip.CloseButtonContent = "关闭";
            }
            else if (status == 1)
            {
                UpdateTip.IsOpen = true;
                UpdateTip.Title = "有可用更新";
                UpdateTip.Subtitle = "新版本:" + result.Version;
                UpdateTip.ActionButtonContent = "查看详情";
                UpdateTip.CloseButtonContent = "关闭";
                UpdateTip.ActionButtonClick += DisplayUpdateInfo;
            }
            else
            {
                UpdateTip.IsOpen = true;
                UpdateTip.ActionButtonContent = null;
                UpdateTip.Subtitle = "网络连接失败，可能是请求次数过多";
            }
        }


        public async void DisplayUpdateInfo(TeachingTip sender, object args)
        {
            UpdateResult updateinfo;
            string Name;
            if (UpdateTip.Target != checkDependUpdate) { updateinfo = await GetUpdate.GetSRToolsUpdate();Name = "SRTools"; }
            else { updateinfo = await GetUpdate.GetDependUpdate(); Name = "Helper"; }
            var version = updateinfo.Version;
            var changelog = updateinfo.Changelog;
            UpdateTip.IsOpen = false;
            ContentDialog updateDialog = new ContentDialog
            {
                Title = Name+":"+version+"版本可用",
                Content = "更新日志:\n"+changelog,
                CloseButtonText = "关闭",
                PrimaryButtonText = "立即更新",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = sender.XamlRoot,
            };
            var result = await updateDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (UpdateTip.Target != checkDependUpdate) {StartUpdate(); }
                else {  StartDependUpdate(); }
            }
        }

        public async void StartUpdate()
        {
            await InstallerHelper.GetInstaller();
            InstallerHelper.RunInstaller("");
        }

        public async void StartDependUpdate()
        {
            WaitOverlayManager.RaiseWaitOverlay(true, true, "正在更新依赖","请稍等片刻");
            await InstallerHelper.GetInstaller();
            InstallerHelper.RunInstaller("/depend");
            WaitOverlayManager.RaiseWaitOverlay(false);
        }

        public async void StartDependForceUpdate(TeachingTip sender, object args)
        {
            WaitOverlayManager.RaiseWaitOverlay(true, true, "正在强制更新依赖", "请稍等片刻");
            await InstallerHelper.GetInstaller();
            InstallerHelper.RunInstaller("/depend /force");
            WaitOverlayManager.RaiseWaitOverlay(false);
        }


        // 选择下载渠道开始
        private void uservice_Github_Choose(object sender, RoutedEventArgs e)
        {
            AppDataController.SetUpdateService(1);
        }

        private void uservice_Gitee_Choose(object sender, RoutedEventArgs e)
        {
            AppDataController.SetUpdateService(1);
        }

        private void uservice_JSG_Choose(object sender, RoutedEventArgs e)
        {
            AppDataController.SetUpdateService(2);
        }

        private async void Backup_Data(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            string formattedDate = now.ToString("yyyy_MM_dd_HH_mm_ss");
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("Zip Archive", new List<string>() { ".SRToolsBackup" });
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.SuggestedFileName = "SRTools_Backup_" + formattedDate;
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);
            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                string startPath = userDocumentsFolderPath + @"\JSG-LLC\SRTools";
                string zipPath = file.Path;
                if (File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
                ZipFile.CreateFromDirectory(startPath, zipPath);
            }
        }

        private void Restore_Data_Click(object sender, RoutedEventArgs e)
        {
            RestoreTip.IsOpen = true;
        }

        private async void Restore_Data(TeachingTip e, object o)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".SRToolsBackup");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file != null) 
            {
                string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                Task.Run(() => Clear_AllData_NoClose(null, null)).Wait();
                Task.Run(() => ZipFile.ExtractToDirectory(file.Path, userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\")).Wait();
                Application.Current.Exit();
            }
            
        }

        private async void Install_Font_Click(object sender, RoutedEventArgs e)
        {
            installSFF.IsEnabled = false;
            installSFF_Progress.Visibility = Visibility.Visible;
            //int result = await InstallFont.InstallSegoeFluentFontAsync();
            installSFF.Content = "安装字体后重启SRTools即生效";
            installSFF_Progress.Visibility = Visibility.Collapsed;
        }

        // 重启程序
        private async void Restart_App(TeachingTip sender, object args)
        {
            await ProcessRun.RestartApp();
        }


        // Debug_Clicks
        private void Debug_Panic_Click(object sender, RoutedEventArgs e) 
        {
            throw new Exception("全局异常处理测试");
        }

        private void Debug_Notification_Test(object sender, RoutedEventArgs e)
        {
            NotificationManager.RaiseNotification("测试通知","这是一条测试通知", InfoBarSeverity.Success);
        }

        // Debug_Disable_NavBtns
        private void Debug_Disable_NavBtns(object sender, RoutedEventArgs e)
        {
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
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is NavigationView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as NavigationView;
        }

    }
}