using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;

namespace SRTools.Depend
{
    class SReg
    {
        string mainPath = @"Software\miHoYo\崩坏：星穹铁道";
        public int CheckMainReg()
        {
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            RegistryKey key = baseKey.OpenSubKey(mainPath, true);
            if (key == null)
            {
                key = baseKey.CreateSubKey(mainPath);
            }
            key.Close();
            return 0;
        }

        public int CheckReg(String valuePath, String Value, bool Write)
        {
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            RegistryKey key = baseKey.OpenSubKey(mainPath, true);
            if (Write)
            {
                key.GetValue(valuePath);
                if (key.GetValue(valuePath) == null)
                {
                    key.SetValue(valuePath, Value, RegistryValueKind.String);
                }
            }
            else
            {
                if (key.GetValue(valuePath) == null)
                {
                    return 1;
                }
            }
            key.Close();
            return 0;
        }

        public String ReadReg(String valuePath) 
        {
            using (var key = Registry.CurrentUser.OpenSubKey(mainPath))
            {
                var value = key.GetValue(valuePath) as string;
                key.Close();
                return value;
            }
        }

        public int WriteReg(String valuePath,String Value)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(mainPath, true))
            {
                key.SetValue(valuePath, Value, RegistryValueKind.String);
                key.Close();
                return 0;
            }
        }

        public void DeleteRegValue(string keyPath, string valueName)
        {
            RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            RegistryKey key = baseKey.OpenSubKey(keyPath, true);

            if (key.GetValue(valueName) != null)
            {
                key.DeleteValue(valueName);
            }

            key.Close();
        }
    }
}
