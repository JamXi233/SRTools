using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Win32;
using SRTools.Depend;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Security.Policy;
using Microsoft.UI.Xaml.Input;

namespace SRTools.Views
{
    public sealed partial class MainView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<string> Pictures { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PicturesClick { get; } = new ObservableCollection<string>();
        private string _url;
        string backgroundUrl = "";
        string iconUrl = "";
        List<String> list = new List<String>();

        public MainView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to MainView", 0);
            _ = LoadPicturesAsync(); // 使用丢弃以避免警告
        }

        private void LoadAdvertisementData()
        {

            // 设置背景图片
            Logging.Write("Getting Background: "+ backgroundUrl, 0);
            BitmapImage backgroundImage = new BitmapImage(new Uri(backgroundUrl));
            BackgroundImage.Source = backgroundImage;

            // 设置按钮图标
            Logging.Write("Getting Button Image: "+iconUrl, 0);
            BitmapImage iconImage = new BitmapImage(new Uri(iconUrl));
            IconImageBrush.ImageSource = iconImage;
        }

        private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开浏览器访问指定URL
            Logging.Write("Open Browser URL["+_url+"]", 0);
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }
        private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            StartFadeAnimation(BackgroundImage, 0, 1, TimeSpan.FromSeconds(0.1));
            StartFadeAnimation(OpenUrlButton, 0, 1, TimeSpan.FromSeconds(0.1));
        }

        private void StartFadeAnimation(FrameworkElement target, double from, double to, TimeSpan duration)
        {
            DoubleAnimation opacityAnimation = new DoubleAnimation
            {
                From = from,
                To = to,
                Duration = duration,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(opacityAnimation, target);
            Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

            Storyboard storyboard = new Storyboard();
            storyboard.Children.Add(opacityAnimation);
            storyboard.Begin();
        }

        private async Task LoadPicturesAsync()
        {
            string apiUrl = "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33";
            ApiResponse response = await FetchData(apiUrl);
            backgroundUrl = response.data.adv.background;
            iconUrl = response.data.adv.icon;
            _url = response.data.adv.url;
            PopulatePictures(response.data.banner);
            LoadAdvertisementData();
        }

        public static async Task<ApiResponse> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseBody);
        }

        public void PopulatePictures(List<Banner> banners)
        {
            
            foreach (Banner banner in banners)
            {
                Pictures.Add(banner.img);
                list.Add(banner.url);
            }
            FlipViewPipsPager.Visibility = Visibility.Visible;
            
        }

        private void Gallery_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            // 获取当前选中的图片
            int selectedPicture = Gallery.SelectedIndex;

            // 如果选中了图片，则打开浏览器并导航到指定的网页
            string url = list[selectedPicture]; // 替换为要打开的网页地址
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }



    }

    public class ApiResponse
    {
        public int retcode { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }

    public class Data
    {
        public Adv adv { get; set; }
        public List<Banner> banner { get; set; }
    }

    public class Adv
    {
        public string background { get; set; }
        public string icon { get; set; }
        public string url { get; set; }
        public string version { get; set; }
        public string bg_checksum { get; set; }
    }

    public class Banner
    {
        public string banner_id { get; set; }
        public string name { get; set; }
        public string img { get; set; }
        public string url { get; set; }
        public string order { get; set; }
    }
}