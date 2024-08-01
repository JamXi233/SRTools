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
using Microsoft.UI.Xaml.Media.Imaging;

namespace SRTools.Views.NotifyViews
{
    public sealed partial class BannerView : Page
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public ObservableCollection<BitmapImage> Pictures { get; } = new ObservableCollection<BitmapImage>();
        private List<string> list = new List<string>();
        private readonly string imageFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "JSG-LLC", "SRTools", "images", "banners");
        private static Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();
        private readonly BitmapImage placeholderImage = new BitmapImage(new Uri("ms-appx:///Assets/placeholder.png"));

        public BannerView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to NotifyAnnounceView", 0);
            LoadBanner();
        }

        public async void LoadBanner()
        {
            Logging.Write("Start loading banners", 0);
            string apiUrl = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=64kMb5iAWu&language=zh-cn";
            string responseBody = await FetchOtherData(apiUrl);
            using (JsonDocument doc = JsonDocument.Parse(responseBody))
            {
                JsonElement root = doc.RootElement;
                JsonElement banners = root.GetProperty("data").GetProperty("content").GetProperty("banners");
                await PopulatePicturesAsync(banners);
            }
            Logging.Write("Finished loading banners", 0);
        }

        public static async Task<string> FetchOtherData(string url)
        {
            Logging.Write("Fetching data from: " + url, 0);
            HttpResponseMessage httpResponse = await httpClient.GetAsync(url);
            httpResponse.EnsureSuccessStatusCode();
            string responseBody = await httpResponse.Content.ReadAsStringAsync();
            Logging.Write("Data fetched successfully", 0);
            return responseBody;
        }

        public async Task PopulatePicturesAsync(JsonElement banners)
        {
            Logging.Write("Populating pictures", 0);
            if (!Directory.Exists(imageFolderPath))
            {
                Logging.Write("Creating image folder: " + imageFolderPath, 0);
                Directory.CreateDirectory(imageFolderPath);
            }

            // 添加占位符图片
            for (int i = 0; i < banners.GetArrayLength(); i++)
            {
                Pictures.Add(placeholderImage);
            }

            int index = 0;
            foreach (JsonElement banner in banners.EnumerateArray())
            {
                string imgUrl = banner.GetProperty("image").GetProperty("url").GetString();
                string linkUrl = banner.GetProperty("image").GetProperty("link").GetString();
                Logging.Write($"Loading image from URL: {imgUrl}", 0);
                BitmapImage image = await LoadImageAsync(imgUrl);  // 加载图片
                Pictures[index] = image;  // 替换占位符
                list.Add(linkUrl);
                index++;
                Logging.Write($"Image loaded and replaced at index {index}", 0);
            }

            FlipViewPipsPager.NumberOfPages = banners.GetArrayLength();  // 一次性设置总页数
            Logging.Write("Finished populating pictures", 0);
        }

        private async Task<BitmapImage> LoadImageAsync(string imageUrl)
        {
            string fileName = Path.GetFileName(imageUrl);
            string filePath = Path.Combine(imageFolderPath, fileName);
            BitmapImage bitmapImage = new BitmapImage();

            bool needDownload = !File.Exists(filePath);

            if (!needDownload)
            {
                try
                {
                    Logging.Write($"Loading image from local file: {filePath}", 0);
                    using (var stream = File.OpenRead(filePath))
                    {
                        await bitmapImage.SetSourceAsync(stream.AsRandomAccessStream());
                        imageCache[imageUrl] = bitmapImage;
                        Logging.Write("Image loaded from local file successfully", 0);
                        return bitmapImage;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Write($"Error loading local image: {ex.Message}", 2);
                    needDownload = true; // 文件存在但损坏，需重新下载
                }
            }

            if (needDownload)
            {
                try
                {
                    Logging.Write($"Downloading image from URL: {imageUrl}", 0);
                    byte[] imageData = await httpClient.GetByteArrayAsync(imageUrl);
                    await File.WriteAllBytesAsync(filePath, imageData);

                    using (var memStream = new MemoryStream(imageData))
                    {
                        memStream.Position = 0;
                        await bitmapImage.SetSourceAsync(memStream.AsRandomAccessStream());
                    }

                    imageCache[imageUrl] = bitmapImage;
                    Logging.Write("Image downloaded and saved successfully", 0);
                }
                catch (Exception ex)
                {
                    Logging.Write($"Error downloading image from {imageUrl}: {ex.Message}", 2);
                }
            }

            return bitmapImage;
        }

        private void Gallery_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            int selectedPicture = Gallery.SelectedIndex;
            string url = list[selectedPicture];
            Logging.Write($"Opening URL: {url}", 0);
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
