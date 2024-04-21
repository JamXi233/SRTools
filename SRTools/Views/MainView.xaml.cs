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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media.Animation;
using SRTools.Depend;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Input;
using SRTools.Views.NotifyViews;
using System.IO;
using static SRTools.App;
using Windows.Storage;
using SRTools.Views.SGViews;

namespace SRTools.Views
{
    public sealed partial class MainView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<string> Pictures { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> PicturesClick { get; } = new ObservableCollection<string>();
        static Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();

        private string _url;
        string backgroundUrl = "";
        string iconUrl = "";
        List<String> list = new List<String>();
        GetNotify getNotify = new GetNotify();

        public MainView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to MainView", 0);
            Loaded += MainView_Loaded;  // 订阅 Loaded 事件
        }

        private async void MainView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadPicturesAsync();  // 异步加载图片
            await LoadPostAsync();

            try
            {
                await getNotify.Get();  // 异步等待 getNotify.Get() 完成
                Notify_Grid.Visibility = Visibility.Visible;
            }
            catch
            {
                loadRing.Visibility = Visibility.Collapsed;
                loadErr.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadPicturesAsync()
        {
            string apiUrl = "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33";
            ApiResponse response = await FetchData(apiUrl);
            backgroundUrl = response.data.adv.background;
            iconUrl = response.data.adv.icon;
            _url = response.data.adv.url;
            PopulatePicturesAsync(response.data.banner);
            await LoadAdvertisementDataAsync();
        }

        private async Task LoadPostAsync()
        {
            await getNotify.Get();
            NotifyLoad.Visibility = Visibility.Collapsed;
            NotifyNav.Visibility = Visibility.Visible;
            // 查找第一个已启用的MenuItem并将其选中
            foreach (var menuItem in NotifyNav.Items)
            {
                if (menuItem is SelectorBarItem item && item.IsEnabled)
                {
                    NotifyNav.SelectedItem = item;
                    break;
                }
            }
        }

        private async Task LoadAdvertisementDataAsync()
        {
            Logging.Write("LoadAdvertisementData...", 0);
            Logging.Write("Getting Background: " + backgroundUrl, 0);
            BitmapImage backgroundImage = await LoadImageAsync(backgroundUrl);
            BackgroundImage.Source = backgroundImage;

            // 设置按钮图标
            try
            {
                Logging.Write("Getting Button Image: " + iconUrl, 0);
                BitmapImage iconImage = await LoadImageAsync(iconUrl);
                IconImageBrush.ImageSource = iconImage;
            }
            catch (Exception e)
            {
                Logging.Write("Getting Button Image Error: " + e.Message, 0);
            }


            var result = await GetUpdate.GetDependUpdate();
            var status = result.Status;
            if (status == 1)
            {
                NotificationManager.RaiseNotification("更新提示", "依赖包需要更新\n请尽快到[设置-检查依赖更新]进行更新", InfoBarSeverity.Warning);
            }

            loadRing.Visibility = Visibility.Collapsed;
        }


        private void OpenUrlButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开浏览器访问指定URL
            Process.Start(new ProcessStartInfo(_url) { UseShellExecute = true });
        }

        private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            StartFadeAnimation(BackgroundImage, 0, 1, TimeSpan.FromSeconds(0.2));
            StartFadeAnimation(OpenUrlButton, 0, 1, TimeSpan.FromSeconds(0.2));
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

        public static async Task<ApiResponse> FetchData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            Logging.Write("FetchData:"+url, 0);
            httpResponse.EnsureSuccessStatusCode();
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ApiResponse>(responseBody);
        }

        public void PopulatePicturesAsync(List<Banner> banners)
        {
            foreach (Banner banner in banners)
            {
                Pictures.Add(banner.img);  // 直接添加 URL 到集合
                list.Add(banner.url);
            }
            FlipViewPipsPager.NumberOfPages = banners.Count;  // 一次性设置总页数
            Gallery_Grid.Visibility = Visibility.Visible;  // 显示 FlipView
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

        private void Notify_NavView_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
        {
            SelectorBarItem selectedItem = sender.SelectedItem;
            int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
            switch (currentSelectedIndex)
            {
                case 0:
                    NotifyFrame.Navigate(typeof(NotifyAnnounceView));
                    break;
                case 1:
                    NotifyFrame.Navigate(typeof(NotifyNotificationView));
                    break;
                case 2:
                    NotifyFrame.Navigate(typeof(NotifyMessageView));
                    break;
            }
        }

        private async Task<BitmapImage> LoadImageAsync(string imageUrl)
        {
            // 检查缓存中是否已存在图片
            if (imageCache.ContainsKey(imageUrl))
            {
                return imageCache[imageUrl];
            }

            // 从网络加载图片
            BitmapImage bitmapImage = new BitmapImage();
            using (var stream = await new HttpClient().GetStreamAsync(imageUrl))
            using (var memStream = new MemoryStream())
            {
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;
                var randomAccessStream = memStream.AsRandomAccessStream();
                await bitmapImage.SetSourceAsync(randomAccessStream);
            }

            // 将加载的图片添加到缓存中
            imageCache[imageUrl] = bitmapImage;

            return bitmapImage;
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
        public List<Banner> post { get; set; }
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

    public class Post
    {
        public string post_id { get; set; }
        public string type { get; set; }
        public string tittle { get; set; }
        public string url { get; set; }
        public string show_time { get; set; }
        public string order { get; set; }
        public string title { get; set; }
    }
}