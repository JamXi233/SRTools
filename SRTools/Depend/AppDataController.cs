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
using Windows.Storage;

namespace SRTools.Depend
{
    class AppDataController
    {
        private const string KeyPath = "SRTools";
        private const string FirstRun = "Config_FirstRun";

        public void FirstRunInit()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            // 检查并写入 Config_DayNight，如果当前为 null
            if (localSettings.Values["Config_DayNight"] == null)
            {
                localSettings.Values["Config_DayNight"] = 0;
                Logging.WriteCustom("AppDataController", "Init Config_DayNight");
            }

            // 检查并写入 Config_GamePath，如果当前为 null
            if (localSettings.Values["Config_GamePath"] == null)
            {
                localSettings.Values["Config_GamePath"] = "Null";  // 这里替换为实际的游戏路径
                Logging.WriteCustom("AppDataController", "Init Config_GamePath");
            }

            // 检查并写入 Config_UpdateService，如果当前为 null
            if (localSettings.Values["Config_UpdateService"] == null)
            {
                localSettings.Values["Config_UpdateService"] = 2;  // 这里替换为实际的更新服务配置
                Logging.WriteCustom("AppDataController", "Init Config_UpdateService");
            }

            // 检查并写入 Config_FirstRun，如果当前为 null
            if (localSettings.Values["Config_FirstRun"] == null)
            {
                localSettings.Values["Config_FirstRun"] = 1;
                Logging.WriteCustom("AppDataController", "Init Config_FirstRun");
            }

            // 检查并写入 Config_FirstRunStatus，如果当前为 null
            if (localSettings.Values["Config_FirstRunStatus"] == null)
            {
                localSettings.Values["Config_FirstRunStatus"] = 0;
                Logging.WriteCustom("AppDataController", "Init Config_FirstRunStatus");
            }

            // 检查并写入 Config_ConsoleMode，如果当前为 null
            if (localSettings.Values["Config_ConsoleMode"] == null)
            {
                localSettings.Values["Config_ConsoleMode"] = 0;
                Logging.WriteCustom("AppDataController", "Init Config_ConsoleMode");
            }

            // 检查并写入 Config_TerminalMode，如果当前为 null
            if (localSettings.Values["Config_TerminalMode"] == null)
            {
                localSettings.Values["Config_TerminalMode"] = 0;
                Logging.WriteCustom("AppDataController", "Init Config_TerminalMode");
            }

        }

        public int CheckOldData()
        {
            // 获取或创建名为 "SRTools" 的设置容器
            ApplicationDataContainer keyContainer = GetOrCreateContainer(KeyPath);
            Logging.WriteCustom("AppDataController", "Checking OldData...");
            // 检查 "Config_FirstRun" 是否存在并且设置为 1
            if (keyContainer.Values.ContainsKey(FirstRun) && keyContainer.Values[FirstRun].ToString() == "0")
            {
                Logging.WriteCustom("AppDataController", "OldData Found");
                return 1;
            }
            Logging.WriteCustom("AppDataController", "OldData Not Found");
            return 0;
        }

        // 获取或创建名为 keyPath 的 ApplicationDataContainer
        private ApplicationDataContainer GetOrCreateContainer(string keyPath)
        {
            var localSettings = ApplicationData.Current.LocalSettings;

            // 检查是否已存在名为 keyPath 的容器
            if (!localSettings.Containers.ContainsKey(keyPath))
            {
                // 如果不存在，创建新容器
                return localSettings.CreateContainer(keyPath, ApplicationDataCreateDisposition.Always);
            }

            // 返回现有容器
            return localSettings.Containers[keyPath];
        }

