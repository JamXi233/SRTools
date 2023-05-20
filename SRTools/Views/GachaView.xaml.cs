using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Titanium.Web.Proxy;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Models;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Net.Http;
using System.Net;
using Microsoft.Win32;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GachaView : Page
    {
        private ProxyServer proxyServer;
        private bool isProxyRunning = false;
        public GachaView()
        {
            this.InitializeComponent();
        }

        private void StartProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProxyButton.IsEnabled) 
            {
                if (isProxyRunning)
                {
                    // 代理服务器已经在运行中
                    return;
                }
                proxyServer = new ProxyServer();
                proxyServer.AddEndPoint(new ExplicitProxyEndPoint(System.Net.IPAddress.Any, 8888));
                proxyServer.BeforeRequest += OnBeforeRequest;

                try
                {
                    // 启动代理服务器
                    proxyServer.Start();
                    SetSystemProxy("127.0.0.1:8888", 1);
                    // 更新UI状态
                    isProxyRunning = true;
                    ProxyButton.Content = "关闭代理";
                }
                catch (Exception ex)
                {
                    GachaLink.Subtitle = ex.Message;
                    GachaLink.IsOpen = true;
                }
            }
            else 
            {
                if (!isProxyRunning)
                {
                    // 代理服务器已经停止
                    return;
                }

                // 停止代理服务器
                proxyServer.Stop();
                SetSystemProxy("127.0.0.1:8888", 0);

                // 更新UI状态
                isProxyRunning = false;
                ProxyButton.Content = "开启代理";
            }
        }


        private async Task OnBeforeRequest(object sender, SessionEventArgs e)
        {
            // 监视所有请求，如果检测到指定的URL，就停止代理服务器并在UI线程上显示URL
            if (e.WebSession.Request.RequestUri.ToString().Contains("getGachaLog"))
            {
                proxyServer.Stop();
                var dispatcher = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().CoreWindow.Dispatcher;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    ProxyButton.IsEnabled = false;
                    StartProxyButton_Click(null, null);
                    GachaLink.Subtitle = "已检测到指定URL：" + e.WebSession.Request.RequestUri.ToString();
                    GachaLink.IsOpen = true;
                    SetSystemProxy("127.0.0.1:8888",0);
                });
            }
        }

        public void SetSystemProxy(string proxyServer, int Enable)
        {
            try
            {
                RegistryKey registry = Registry.CurrentUser.OpenSubKey("Software\\Policies\\Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
                registry.SetValue("ProxyEnable", Enable);
                registry.SetValue("ProxyServer", proxyServer);
                registry.Close();
            }
            catch (Exception ex)
            {
                GachaLink.Title = "错误";
                GachaLink.Subtitle = ex.Message;
                GachaLink.IsOpen = true;
            }
        }
    }
}
