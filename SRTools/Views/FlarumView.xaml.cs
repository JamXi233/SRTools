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

using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using SRTools.Depend;
using System;
using Microsoft.UI.Xaml;
using System.Net.Http;

namespace SRTools.Views
{
    public sealed partial class FlarumView : Page
    {
        private HttpClient client = new HttpClient();
        private string csrfToken;
        private Uri baseUri = new Uri("https://bbs.srtools.jamsg.cn");
        public FlarumView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to FlarumView", 0);

            BBS.NavigationStarting += WebView2_NavigationStarting;
            BBS.NavigationCompleted += WebView2_NavigationCompleted;
            LoadWebView2();

            
        }


        private async void LoadWebView2() 
        {

            await BBS.EnsureCoreWebView2Async();


            // 禁用开发者工具
            BBS.CoreWebView2.Settings.AreDevToolsEnabled = true;
            BBS.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        }

        private async void WebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            Logging.Write(e.Uri);
            Loading.Visibility = Visibility.Visible;
        }

        private async void WebView2_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                //((WebView2)sender).ExecuteScriptAsync("document.querySelector('body').style.overflow='scroll';var style=document.createElement('style');style.type='text/css';style.innerHTML='::-webkit-scrollbar{display:none}';document.getElementsByTagName('body')[0].appendChild(style)");
                /*var uri = new Uri("https://bbs.srtools.jamsg.cn");
                var cookies = await BBS.CoreWebView2.CookieManager.GetCookiesAsync(uri.ToString());
                foreach (var cookie in cookies)
                {
                    if (cookie.Name == "flarum_remember")
                    {
                        // 找到 cookie，执行相应操作
                        return;
                    }
                }*/

                // 没有找到 cookie，显示登录对话框
                // ShowLoginDialog();
            }
            Loading.Visibility = Visibility.Collapsed;
        }
/*
        private async Task InitializeSession()
        {
            client.BaseAddress = baseUri;

            // 发送请求以获取 cookies 和 CSRF 令牌
            var response = await client.GetAsync("/");
            if (response.IsSuccessStatusCode)
            {
                // 获取并保存 CSRF 令牌
                if (response.Headers.TryGetValues("X-Csrf-Token", out var values))
                {
                    csrfToken = values.FirstOrDefault();
                }

                // 将接收到的 cookies 添加到 HttpClient
                var cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                if (cookies != null)
                {
                    foreach (var cookie in cookies)
                    {
                        client.DefaultRequestHeaders.Add("Cookie", cookie);
                    }
                }
            }
        }

        private async void ShowLoginDialog()
        {
            // 创建控件实例并保存引用
            TextBox usernameTextBox = new TextBox { PlaceholderText = "用户名", Margin = new Thickness(0, 0, 0, 10) };
            PasswordBox passwordBox = new PasswordBox { PlaceholderText = "密码", Margin = new Thickness(0, 0, 0, 10) };

            ContentDialog loginDialog = new ContentDialog
            {
                Title = "登录到 JSG-LLC",
                Content = new StackPanel
                {
                    Children =
            {
                usernameTextBox,
                passwordBox
            }
                },
                PrimaryButtonText = "登录",
                SecondaryButtonText = "注册",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot // 设置 XamlRoot
            };

            ContentDialogResult result = await loginDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                LoginUser(usernameTextBox.Text,passwordBox.Password);
                // 处理登录逻辑
            }
            else if (result == ContentDialogResult.Secondary)
            {
                await ShowRegisterDialog();
            }
        }

        private async Task ShowRegisterDialog()
        {
            // 创建控件实例并保存引用
            TextBox usernameTextBox = new TextBox { PlaceholderText = "用户名", Margin = new Thickness(0, 0, 0, 10) };
            PasswordBox passwordBox = new PasswordBox { PlaceholderText = "密码", Margin = new Thickness(0, 0, 0, 10) };
            PasswordBox confirmPasswordBox = new PasswordBox { PlaceholderText = "确认密码", Margin = new Thickness(0, 0, 0, 10) };
            TextBox emailTextBox = new TextBox { PlaceholderText = "电子邮件地址", Margin = new Thickness(0, 0, 0, 10) };

            ContentDialog registerDialog = new ContentDialog
            {
                Title = "注册新账号",
                Content = new StackPanel
                {
                    Children =
            {
                usernameTextBox,
                passwordBox,
                confirmPasswordBox,
                emailTextBox
            }
                },
                PrimaryButtonText = "注册",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            ContentDialogResult result = await registerDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                if (passwordBox.Password == confirmPasswordBox.Password)
                {
                    RegisterUser(usernameTextBox.Text, passwordBox.Password, emailTextBox.Text);
                }
                else
                {
                    // 密码不匹配的处理
                }
            }
        }

        private async void RegisterUser(string username, string password, string email)
        {
            await InitializeSession();  // 确保会话已初始化并获取了 CSRF 令牌和 cookies

            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                data = new
                {
                    type = "users",
                    attributes = new
                    {
                        username = username,
                        password = password,
                        email = email
                    }
                }
            }), Encoding.UTF8, "application/json");

            // 添加 CSRF 令牌到请求头部
            client.DefaultRequestHeaders.Add("X-CSRF-Token", csrfToken);

            var response = await client.PostAsync("/api/users", content);
            if (response.IsSuccessStatusCode)
            {
                
            }
            else
            {
                // 处理失败的请求
                var error = await response.Content.ReadAsStringAsync();
                await ShowErrorDialog("注册失败", "无法完成注册，请重试。错误详情：" + error);
            }
        }


        private async void LoginUser(string username, string password)
        {
            await InitializeSession();  // 确保会话已初始化

            var content = new StringContent($"{{\"identification\": \"{username}\", \"password\": \"{password}\"}}", Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Add("X-CSRF-Token", csrfToken);

            var response = await client.PostAsync("/api/token", content);
            if (response.IsSuccessStatusCode)
            {
                
            }
            else
            {
                // 处理失败的请求
                var error = await response.Content.ReadAsStringAsync();
                await ShowErrorDialog("登录失败", "用户名或密码错误。错误详情：" + error);
            }
        }

        private async Task ShowErrorDialog(string title, string content)
        {
            await new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            }.ShowAsync();
        }*/


    }
}