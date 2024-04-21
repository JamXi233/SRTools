﻿// Copyright (c) 2021-2024, JamXi JSG-LLC.
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