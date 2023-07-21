using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using SRTools.Depend;
using System;
using System.Diagnostics;
using Windows.Devices.Geolocation;

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

        private void LoadData()
        {
            string executablePath = userDocumentsFolderPath + @"\JSG-LLC\SRTools\Depends\SRToolsHelper\SRToolsHelper.exe";
            string arguments = "/GetReg";

            Process process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            int exitCode = process.ExitCode;
            string returnValue = output.Trim(); // 去掉字符串末尾的换行符和空格
            Logging.Write(returnValue, 3, "SRToolsHelper");
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

        private void ChangeGraphic(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string text = item.Text;

            switch (text.ToString())
            {
                //特殊调节
                case string s when s.Contains("30") || s.Contains("60") || s.Contains("120"):
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue FPS " + s);
                    break;
                case "FXAA":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue AAMode " + 1);
                    break;
                case "TAA":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue AAMode " + 2);
                    break;
                //画质调节
                case "开":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " true");
                    break;
                case "关":
                    if ((String)item.Tag == "EnableVSync")
                    {
                        ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " false");
                    }
                    else
                    {
                        ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " " + 0);
                    }
                    
                    break;
                case "非常低":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " " + 1);
                    break;
                case "低":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " " + 2);
                    break;
                case "中":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " " + 3);
                    break;
                case "高":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " " + 4);
                    break;
                case "非常高":
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue " + item.Tag + " " + 5);
                    break;
                //渲染精度调节
                case string s when s.Contains("."):
                    ProcessRun.RunProcess_Message(userDocumentsFolderPath + "\\JSG-LLC\\SRTools\\Depends\\SRToolsHelper\\SRToolsHelper.exe", "/SetValue RenderScale "+s);
                    break;
            }
            LoadData();

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