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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SRTools.Depend;
using System;

namespace SRTools.Views.FirstRunViews
{
    public sealed partial class FirstRunExtra : Page
    {
        public FirstRunExtra()
        {
            this.InitializeComponent();
            Logging.Write("Switch to FirstRunExtra", 0);
            AppDataController.SetFirstRunStatus(5);
        }

        private async void Install_Font_Click(object sender, RoutedEventArgs e)
        {
            InstallFontButton.IsEnabled = false;

            SkipButton.IsEnabled = false;
            SkipButton.Visibility = Visibility.Collapsed;
            font_Install_Progress.Visibility = Visibility.Visible;
            font_Install.Visibility = Visibility.Collapsed;

            var progress = new Progress<double>(p =>
            {
                InstallFontProgress.Value = p * 100; // 假设 p 是一个0到1之间的比例
            });

            int result = await InstallFont.InstallSegoeFluentFontAsync(progress);

            //按照回传显示
            if (result == 0)
            {
                InstallFontButton.Content = "字体安装成功";
                Logging.Write("Font installed successfully", 0);
                Frame parentFrame = GetParentFrame(this);
                if (parentFrame != null)
                {
                    parentFrame.Navigate(typeof(FirstRunFinish));
                }
            }
            else
            {
                InstallFontButton.Content = "字体安装失败";
                Logging.Write("Font installed failed", 2);
                SkipButton.Visibility = Visibility.Visible;
                SkipButton.IsEnabled = true;
            }
        }


        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            Frame parentFrame = GetParentFrame(this);
            if (parentFrame != null)
            {
                parentFrame.Navigate(typeof(FirstRunFinish));
            }
        }

        private Frame GetParentFrame(FrameworkElement child)
        {

            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is Frame))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as Frame;
        }
    }
}