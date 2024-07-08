using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using static SRTools.App;
using System.Text.RegularExpressions;
using System.IO;
using SRTools.Views;

namespace SRTools.Depend
{
    public static class Region
    {
        public static async Task GetRegion(bool isFirst)
        {
            string gameFile = AppDataController.GetGamePath();
            string fileAssemblyPath = @gameFile.Replace("StarRail.exe", "GameAssembly.dll");
            Logging.Write(fileAssemblyPath);
            try
            {
                // 创建一个X509Certificate2对象，并加载文件的数字证书
                X509Certificate2 cert = new X509Certificate2(fileAssemblyPath);

                // 解析Subject字符串以获取CN部分
                string[] parts = cert.Subject.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                string signerName = "";
                foreach (var part in parts)
                {
                    if (part.Trim().StartsWith("CN="))
                    {
                        signerName = part.Trim().Substring(3);
                        break;
                    }
                }
                if (signerName == "\"miHoYo Co.,Ltd.\"") 
                {
                    if (await isBili()) 
                    { 
                        if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测到B服\n" + signerName, InfoBarSeverity.Success, true, 3); 
                        StartGameView.GameRegion = "Bili"; 
                    }
                    else 
                    {
                        if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测到国服\n" + signerName, InfoBarSeverity.Success, true, 3); 
                        StartGameView.GameRegion = "CN"; 
                    }
                }
                else if (signerName == "COGNOSPHERE PTE. LTD.") 
                {
                    if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测到国际服\n" + signerName, InfoBarSeverity.Success, true, 3);
                    StartGameView.GameRegion = "Global"; 
                }
                Logging.Write(signerName);
            }
            catch (CryptographicException ex)
            {
                if (isFirst) NotificationManager.RaiseNotification("游戏路径读取完成", "检测区服失败", InfoBarSeverity.Warning, true, 5);
                Logging.Write($"{ex.Message}", 2);
            }
        }

        private static async Task<bool> isBili()
        {
            try
            {
                var gameDirectory = AppDataController.GetGamePathWithoutGameName();
                if (gameDirectory != null)
                {
                    string configFilePath = Path.Combine(gameDirectory, "config.ini");

                    if (File.Exists(configFilePath))
                    {
                        string[] lines = await File.ReadAllLinesAsync(configFilePath);
                        bool inGeneralSection = false;
                        bool cpsIsBilibiliPC = false;

                        foreach (string line in lines)
                        {
                            if (line.Trim() == "[General]")
                            {
                                inGeneralSection = true;
                            }
                            else if (inGeneralSection)
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    // 离开[General]部分
                                    inGeneralSection = false;
                                }
                                else if (line.StartsWith("cps="))
                                {
                                    string cpsValue = line.Substring("cps=".Length).Trim();
                                    if (cpsValue == "bilibili_PC")
                                    {
                                        cpsIsBilibiliPC = true;
                                    }
                                }
                                else if (line.StartsWith("game_version="))
                                {
                                    // 使用正则表达式提取版本号
                                    Match match = Regex.Match(line, @"\d+(\.\d+)+");
                                    if (match.Success)
                                    {
                                        Logging.Write($"StarRail Current Version: {match.Value}");
                                        if (cpsIsBilibiliPC)
                                        {
                                            return true;
                                        }
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Logging.Write("config.ini 文件不存在.");
                    }
                }
                else
                {
                    Logging.Write("无法获取游戏目录.");
                }
            }
            catch (Exception ex)
            {
                Logging.Write($"获取游戏版本时出现错误: {ex.Message}");
            }
            return false;
        }
    }
}
