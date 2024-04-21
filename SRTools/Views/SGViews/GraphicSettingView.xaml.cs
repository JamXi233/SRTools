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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using SRTools.Depend;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SRTools.Views.SGViews
{
    public sealed partial class GraphicSettingView : Page
    {
        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public GraphicSettingView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to GraphicSettingView", 0);
            LoadData();

        }

        private async void LoadData()
        {
            string output = await ProcessRun.SRToolsHelperAsync("/GetReg");
            string returnValue = output.Trim(); // 去掉字符串末尾的换行符和空格
            // 使用 returnValue 变量进行后续处理
            Config config = JsonConvert.DeserializeObject<Config>(returnValue);

            // 将 config.FPS 的值替换为对应的文本
            switch (config.FPS)
            {
                case 30:
                    DDB_FPS.Content = "30";
                    break;
                case 60:
                    DDB_FPS.Content = "60";
                    break;
                case 120:
                    DDB_FPS.Content = "120";
                    break;
                default:
                    DDB_FPS.Content = config.FPS.ToString();
                    break;
            }

            // 将 config.EnableVSync 的值替换为对应的文本
            if (config.EnableVSync)
            {
                DDB_EnableVSync.Content = "开";
            }
            else
            {
                DDB_EnableVSync.Content = "关";
            }

            // 将 config.AAMode 的值替换为对应的文本
            switch (config.AAMode)
            {
                case 0:
                    DDB_AAMode.Content = "关";
                    break;
                case 1:
                    DDB_AAMode.Content = "FXAA";
                    break;
                case 2:
                    DDB_AAMode.Content = "TAA";
                    break;
                default:
                    DDB_AAMode.Content = config.AAMode.ToString();
                    break;
            }

            // 将 config.BloomQuality 的值替换为对应的文本
            switch (config.BloomQuality)
            {
                case 0:
                    DDB_BloomQuality.Content = "关";
                    break;
                case 1:
                    DDB_BloomQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_BloomQuality.Content = "低";
                    break;
                case 3:
                    DDB_BloomQuality.Content = "中";
                    break;
                case 4:
                    DDB_BloomQuality.Content = "高";
                    break;
                case 5:
                    DDB_BloomQuality.Content = "非常高";
                    break;
                default:
                    DDB_BloomQuality.Content = config.BloomQuality.ToString();
                    break;
            }

            // 将 config.CharacterQuality 的值替换为对应的文本
            switch (config.CharacterQuality)
            {
                case 0:
                    DDB_CharacterQuality.Content = "关";
                    break;
                case 1:
                    DDB_CharacterQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_CharacterQuality.Content = "低";
                    break;
                case 3:
                    DDB_CharacterQuality.Content = "中";
                    break;
                case 4:
                    DDB_CharacterQuality.Content = "高";
                    break;
                case 5:
                    DDB_CharacterQuality.Content = "非常高";
                    break;
                default:
                    DDB_CharacterQuality.Content = config.CharacterQuality.ToString();
                    break;
            }

            // 将 config.EnvDetailQuality 的值替换为对应的文本
            switch (config.EnvDetailQuality)
            {
                case 0:
                    DDB_EnvDetailQuality.Content = "关";
                    break;
                case 1:
                    DDB_EnvDetailQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_EnvDetailQuality.Content = "低";
                    break;
                case 3:
                    DDB_EnvDetailQuality.Content = "中";
                    break;
                case 4:
                    DDB_EnvDetailQuality.Content = "高";
                    break;
                case 5:
                    DDB_EnvDetailQuality.Content = "非常高";
                    break;
                default:
                    DDB_EnvDetailQuality.Content = config.EnvDetailQuality.ToString();
                    break;
            }

            // 将 config.LightQuality 的值替换为对应的文本
            switch (config.LightQuality)
            {
                case 0:
                    DDB_LightQuality.Content = "关";
                    break;
                case 1:
                    DDB_LightQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_LightQuality.Content = "低";
                    break;
                case 3:
                    DDB_LightQuality.Content = "中";
                    break;
                case 4:
                    DDB_LightQuality.Content = "高";
                    break;
                case 5:
                    DDB_LightQuality.Content = "非常高";
                    break;
                default:
                    DDB_LightQuality.Content = config.LightQuality.ToString();
                    break;
            }

            // 将 config.ReflectionQuality 的值替换为对应的文本
            switch (config.ReflectionQuality)
            {
                case 0:
                    DDB_ReflectionQuality.Content = "关";
                    break;
                case 1:
                    DDB_ReflectionQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_ReflectionQuality.Content = "低";
                    break;
                case 3:
                    DDB_ReflectionQuality.Content = "中";
                    break;
                case 4:
                    DDB_ReflectionQuality.Content = "高";
                    break;
                case 5:
                    DDB_ReflectionQuality.Content = "非常高";
                    break;
                default:
                    DDB_ReflectionQuality.Content = config.ReflectionQuality.ToString();
                    break;
            }

            // 将 config.RenderScale 的值替换为对应的文本
            DDB_RenderScale.Content = config.RenderScale;

            // 将 config.SFXQuality 的值替换为对应的文本
            switch (config.SFXQuality)
            {
                case 0:
                    DDB_SFXQuality.Content = "关";
                    break;
                case 1:
                    DDB_SFXQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_SFXQuality.Content = "低";
                    break;
                case 3:
                    DDB_SFXQuality.Content = "中";
                    break;
                case 4:
                    DDB_SFXQuality.Content = "高";
                    break;
                case 5:
                    DDB_SFXQuality.Content = "非常高";
                    break;
                default:
                    DDB_SFXQuality.Content = config.SFXQuality.ToString();
                    break;
            }

            // 将 config.ShadowQuality 的值替换为对应的文本
            switch (config.ShadowQuality)
            {
                case 0:
                    DDB_ShadowQuality.Content = "关";
                    break;
                case 1:
                    DDB_ShadowQuality.Content = "非常低";
                    break;
                case 2:
                    DDB_ShadowQuality.Content = "低";
                    break;
                case 3:
                    DDB_ShadowQuality.Content = "中";
                    break;
                case 4:
                    DDB_ShadowQuality.Content = "高";
                    break;
                case 5:
                    DDB_ShadowQuality.Content = "非常高";
                    break;
                default:
                    DDB_ShadowQuality.Content = config.ShadowQuality.ToString();
                    break;
            }
        }

        private async void ChangeGraphic(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string text = item.Text;

            try
            {
                switch (text)
                {
                    // 特殊调节
                    case string s when s.Contains("30") || s.Contains("60") || s.Contains("120"):
                        await ProcessRun.SRToolsHelperAsync("/SetValue FPS " + s);
                        break;
                    case "FXAA":
                        await ProcessRun.SRToolsHelperAsync("/SetValue AAMode 1");
                        break;
                    case "TAA":
                        await ProcessRun.SRToolsHelperAsync("/SetValue AAMode 2");
                        break;
                    // 画质调节
                    case "开":
                        await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " true");
                        break;
                    case "关":
                        if ((string)item.Tag == "EnableVSync")
                        {
                            await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " false");
                        }
                        else
                        {
                            await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " 0");
                        }
                        break;
                    case "非常低":
                        await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " 1");
                        break;
                    case "低":
                        await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " 2");
                        break;
                    case "中":
                        await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " 3");
                        break;
                    case "高":
                        await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " 4");
                        break;
                    case "非常高":
                        await ProcessRun.SRToolsHelperAsync("/SetValue " + item.Tag + " 5");
                        break;
                    // 渲染精度调节
                    case string s when s.Contains("."):
                        await ProcessRun.SRToolsHelperAsync("/SetValue RenderScale " + s);
                        break;
                }
                LoadData();
            }
            catch (Exception ex)
            {
                Logging.Write($"Error in ChangeGraphic: {ex.Message}", 2);
            }

        }
    }

    public class Config
    {
        public int FPS { get; set; }
        public bool EnableVSync { get; set; }
        public float RenderScale { get; set; }
        public int ResolutionQuality { get; set; }
        public int ShadowQuality { get; set; }
        public int LightQuality { get; set; }
        public int CharacterQuality { get; set; }
        public int EnvDetailQuality { get; set; }
        public int ReflectionQuality { get; set; }
        public int SFXQuality { get; set; }
        public int BloomQuality { get; set; }
        public int AAMode { get; set; }
    }
}