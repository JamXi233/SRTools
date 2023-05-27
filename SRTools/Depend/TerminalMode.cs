using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using SRTools.Views;
using System.IO;

namespace SRTools.Depend
{
    internal class TerminalMode
    {

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;

        private Window m_window;

        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public static void ShowConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
        }

        public static void HideConsole()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);
        }

        public async Task<bool> Init(int Mode = 0, int SafeMode = 0, String PanicMessage = "Null",String OtherMessage = null)
        {
            var currentProcess = Process.GetCurrentProcess();
            var hWnd = currentProcess.MainWindowHandle;
            Console.Title = "SRTools TerminalMode";
            Console.Clear();
            if (SafeMode == 0) { 
                if (Mode == 1)
                {
                    var list = new[] { "选择游戏路径", "抽卡分析", "设置", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]", };
                    if (localSettings.Values.ContainsKey("Config_GamePath"))
                    {
                        var value = localSettings.Values["Config_GamePath"] as string;
                        if (!string.IsNullOrEmpty(value) && value.Contains("Null"))
                        {
                            list = new[] { "选择游戏路径", "[Cyan]显示主界面[/]", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]", };
                        }
                        else
                        {
                            list = new[] { "[bold green]开启游戏(120FPS)[/]", "[bold yellow]清除游戏路径[/]", "[Cyan]显示主界面[/]", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]" };
                        }
                    }
                    else
                    {
                        list = new[] { "选择游戏路径", "[Cyan]显示主界面[/]", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]", };
                    }
                    var select = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("[bold green]SRTools[/] 控制台模式")
                                .PageSize(10)
                                .AddChoices(list));
                    StartGameView startGameView = new StartGameView();
                    switch (select)
                    {
                        case "[bold green]开启游戏(120FPS)[/]":
                            startGameView.StartGame(null, null);
                            await Init(1);
                            return false;
                        case "[bold yellow]清除游戏路径[/]":
                            startGameView.RMGameLocation(null, null);
                            await Init(1);
                            return false;
                        case "[bold red]退出SRTools[/]":
                            Application.Current.Exit();
                            return false;
                        case "选择游戏路径":
                            SelectGame();
                            return false;
                        case "[red]退出控制台模式[/]":
                            localSettings.Values["Config_TerminalMode"] = 0;
                            m_window = new MainWindow();
                            m_window.Activate();
                            return false;
                        case "[Cyan]显示主界面[/]":
                            Console.Clear();
                            m_window = new MainWindow();
                            m_window.Activate();
                            return false;
                        default:
                            return false;
                    }
                }
            }
            else 
            {
                Console.Title = "SRTools SafeMode";
                Console.Clear();
                Logging.Write("[red]错误问题：[/]" + PanicMessage,2);
                Logging.Write("其他信息：" + OtherMessage,2);
                var list = new[] { "检查本地设置参数", "[red]清空所有配置文件[/]", "[bold red]退出SRTools[/]" };
                var select = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("\n[bold red]SRTools 安全模式[/]")
                            .PageSize(10)
                            .AddChoices(list));
                switch (select)
                {
                    case "检查本地设置参数":
                        CheckLocalSetting();
                        await Init(0,1, PanicMessage ,"本地设置参数检查完成");
                        return false;
                    case "[red]清空所有配置文件[/]":
                        Clear_AllData(null,null);
                        return false;
                    case "[bold red]退出SRTools[/]":
                        Application.Current.Exit();
                        return false;
                    default:
                        return false;
                }
            }
            return true;
        }

        private async void SelectGame()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            Console.Clear();
            var fileselect = 2;
            Logging.Write("选择游戏文件\n通常位于(游戏根目录\\Game\\StarRail.exe)", 0);
            await AnsiConsole.Status().StartAsync("等待选择文件...", async ctx =>
            {
                var file = await picker.PickSingleFileAsync();
                if (file == null) { fileselect = 1; }
                else if (file.Name == "StarRail.exe")
                {
                    localSettings.Values["Config_GamePath"] = @file.Path;
                    fileselect = 0;
                }
            });
            if (fileselect == 0)
            { await Init(1); }
            else if (fileselect == 1)
            { await Init(1); }
            else
            {
                Logging.Write("选择文件不正确，请确保是StarRail.exe\n等待3秒后重新选择", 2);
                await Task.Delay(TimeSpan.FromSeconds(3));
                SelectGame();
            }
        }

        private void CheckLocalSetting() 
        {
            // 检查并设置 Config_GamePath
            if (!localSettings.Values.ContainsKey("Config_GamePath"))
            {
                localSettings.Values["Config_GamePath"] = 0;
            }

            // 检查并设置 Config_UnlockFPS
            if (!localSettings.Values.ContainsKey("Config_UnlockFPS"))
            {
                localSettings.Values["Config_UnlockFPS"] = 0;
            }

            // 检查并设置 Config_UpdateService
            if (!localSettings.Values.ContainsKey("Config_UpdateService"))
            {
                localSettings.Values["Config_UpdateService"] = 0;
            }

            // 检查并设置 Gacha_Data
            if (!localSettings.Values.ContainsKey("Gacha_Data"))
            {
                localSettings.Values["Gacha_Data"] = 0;
            }

            // 检查并设置 Config_TerminalMode
            if (!localSettings.Values.ContainsKey("Config_TerminalMode"))
            {
                localSettings.Values["Config_TerminalMode"] = 0;
            }
        }

        public void Clear_AllData(object sender, RoutedEventArgs e)
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
            // 获取 LocalData 文件夹的引用
            var localFolder = ApplicationData.Current.LocalFolder;

            // 删除 LocalData 文件夹中的所有子文件夹和文件
            await DeleteFilesAndSubfoldersAsync(localFolder, Close);

            // 需要重新创建删除的 LocalData 文件夹
            await ApplicationData.Current.ClearAsync(ApplicationDataLocality.Local);
        }

        private async Task DeleteFilesAndSubfoldersAsync(StorageFolder folder, String Close)
        {
            // 获取文件夹中的所有文件和子文件夹
            var items = await folder.GetItemsAsync();

            // 遍历所有项目
            foreach (var item in items)
            {
                // 如果项目是文件，则删除它
                if (item is StorageFile file)
                {
                    await file.DeleteAsync();
                }
                // 如果项目是文件夹，则递归删除其中所有文件和子文件夹
                else if (item is StorageFolder subfolder)
                {
                    await DeleteFilesAndSubfoldersAsync(subfolder, Close);

                    // 删除子文件夹本身
                    await subfolder.DeleteAsync();
                }
            }
            if (Close == "1")
            {
                Application.Current.Exit();
            }
        }
    }
}