        public static int GetFirstRun()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            object status = localSettings.Values["Config_FirstRun"];
            return status != null ? Convert.ToInt32(status) : -1;
        }

        public static int GetFirstRunStatus()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            object status = localSettings.Values["Config_FirstRunStatus"];
            return status != null ? Convert.ToInt32(status) : -1;
        }


        public static string GetGamePath()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return (string)localSettings.Values["Config_GamePath"];
        }

        public static int GetUpdateService()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            Logging.Write("GetUpdateService: " + localSettings.Values["Config_UpdateService"], 0);
            return (int)localSettings.Values["Config_UpdateService"];
        }

        public static int GetDayNight()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return (int)localSettings.Values["Config_DayNight"];
        }

        public static int GetConsoleMode()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return (int)localSettings.Values["Config_ConsoleMode"];
        }

        public static int GetTerminalMode()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            return (int)localSettings.Values["Config_TerminalMode"];
        }

        public static int SetFirstRunStatus(int firstRunStatus)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_FirstRunStatus"] = firstRunStatus;
            Logging.WriteCustom("AppDataController", "Set Config_FirstRunStatus");
            return (int)localSettings.Values["Config_FirstRunStatus"];
        }

        public static int SetFirstRun(int firstRun)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_FirstRun"] = firstRun;
            Logging.WriteCustom("AppDataController", "Set Config_FirstRun");
            return (int)localSettings.Values["Config_FirstRun"];
        }

        public static string SetGamePath(string gamePath)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_GamePath"] = gamePath;
            Logging.WriteCustom("AppDataController", "Set Config_GamePath");
            return localSettings.Values["Config_GamePath"].ToString();
        }

        public static string SetUpdateService(int updateService)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = updateService;
            Logging.WriteCustom("AppDataController", "Set Config_UpdateService");
            return localSettings.Values["Config_UpdateService"].ToString();
        }

        public static int SetDayNight(int dayNight)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_DayNight"] = dayNight;
            Logging.WriteCustom("AppDataController", "Set Config_DayNight");
            return (int)localSettings.Values["Config_DayNight"];
        }

        public static int SetConsoleMode(int consoleMode)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_ConsoleMode"] = consoleMode;
            Logging.WriteCustom("AppDataController", "Set Config_ConsoleMode");
            return (int)localSettings.Values["Config_ConsoleMode"];
        }

        public static int SetTerminalMode(int terminalMode)
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_TerminalMode"] = terminalMode;
            Logging.WriteCustom("AppDataController", "Set Config_TerminalMode");
            return (int)localSettings.Values["Config_TerminalMode"];
        }

        public static string RMFirstRunStatus()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_FirstRunStatus"] = 0;
            Logging.WriteCustom("AppDataController", "Remove Config_FirstRunStatus");
            return localSettings.Values["Config_FirstRunStatus"].ToString();
        }

        public static string RMFirstRun()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_FirstRun"] = 1;
            Logging.WriteCustom("AppDataController", "Remove Config_FirstRun");
            return localSettings.Values["Config_FirstRun"].ToString();
        }

        public static string RMGamePath()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_GamePath"] = "Null";
            Logging.WriteCustom("AppDataController", "Remove Config_GamePath");
            return localSettings.Values["Config_GamePath"].ToString();
        }

        public static string RMUpdateService()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_UpdateService"] = "null";
            Logging.WriteCustom("AppDataController", "Remove Config_UpdateService");
            return localSettings.Values["Config_UpdateService"].ToString();
        }

        public static int RMDayNight()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_DayNight"] = 0;
            Logging.WriteCustom("AppDataController", "Remove Config_DayNight");
            return (int)localSettings.Values["Config_DayNight"];
        }

        public static int RMConsoleMode()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_ConsoleMode"] = 0;
            Logging.WriteCustom("AppDataController", "Remove Config_ConsoleMode");
            return (int)localSettings.Values["Config_ConsoleMode"];
        }

        public static int RMTerminalMode()
        {
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values["Config_TerminalMode"] = 0;
            Logging.WriteCustom("AppDataController", "Remove Config_TerminalMode");
            return (int)localSettings.Values["Config_TerminalMode"];
        }
    }
}
