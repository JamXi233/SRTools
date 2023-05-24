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

namespace SRTools.Depend
{
    internal class TermianlMode
    {
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public async Task<bool> Init() 
        {
            AllocConsole();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            Console.SetWindowSize(50,20);
            Console.SetBufferSize(50,20);
            Console.Title = "SRTools TerminalMode";
            Console.Clear();
            var list = new[] { "选择游戏路径", "抽卡分析", "设置", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]", };
            if (localSettings.Values.ContainsKey("Config_GamePath"))
            {
                var value = localSettings.Values["Config_GamePath"] as string;
                if (!string.IsNullOrEmpty(value) && value.Contains("Null"))
                {
                    list = new[] { "选择游戏路径", "抽卡分析", "设置", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]", };
                }
                else
                {
                    list = new[] { "[bold green]开启游戏(120FPS)[/]", "[bold yellow]清除游戏路径[/]", "抽卡分析", "设置", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]" };
                }
            }
            else
            {
                list = new[] { "选择游戏路径", "抽卡分析", "设置", "[red]退出控制台模式[/]", "[bold red]退出SRTools[/]", };
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
                    Init();
                    return false;
                case "[bold yellow]清除游戏路径[/]":
                    startGameView.RMGameLocation(null, null);
                    Init();
                    return false;
                case "[bold red]退出SRTools[/]":
                    Application.Current.Exit();
                    return false;
                case "选择游戏路径":
                    SelectGame();
                    return false;
                case "[red]退出控制台模式[/]":
                    localSettings.Values["Config_TerminalMode"] = 0;
                    FreeConsole();
                    return true;
                default:
                    return true;
            }

        }

        private async void SelectGame()
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            Console.Clear();
            Logging.Write("选择游戏文件\n通常位于(游戏根目录\\Game\\StarRail.exe)", 0);
            await AnsiConsole.Status().StartAsync("等待选择文件...", async ctx => { 
                var file = await picker.PickSingleFileAsync();
                if (file == null)
                {
                    Init();
                }
                else if (file.Name == "StarRail.exe")
                {
                    localSettings.Values["Config_GamePath"] = @file.Path;
                    
                    Init();
                }
                Logging.Write("选择文件不正确，请确保是StarRail.exe", 2);
                await Task.Delay(TimeSpan.FromSeconds(3));
            });
            SelectGame();
        }
    }
}