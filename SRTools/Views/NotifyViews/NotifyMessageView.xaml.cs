﻿using System;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;
using System.Linq;
using Windows.Storage;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SRTools.Views.NotifyViews
{
    public sealed partial class NotifyMessageView : Page
    {
        List<String> list = new List<String>();
        public NotifyMessageView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to NotifyMessageView", 0);
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("Posts\\info.json").AsTask().GetAwaiter().GetResult();
            var notify = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            GetNotify getNotify = new GetNotify();
            var records = getNotify.GetData(notify);
            NotifyMessageView_List.ItemsSource = records;
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
            string url = list[NotifyMessageView_List.SelectedIndex]; // 替换为要打开的网页地址
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            NotifyMessageView_List.SelectedIndex = -1;
        }
    }
}