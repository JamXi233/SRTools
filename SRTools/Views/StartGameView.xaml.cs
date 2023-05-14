﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartGameView : Page
    {
        public StartGameView()
        {
            this.InitializeComponent();
            BitmapImage image = new BitmapImage(new Uri("https://cdn.jamsg.cn/logo.svg"));
            image.ImageOpened += (sender, e) =>
            {
                backgroundBrush.ImageSource = image;
            };

            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueName = "SRTools_Config_GamePath";
            string valueUnlockFPS = "SRTools_Config_UnlockFPS";
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    var value = key.GetValue(valueName) as string;
                    if (!string.IsNullOrEmpty(value) && value.Contains("Null"))
                    {
                        UpdateUIElementsVisibility(0);
                    }
                    else
                    {
                        UpdateUIElementsVisibility(1);
                    }
                }
                else
                {
                    UpdateUIElementsVisibility(1);
                }
            }
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    var value = key.GetValue(valueUnlockFPS) as string;
                    if (value == "1")
                    {
                        unlockFPS.IsChecked = true;
                    }
                    else if (value == "0")
                    {
                        unlockFPS.IsChecked = false;
                    }
                }
                else
                {
                    unlockFPS.IsChecked = false;
                }
            }


        }
        private async void SelectGame(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            var window = new Window();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
            var file = await picker.PickSingleFileAsync();
            if (file != null && file.Name == "StarRail.exe")
            {
                string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
                string valueGamePath = "SRTools_Config_GamePath";
                string folderPath = @file.Path;
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))
                {
                    key.SetValue(valueGamePath, folderPath, RegistryValueKind.String);
                }
                UpdateUIElementsVisibility(1);
            }
        }

        private async void RMGameLocation(object sender, RoutedEventArgs e)
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueGamePath = "SRTools_Config_GamePath";
            string folderPath = @"Null";
            using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))
            {
                key.SetValue(valueGamePath, folderPath, RegistryValueKind.String);
            }
            UpdateUIElementsVisibility(0);
        }
        //启动游戏
        private void StartGame_Click(object sender, RoutedEventArgs e)
        {
            if (unlockFPS.IsChecked ?? false)
            {
                string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
                string valueName = "GraphicsSettings_Model_h2986158309";
                RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
                byte[] valueBytes = (byte[])key.GetValue(valueName);
                if (key == null || valueBytes == null)
                {
                    NoGraphicsTip.IsOpen = true;
                    return;
                }
                string valueString = Encoding.UTF8.GetString(valueBytes);
                if (!valueString.Contains("\"FPS\":60"))
                {
                    key.Close();
                    StartGame(null, null);
                    return;
                }
                string newValueString = valueString.Replace("{\"FPS\":60", "{\"FPS\":120");
                byte[] newValueBytes = Encoding.UTF8.GetBytes(newValueString);
                key.SetValue(valueName, newValueBytes, RegistryValueKind.Binary);
                key.Close();
                StartGame(null, null);
            }
            else 
            {
                StartGame(null, null);
            }
        }

        private void UnlockFPS_Click(object sender, RoutedEventArgs e) 
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueUnlockFPS = "SRTools_Config_UnlockFPS";
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            RegistryKey key = baseKey.OpenSubKey(keyPath, true);
            if (unlockFPS.IsChecked ?? false) 
            {
                key.SetValue(valueUnlockFPS, 1, RegistryValueKind.String);
            }
            else
            {
                key.SetValue(valueUnlockFPS, 0, RegistryValueKind.String);
            }
        }

        private void UpdateUIElementsVisibility(int status)
        {
            if (status == 0) 
            {
                selectGame.Visibility = Visibility.Visible;
                rmGame.Visibility = Visibility.Collapsed;
                startGame.Visibility = Visibility.Collapsed;
            }
            else
            {
                selectGame.Visibility = Visibility.Collapsed;
                rmGame.Visibility = Visibility.Visible;
                startGame.Visibility = Visibility.Visible;
            }
        }

        private void StartGame(TeachingTip sender, object args)
        {
            var folder = KnownFolders.DocumentsLibrary;
            var srtoolsFolder = folder.GetFolderAsync("JSG-LLC\\SRTools").AsTask().GetAwaiter().GetResult();
            var settingsFile = srtoolsFolder.GetFileAsync("GameLocation.ini").AsTask().GetAwaiter().GetResult();
            var filePath = FileIO.ReadTextAsync(settingsFile).AsTask().GetAwaiter().GetResult();
            var processInfo = new ProcessStartInfo(filePath);
            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas"; // this will prompt the user for admin privileges
            Process.Start(processInfo);
        }

    }
}