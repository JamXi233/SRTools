using System;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.UI.Xaml.Input;
using System.Net.Http;
using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml;

namespace SRTools.Views.NotifyViews
{
    public sealed partial class BannerView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<string> Pictures { get; } = new ObservableCollection<string>();
        List<String> list = new List<String>();

        public BannerView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to NotifyAnnounceView", 0);
            LoadBanner();
        }

        public async void LoadBanner()
        {
            string apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=64kMb5iAWu&language=zh-cn";
            string responseBody = await FetchOtherData(apiUrl);
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;
                JsonElement banners = root.GetProperty("data").GetProperty("content").GetProperty("banners");
                PopulatePicturesAsync(banners);
            }
        }

        public static async Task<string> FetchOtherData(string url)
        {
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            Logging.Write("FetchData:" + url, 0);
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        public void PopulatePicturesAsync(JsonElement banners)
        {
            foreach (JsonElement banner in banners.EnumerateArray())
            {
                string imgUrl = banner.GetProperty("image").GetProperty("url").GetString();
                string linkUrl = banner.GetProperty("image").GetProperty("link").GetString();
                Pictures.Add(imgUrl);  // 直接添加 URL 到集合
                list.Add(linkUrl);
            }
            FlipViewPipsPager.NumberOfPages = banners.GetArrayLength();  // 一次性设置总页数
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
}
