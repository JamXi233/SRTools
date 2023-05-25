// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Popups;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    /// 

    
    

    public partial class App : Application
    {
        // 导入 AllocConsole 和 FreeConsole 函数
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
        GetNotify getNotify = new GetNotify();
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Windows.ApplicationModel.Core.CoreApplication.UnhandledErrorDetected += OnUnhandledErrorDetected;
            Init();
        }

        public async void Init()
        {
            Task.Run(() => getNotify.Get()).Wait();
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            switch (localSettings.Values["Config_TerminalMode"])
            {
                case 0:
                    m_window = new MainWindow();
                    m_window.Activate();
                    break;
                case 1:
                    TerminalMode terminalMode = new TerminalMode();
                    bool response = await terminalMode.Init();
                    if (response)
                    {
                        m_window = new MainWindow();
                        m_window.Activate();
                    }
                    break;
                default:
                    m_window = new MainWindow();
                    m_window.Activate();
                    break;
            }
        }


        private async void OnUnhandledErrorDetected(object sender, Windows.ApplicationModel.Core.UnhandledErrorDetectedEventArgs e)
        {
            e.UnhandledError.Propagate();
            // 获取异常信息
            
            AllocConsole();
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            Console.SetWindowSize(50, 20);
            Console.SetBufferSize(50, 20);
            Console.Title = "SRTools TerminalMode";
            
            // 显示错误消息框
            var dialog = new MessageDialog("An error has occurred. The application will now close.");
            await dialog.ShowAsync();

            
            // 处理异常并在控制台上输出错误消息
            Console.WriteLine("发生全局异常: " + e.ToString());

            // 关闭应用程序
            Application.Current.Exit();
        }


        private Window m_window;
    }
}
