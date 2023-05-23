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
            Logging.Write("Switch to GachaView", 0);
            switch (localSettings.Values["SRTools_Gacha_Data"])
            {
                case 0:
                    break;
                case 1:
                    LoadData();
                    break;
                default:
                    break;
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
                        ProxyButton.IsEnabled = true;
                        dispatcherTimer.Start();
                    }
                    catch (Exception ex)
                    {
                        GachaLink.Subtitle = ex.Message;
                        GachaLink.IsOpen = true;
                    }
                }
                else
                {
                    // 进程未运行
                    ProxyButton.IsChecked = false;
                    GachaLink.Title = "未运行游戏";
                    GachaLink.Subtitle = "先到启动游戏页面启动游戏后再启动代理";
                    GachaLink.IsOpen = true;
                }
                
            }
            else { Stop(); isProxyRunning = false; }
            
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
                    gacha_status.Text = GachaLink_String;
                    GachaLink.Title = "获取到抽卡记录地址";
                    GachaLink.Subtitle = "正在获取API信息";
                    GachaLink.IsOpen = true;
                    Stop();
                    Logging.Write("Get GachaLink Finish!", 0);
                    ProxyButton.IsChecked = false;
                    DataPackage dataPackage = new DataPackage();
                    dataPackage.SetText(GachaLink_String);
                    Logging.Write("Writing GachaLink in Clipboard...", 0);
                    Clipboard.SetContent(dataPackage);
                    dispatcherTimer.Stop();
                    Logging.Write("Loading Gacha Data...", 0);
                    gacha_status.Text = "Loading Gacha Data...";
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
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = await folder.CreateFolderAsync("JSG-LLC\\SRTools", CreationCollisionOption.OpenIfExists);
            var gachaFile = await srtoolsFolder.CreateFileAsync("GachaRecords.ini", CreationCollisionOption.OpenIfExists);
            // 两个数据
            var serializedList = JsonConvert.SerializeObject(records);//获取到的数据
            var GachaRecords = FileIO.ReadTextAsync(gachaFile).AsTask().GetAwaiter().GetResult();//原来的数据

            if (GachaRecords != "") //如果不为空
            {
                // 反序列化为List<Record>对象
                List<GachaRecords> data1 = JsonConvert.DeserializeObject<List<GachaRecords>>(serializedList);
                List<GachaRecords> data2 = JsonConvert.DeserializeObject<List<GachaRecords>>(GachaRecords);
                // 合并数据，确保ID不重复
                var combinedData = data1.Concat(data2)
                            .GroupBy(r => r.Id)
                            .Select(g => g.First())
                            .ToList();
                string combinedDataJson = JsonConvert.SerializeObject(combinedData);
                // 如果需要，将合并后的数据序列化为JSON字符串
                await FileIO.WriteTextAsync(gachaFile, combinedDataJson);
            }
            else
            {
                await FileIO.WriteTextAsync(gachaFile, serializedList);
            }
            
            localSettings.Values["SRTools_Gacha_Data"] = 1;
            GachaLink.IsOpen = false;
            gacha_status.Text = "API获取完成";
            LoadData();
        }

        private async void LoadData()
        {
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("GachaRecords.ini").AsTask().GetAwaiter().GetResult();
            var GachaRecords = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            var records = await new GachaRecords().GetAllGachaRecordsAsync(null, GachaRecords);
            var groupedRecords = records.GroupBy(r => r.RankType).ToDictionary(g => g.Key, g => g.ToList());
            int RankType5 = records.TakeWhile(r => r.RankType != "5").Count();
            int RankType4 = records.TakeWhile(r => r.RankType != "4").Count();
            string uid = records.Select(r => r.Uid).FirstOrDefault();

            // 筛选出四星和五星的记录
            var rank4Records = records.Where(r => r.RankType == "4");
            var rank5Records = records.Where(r => r.RankType == "5");

            // 按名称进行分组并计算每个分组中的记录数量
            var rank4Grouped = rank4Records.GroupBy(r => r.Name).Select(g => new { Name = g.Key, Count = g.Count() });
            var rank5Grouped = rank5Records.GroupBy(r => r.Name).Select(g => new { Name = g.Key, Count = g.Count() });

            // 输出五星记录
            var rank5TextBlock = new TextBlock { Text = "五星：\n" };
            var rank4TextBlock = new TextBlock { Text = "四星：\n" };
            foreach (var group in rank5Grouped)
            {
                rank5TextBlock.Text += $"{group.Name} x{group.Count}, \n";
            }
            foreach (var group in rank4Grouped)
            {
                rank4TextBlock.Text += $"{group.Name} x{group.Count}, \n";
            }
            MyStackPanel.Children.Clear();
            MyStackPanel2.Children.Clear();
            MyStackPanel2.Children.Add(rank5TextBlock);
            MyStackPanel2.Children.Add(rank4TextBlock);

            MyStackPanel.Children.Add(new TextBlock { Text = $"UID:" + uid });
            foreach (var group in groupedRecords)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{group.Key}星: {group.Value.Count} (相同的有{group.Value.GroupBy(r => r.Name).Count()}个)"
                };
                MyStackPanel.Children.Add(textBlock);
            }
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个5星已经抽了" + RankType5 + "发" });
            MyStackPanel.Children.Add(new TextBlock { Text = $"距离上一个4星已经抽了" + RankType4 + "发" });
            MyListView.ItemsSource = records;
            gacha_status.Text = "已加载本地缓存";
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= CheckProcess;
        }

        
    }
}