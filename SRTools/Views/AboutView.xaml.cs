using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SRTools.Views
{
    public sealed partial class AboutView : Page
    {
        //public AboutView()
        //{
        //this.InitializeComponent();
        // 从注册表中读取一个名为 valueName 的 32 位 DWORD 类型的 16 进制格式的值，并将其分配给 Slider 控件的 Value 属性
        //string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
        //string valueName = "AudioSettings_MasterVolume_h1622207037";
          //  RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath);
           // if (key != null)
            //{
             //   object value = key.GetValue(valueName);
              //  if (value != null)
               // {
                 //   int intValue = Convert.ToInt32(value.ToString(), 16);
                   // volumeSlider.Value = intValue;
                //}
            //}
        //}
        private void SetVolume(object sender, RangeBaseValueChangedEventArgs e)
        {
            string keyPath = @"Software\miHoYo\崩坏：星穹铁道";
            string valueName = "AudioSettings_MasterVolume_h1622207037";
            RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true);
            key.SetValue(valueName, volumeSlider.Value, RegistryValueKind.DWord);
            key.Close();
        }

        private Dictionary<string, object> settings = new Dictionary<string, object>()
        {
            {"FPS", 60},
            {"EnableVSync", false},
            {"RenderScale", 1.0},
            {"ShadowQuality", 5},
            {"LightQuality", 5},
            {"CharacterQuality", 5},
            {"EnvironmentQuality", 5},
            {"ReflectionQuality", 5},
            {"BloomQuality", 5},
            {"AAMode", 1}
        };

        public AboutView()
        {
            InitializeComponent();

            // ComboBox items for FPS
            var fpsItems = new List<int>() { 60, 120 };
            FPSComboBox.ItemsSource = fpsItems;
            FPSComboBox.SelectedValue = settings["FPS"];

            // ToggleSwitch for EnableVSync
            EnableVSyncToggleSwitch.IsOn = (bool)settings["EnableVSync"];

            // ComboBox items for RenderScale
            var renderScaleItems = new List<double>() { 1.0, 2.0, 3.0, 4.0, 5.0 };
            RenderScaleComboBox.ItemsSource = renderScaleItems;
            RenderScaleComboBox.SelectedValue = settings["RenderScale"];

            // ComboBox items for ShadowQuality
            var shadowQualityItems = new List<int>() { 0, 2, 3, 5 };
            ShadowQualityComboBox.ItemsSource = shadowQualityItems;
            ShadowQualityComboBox.SelectedValue = settings["ShadowQuality"];

            // ComboBox items for LightQuality
            var lightQualityItems = new List<int>() { 0, 1, 3, 4, 5 };
            LightQualityComboBox.ItemsSource = lightQualityItems;
            LightQualityComboBox.SelectedValue = settings["LightQuality"];

            // ComboBox items for CharacterQuality
            var characterQualityItems = new List<int>() { 2, 3, 4 };
            CharacterQualityComboBox.ItemsSource = characterQualityItems;
            CharacterQualityComboBox.SelectedValue = settings["CharacterQuality"];

            // ComboBox items for EnvironmentQuality
            var environmentQualityItems = new List<int>() { 1, 2, 3, 4, 5 };
            EnvironmentQualityComboBox.ItemsSource = environmentQualityItems;
            EnvironmentQualityComboBox.SelectedValue = settings["EnvironmentQuality"];

            // ComboBox items for ReflectionQuality
            var reflectionQualityItems = new List<int>() { 1, 2, 3, 4, 5 };
            ReflectionQualityComboBox.ItemsSource = reflectionQualityItems;
            ReflectionQualityComboBox.SelectedValue = settings["ReflectionQuality"];

            // ComboBox items for BloomQuality
            var bloomQualityItems = new List<int>() { 0, 1, 2, 3, 4, 5 };
            BloomQualityComboBox.ItemsSource = bloomQualityItems;
            BloomQualityComboBox.SelectedValue = settings["BloomQuality"];

            // ComboBox items for AAMode
            var aaModeItems = new List<int>() { 0, 1, 2 };
            AAModeComboBox.ItemsSource = aaModeItems;
            AAModeComboBox.SelectedValue = settings["AAMode"];
        }

        // Save button click event handler
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            settings["FPS"] = FPSComboBox.SelectedValue;
            settings["EnableVSync"] = EnableVSyncToggleSwitch.IsOn;
            settings["RenderScale"] = RenderScaleComboBox.SelectedValue;
            settings["ShadowQuality"] = ShadowQualityComboBox.SelectedValue;
            settings["LightQuality"] = LightQualityComboBox.SelectedValue;
            settings["CharacterQuality"] = CharacterQualityComboBox.SelectedValue;
            settings["EnvironmentQuality"] = EnvironmentQualityComboBox.SelectedValue;
            settings["ReflectionQuality"] = ReflectionQualityComboBox.SelectedValue;
            settings["BloomQuality"] = BloomQualityComboBox.SelectedValue;
            settings["AAMode"] = AAModeComboBox.SelectedValue;

            // TODO: Save the settings to the registry
        }

        // Load button click event handler
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Load the settings from the registry
            // and update the UI controls with the loaded values
        }
    }
}