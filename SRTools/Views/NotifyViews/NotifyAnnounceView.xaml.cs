﻿using System;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SRTools.Views.NotifyViews
{
    public sealed partial class NotifyAnnounceView : Page
    {
        List<String> list = new List<String>();
        public NotifyAnnounceView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to NotifyAnnounceView", 0);
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("Posts\\announce.json").AsTask().GetAwaiter().GetResult();
            var notify = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            GetNotify getNotify = new GetNotify();
            var records = getNotify.GetData(notify);
            NotifyAnnounceView_List.ItemsSource = records;
            LoadData(records);
        }

        private void LoadData(List<GetNotify> getNotifies)
        {
            foreach (GetNotify getNotify in getNotifies)
            {
                list.Add(getNotify.url);
            }
        }

        private async void List_PointerPressed(object sender, ItemClickEventArgs e)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            string url = list[NotifyAnnounceView_List.SelectedIndex]; // 替换为要打开的网页地址
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            NotifyAnnounceView_List.SelectedIndex = -1;
        }
    }
}