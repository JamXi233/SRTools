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
using System.Diagnostics;
using Windows.Storage;

namespace SRTools.Depend
{
    internal class GameStartUtil
    {
        ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
        public async void StartGame()
        {
            string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            await ProcessRun.SRToolsHelperAsync("/SetValue FPS " + 120);
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            var processInfo = new ProcessStartInfo(gamePath);

            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas";
            Process.Start(processInfo);
        }

        public void StartLauncher()
        {
            string gamePath = localSettings.Values["Config_GamePath"] as string;
            var processInfo = new ProcessStartInfo(gamePath.Replace("StarRail.exe", "..\\launcher.exe"));

            //启动程序
            processInfo.UseShellExecute = true;
            processInfo.Verb = "runas";
            Process.Start(processInfo);
        }
    }
}
