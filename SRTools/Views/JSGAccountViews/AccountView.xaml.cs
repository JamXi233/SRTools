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
using SRTools.Depend;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using static SRTools.App;

namespace SRTools.Views.JSGAccountViews
{
    public sealed partial class AccountView : Page
    {
        public AccountView()
        {
            this.InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox usernameTextBox;
            PasswordBox passwordBox;
            usernameTextBox = new TextBox
            {
                Header = "用户名",
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10),
                PlaceholderText = "请填写用户名"
            };
            passwordBox = new PasswordBox
            {
                Header = "密码",
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10),
                PlaceholderText = "请填写密码"
            };
            ContentDialog loginDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Microsoft.UI.Xaml.Style,
                Title = "登录JSG-Account",
                Content = new StackPanel
                {
                    Children = { usernameTextBox, passwordBox }
                },
                PrimaryButtonText = "提交",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await loginDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 登录对话框提交时的处理
                if (result == ContentDialogResult.Primary)
                {
                    string token = await LoginUser(
                        usernameTextBox.Text, passwordBox.Password);
                    if (string.IsNullOrEmpty(token))
                    {
                        // 显示错误消息
                        var errorDialog = new ContentDialog
                        {
                            XamlRoot = this.XamlRoot,
                            Title = "登录失败",
                            Content = "用户名或密码不正确。",
                            CloseButtonText = "确定"
                        };
                        await errorDialog.ShowAsync();
                    }
                    else
                    {
                        NotificationManager.RaiseNotification("登陆成功","Token为:"+token,InfoBarSeverity.Success);
                    }
                }
            }
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            TextBox emailTextBox;
            TextBox usernameTextBox;
            PasswordBox passwordBox;
            PasswordBox confirmPasswordBox;
            emailTextBox = new TextBox
            {
                Header = "邮箱",
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10),
                PlaceholderText = "请填写邮箱"
            };
            usernameTextBox = new TextBox
            {
                Header = "用户名",
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10),
                PlaceholderText = "请填写用户名"
            };
            passwordBox = new PasswordBox
            {
                Header = "密码",
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10),
                PlaceholderText = "请填写密码"
            };
            confirmPasswordBox = new PasswordBox
            {
                Header = "确认密码",
                PlaceholderText = "请重复密码"
            };

            ContentDialog registerDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Style = Application.Current.Resources["DefaultContentDialogStyle"] as Microsoft.UI.Xaml.Style,
                Title = "注册JSG-Account",
                Content = new StackPanel
                {
                    Children = { emailTextBox, usernameTextBox, passwordBox, confirmPasswordBox }
                },
                PrimaryButtonText = "注册",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await registerDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 确保密码匹配
                if (passwordBox.Password != confirmPasswordBox.Password)
                {
                    var errorDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "错误",
                        Content = "密码不匹配。",
                        CloseButtonText = "确定"
                    };
                    await errorDialog.ShowAsync();
                    return;
                }

                // 调用注册API
                bool isRegistered = await RegisterUser(emailTextBox.Text, usernameTextBox.Text, passwordBox.Password);
                if (!isRegistered)
                {
                    // 显示错误消息
                    var errorDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "注册失败",
                        Content = "用户名或邮箱已存在。",
                        CloseButtonText = "确定"
                    };
                    await errorDialog.ShowAsync();
                }
                else
                {
                    // 处理注册成功的情况
                }
            }
        }


        private async Task<bool> RegisterUser(string email, string username, string password)
        {
            var client = new HttpClient();
            var requestContent = new StringContent(JsonSerializer.Serialize(new
            {
                email = email,
                username = username,
                password = password,
                nickname = username  // 假设昵称和用户名相同
            }), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("", requestContent);
                if (response.IsSuccessStatusCode)
                {
                    return true; // 注册成功
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                    // 处理错误，如用户名或邮箱已存在
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("注册失败: " + ex.Message);
            }

            return false;
        }


        private async Task<string> LoginUser(string username, string password)
        {
            var client = new HttpClient();
            var requestContent = new StringContent(JsonSerializer.Serialize(new
            {
                username = username,
                password = password
            }), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await client.PostAsync("", requestContent);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    using (var jsonDoc = JsonDocument.Parse(responseBody))
                    {
                        var root = jsonDoc.RootElement;
                        var token = root.GetProperty("token").GetString();
                        var email = root.GetProperty("email").GetString();
                        int userid = root.GetProperty("id").GetInt16();
                        var nickname = root.GetProperty("nickname").GetString();
                        AppDataController.SetJSGAccountLogined(true);
                        AppDataController.SetJSGAccountEmail(email);
                        AppDataController.SetJSGAccountNickname(nickname);
                        AppDataController.SetJSGAccountToken(token);
                        AppDataController.SetJSGAccountUserID(userid);
                        AppDataController.SetJSGAccountUsername(username);

                        return token; // 返回用户令牌
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // 登录失败，用户名或密码错误
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("登录失败: " + ex.Message);
            }

            return null;
        }
    }

}
