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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace SRTools.Views.NotifyViews
{
    public sealed partial class NotifyMessageView : Page
    {
        List<String> list = new List<String>();
        public NotifyMessageView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to NotifyMessageView", 0);
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string SRToolsPostsFolderPath = Path.Combine(documentsPath, "JSG-LLC", "SRTools", "Posts");
            string settingsFilePath = Path.Combine(SRToolsPostsFolderPath, "info.json");
            if (File.Exists(settingsFilePath))
            {
                string notify = File.ReadAllText(settingsFilePath);
                GetNotify getNotify = new GetNotify();
                var records = getNotify.GetData(notify);
                NotifyMessageView_List.ItemsSource = records;
                LoadData(records);
            }
            else
            {
                Logging.Write("Info file not found", 0);
            }
        }

        private void LoadData(List<GetNotify> getNotifies)
        {
            foreach (GetNotify getNotify in getNotifies)
            {
                list.Add(getNotify.link);
            }
        }

        private async void List_PointerPressed(object sender, ItemClickEventArgs e)
        {
            await Task.Delay(TimeSpan.FromSeconds(0.1));
            string url = list[NotifyMessageView_List.SelectedIndex];
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