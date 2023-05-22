using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;
using Fiddler;
using Microsoft.UI.Dispatching;
using System.Threading;
using Windows.ApplicationModel.DataTransfer;
using static System.Net.Mime.MediaTypeNames;
using SRTools.Depend;
using System.Linq;
using System.Security.Policy;
using Windows.Storage;
using Newtonsoft.Json;

namespace SRTools.Views
{
    public sealed partial class GachaView : Page
    {
        public bool isProxyRunning;
        public String GachaLink_String;
        BCCertMaker.BCCertMaker certProvider = new BCCertMaker.BCCertMaker();
        private DispatcherQueueTimer dispatcherTimer;
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

        public GachaView()
        {
            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            // 创建定时器，并设置回调函数和时间间隔
            dispatcherTimer = dispatcherQueue.CreateTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(2);
            dispatcherTimer.Tick += CheckProcess;
            this.InitializeComponent();
            if (localSettings.Values.ContainsKey("SRTools_Gacha_Data"))
            {
                LoadData();
            }
        }

        private FiddlerCoreStartupSettings startupSettings;
        private Proxy oProxy;


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
                oProxy = FiddlerApplication.CreateProxyEndpoint(8888, true, "127.0.0.1");
            });
        }

        public void Stop()
        {
            FiddlerApplication.Shutdown();
            if (oProxy != null)
            {
                oProxy.Dispose();
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

        private async void StartProxyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CertMaker.oCertProvider = certProvider;
                string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC\\SRTools");
                string filePath = Path.Combine(folderPath, "RootCertificate.p12");
                string rootCertificatePassword = "S0m3T0pS3cr3tP4ssw0rd";
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                if (!File.Exists(filePath))
                {
                    certProvider.CreateRootCertificate();
                    certProvider.WriteRootCertificateAndPrivateKeyToPkcs12File(filePath, rootCertificatePassword);
                }
                certProvider.ReadRootCertificateAndPrivateKeyFromPkcs12File(filePath, rootCertificatePassword);
                if (!CertMaker.rootCertIsTrusted())
                {
                    CertMaker.trustRootCert();
                }
                isProxyRunning = true;
                await StartAsync();
                ProxyButton.IsEnabled = true;
                dispatcherTimer.Start();
            }
            catch (Exception ex)
            {
                GachaLink.Subtitle = ex.Message;
                GachaLink.IsOpen = true;
            }
        }

        private void StopProxyButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProxyRunning)
            {
                isProxyRunning = false;
                GachaLink.Subtitle = "代理服务器已关闭";
                GachaLink.IsOpen = true;
                Stop();
                ProxyButton.IsChecked = false;
            }
        }
        // 定时器回调函数，检查进程是否正在运行
        private void CheckProcess(DispatcherQueueTimer timer, object e)
        {
            if (isProxyRunning)
            {
                ProxyButton.IsChecked = true;
                // 进程正在运行
                if (GachaLink_String != null && GachaLink_String.Contains("getGachaLog"))
                {
                    GachaLink_Box.Text = GachaLink_String;
                    GachaLink.Title = "获取到抽卡记录地址";
                    GachaLink.Subtitle = "已复制到剪贴板";
                    GachaLink.IsOpen = true;
                    Stop();
                    ProxyButton.IsChecked = false;
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.SetText(GachaLink_String);
                    Clipboard.SetContent(dataPackage);
                    dispatcherTimer.Stop();
                    LoadDataAsync(GachaLink_String);
                    GachaLink_String = null;
                }
            }
            else
            {
                ProxyButton.IsChecked = false;
            }
        }

        private async void LoadDataAsync(String url)
        {
            var records = await new GachaRecords().GetAllGachaRecordsAsync(url);
            var groupedRecords = records.GroupBy(r => r.RankType).ToDictionary(g => g.Key, g => g.ToList());
            int RankType5 = records.TakeWhile(r => r.RankType != "5").Count();
            int RankType4 = records.TakeWhile(r => r.RankType != "4").Count();
            string uid = records.Select(r => r.Uid).FirstOrDefault();
            foreach (var group in groupedRecords)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{group.Key}星: {group.Value.Count} (相同的有{group.Value.GroupBy(r => r.Name).Count()}个)"
                };
                MyStackPanel.Children.Add(textBlock);
            }
            MyStackPanel.Children.Add(new TextBlock { Text = $"UID:" + uid });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个5星已经抽了" + RankType5 + "发" });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个4星已经抽了" + RankType4 + "发" });
            MyListView.ItemsSource = records;
            var serializedList = JsonConvert.SerializeObject(records);
            try { localSettings.Values["SRTools_Gacha_Data"] = serializedList; } catch (Exception e) 
            {
                GachaLink.Title = "保存记录失败";
                GachaLink.Subtitle = "请等待更新";
                GachaLink.IsOpen = true;
            }
        }

        private async void LoadData()
        {
            var records = await new GachaRecords().GetAllGachaRecordsAsync(localSettings.Values["SRTools_Gacha_Data"] as string);
            var groupedRecords = records.GroupBy(r => r.RankType).ToDictionary(g => g.Key, g => g.ToList());
            int RankType5 = records.TakeWhile(r => r.RankType != "5").Count();
            int RankType4 = records.TakeWhile(r => r.RankType != "4").Count();
            string uid = records.Select(r => r.Uid).FirstOrDefault();
            foreach (var group in groupedRecords)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{group.Key}星: {group.Value.Count} (相同的有{group.Value.GroupBy(r => r.Name).Count()}个)"
                };
                MyStackPanel.Children.Add(textBlock);
            }
            MyStackPanel.Children.Add(new TextBlock { Text = $"UID:" + uid });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个5星已经抽了" + RankType5 + "发" });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个4星已经抽了" + RankType4 + "发" });
            MyListView.ItemsSource = records;
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= CheckProcess;
        }
    }
}