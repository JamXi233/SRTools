using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SRTools.Depend;

namespace SRTools.Views.SGViews
{
    public sealed partial class GraphicSettingView : Page
    {
        public GraphicSettingView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to GraphicSettingView", 0);
            LoadData();
        }

        private async void LoadData()
        {
            await LoadGraphicsData(false);
            DDB_Main.Visibility = Visibility.Visible;
            DDB_Load.Visibility = Visibility.Collapsed;
        }

        private async Task LoadGraphicsData(bool isForce)
        {
            string output;
            string returnValue;

            if (isForce)
            {
                output = await ProcessRun.SRToolsHelperAsync($"/GetReg {StartGameView.GameRegion}");
                returnValue = output.Trim();
            }
            else
            {
                if (StartGameView.GS is not null)
                {
                    returnValue = StartGameView.GS;
                }
                else
                {
                    output = await ProcessRun.SRToolsHelperAsync($"/GetReg {StartGameView.GameRegion}");
                    returnValue = output.Trim();
                }
            }

            // 使用 returnValue 变量进行后续处理
            JObject config = JObject.Parse(returnValue);

            // 设置UI控件的值
            SetUIValue(config, "FPS", DDB_FPS, new Dictionary<int, string> { { 30, "30" }, { 60, "60" }, { 120, "120" } });
            SetUIValue(config, "EnableVSync", DDB_EnableVSync, new Dictionary<bool, string> { { false, "关" }, { true, "开" } });
            SetUIValue(config, "AAMode", DDB_AAMode, new Dictionary<int, string> { { 0, "关" }, { 1, "FXAA" }, { 2, "TAA" } });
            SetUIValue(config, "BloomQuality", DDB_BloomQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }, { 5, "非常高" } });
            SetUIValue(config, "CharacterQuality", DDB_CharacterQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }, { 5, "非常高" } });
            SetUIValue(config, "EnableSelfShadow", DDB_EnableSelfShadow, new Dictionary<int, string> { { 2, "关" }, { 1, "开" } } );
            SetUIValue(config, "EnvDetailQuality", DDB_EnvDetailQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }, { 5, "非常高" } });
            SetUIValue(config, "LightQuality", DDB_LightQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }, { 5, "非常高" } });
            SetUIValue(config, "ReflectionQuality", DDB_ReflectionQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }, { 5, "非常高" } });
            SetUIValue(config, "RenderScale", DDB_RenderScale);
            SetUIValue(config, "SFXQuality", DDB_SFXQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }});
            SetUIValue(config, "ShadowQuality", DDB_ShadowQuality, new Dictionary<int, string> { { 0, "关" }, { 1, "非常低" }, { 2, "低" }, { 3, "中" }, { 4, "高" }, { 5, "非常高" } });
            SetUIValue(config, "EnableMetalFXSU", DDB_EnableMetalFXSU, new Dictionary<bool, string> { { false, "关" }, { true, "开" } });
        }

        private void SetUIValue(JObject config, string key, DropDownButton button, Dictionary<int, string> map = null)
        {
            if (config.TryGetValue(key, out JToken value))
            {
                if (map != null && value.Type == JTokenType.Integer)
                {
                    if (map.TryGetValue(value.ToObject<int>(), out string text))
                    {
                        button.Content = text;
                    }
                    else
                    {
                        button.Content = value.ToString();
                    }
                }
                else
                {
                    button.Content = value.Type == JTokenType.Integer ? value.ToObject<int>().ToString() : value.ToString();
                }
            }
        }

        private void SetUIValue(JObject config, string key, DropDownButton button, Dictionary<bool, string> map = null)
        {
            if (config.TryGetValue(key, out JToken value))
            {
                if (map != null && value.Type == JTokenType.Boolean)
                {
                    if (map.TryGetValue(value.ToObject<bool>(), out string text))
                    {
                        button.Content = text;
                    }
                    else
                    {
                        button.Content = value.ToString();
                    }
                }
                else
                {
                    button.Content = value.Type == JTokenType.Boolean ? value.ToObject<bool>().ToString() : value.ToString();
                }
            }
        }

        private void SetUIValue(JObject config, string key, DropDownButton button)
        {
            if (config.TryGetValue(key, out JToken value))
            {
                button.Content = value.Type == JTokenType.Integer ? value.ToObject<int>().ToString() : value.ToString();
            }
        }

        private async void ChangeGraphic(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string text = item.Text;
            string tag = item.Tag.ToString();

            try
            {
                // 先应用用户选择
                ApplyUserChoice(text, tag);

                // 异步更新画质设置
                var settingsMap = new Dictionary<string, Dictionary<string, int>>
                {
                    { "FPS", new Dictionary<string, int> { { "30", 30 }, { "60", 60 }, { "120", 120 } } },
                    { "AAMode", new Dictionary<string, int> { { "关", 0 }, { "FXAA", 1 }, { "TAA", 2 } } },
                    { "BloomQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 }, { "非常高", 5 } } },
                    { "CharacterQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 }, { "非常高", 5 } } },
                    { "EnableSelfShadow", new Dictionary<string, int> { { "关", 2 }, { "开", 1 } } },
                    { "EnvDetailQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 }, { "非常高", 5 } } },
                    { "LightQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 }, { "非常高", 5 } } },
                    { "ReflectionQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 }, { "非常高", 5 } } },
                    { "SFXQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 } } },
                    { "ShadowQuality", new Dictionary<string, int> { { "关", 0 }, { "非常低", 1 }, { "低", 2 }, { "中", 3 }, { "高", 4 }, { "非常高", 5 } } }
                };

                var booleanSettingsMap = new Dictionary<string, Dictionary<string, bool>>
                {
                    { "EnableVSync", new Dictionary<string, bool> { { "关", false }, { "开", true } } },
                    { "EnableMetalFXSU", new Dictionary<string, bool> { { "关", false }, { "开", true } } }
                };

                if (settingsMap.ContainsKey(tag) && settingsMap[tag].ContainsKey(text))
                {
                    int value = settingsMap[tag][text];
                    await ProcessRun.SRToolsHelperAsync($"/SetValue {StartGameView.GameRegion} {tag} {value}");
                }
                else if (booleanSettingsMap.ContainsKey(tag) && booleanSettingsMap[tag].ContainsKey(text))
                {
                    bool value = booleanSettingsMap[tag][text];
                    await ProcessRun.SRToolsHelperAsync($"/SetValue {StartGameView.GameRegion} {tag} {value.ToString().ToLower()}");
                }
                else if (double.TryParse(text, out double scaleValue) && tag == "RenderScale")
                {
                    await ProcessRun.SRToolsHelperAsync($"/SetValue {StartGameView.GameRegion} {tag} {scaleValue}");
                }
                else if (text.Contains("30") || text.Contains("60") || text.Contains("120"))
                {
                    await ProcessRun.SRToolsHelperAsync($"/SetValue FPS {StartGameView.GameRegion} {text}");
                }

                // 确保设置完成后重新加载数据
                await LoadGraphicsData(true);
            }
            catch (Exception ex)
            {
                Logging.Write($"Error in ChangeGraphic: {ex.Message}", 2);
            }
        }

        private void ApplyUserChoice(string text, string tag)
        {
            // 根据选择直接更新UI
            switch (tag)
            {
                case "FPS":
                    DDB_FPS.Content = text;
                    break;
                case "EnableVSync":
                    DDB_EnableVSync.Content = text;
                    break;
                case "AAMode":
                    DDB_AAMode.Content = text;
                    break;
                case "BloomQuality":
                    DDB_BloomQuality.Content = text;
                    break;
                case "CharacterQuality":
                    DDB_CharacterQuality.Content = text;
                    break;
                case "EnableSelfShadow":
                    DDB_EnableSelfShadow.Content = text;
                    break;
                case "EnvDetailQuality":
                    DDB_EnvDetailQuality.Content = text;
                    break;
                case "LightQuality":
                    DDB_LightQuality.Content = text;
                    break;
                case "ReflectionQuality":
                    DDB_ReflectionQuality.Content = text;
                    break;
                case "RenderScale":
                    DDB_RenderScale.Content = text;
                    break;
                case "SFXQuality":
                    DDB_SFXQuality.Content = text;
                    break;
                case "ShadowQuality":
                    DDB_ShadowQuality.Content = text;
                    break;
                case "EnableMetalFXSU":
                    DDB_EnableMetalFXSU.Content = text;
                    break;
            }
        }
    }
}
