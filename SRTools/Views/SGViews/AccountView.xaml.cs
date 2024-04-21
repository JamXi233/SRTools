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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SRTools.Views.SGViews
{
    public sealed partial class AccountView : Page
    {
        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public AccountView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to AccountView", 0);
            InitData();
        }

        private async void InitData()
        {
            await LoadData(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
        }

        private async Task LoadData(string jsonData)
        {
            var accountexist = false;
            AccountListView.SelectionChanged -= AccountListView_SelectionChanged;
            var CurrentLoginUID = await ProcessRun.SRToolsHelperAsync("/GetCurrentLogin");
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(jsonData);
            AccountListView.ItemsSource = accounts;
            if (accounts is not null) 
            {
                foreach (Account account in accounts)
                {
                    if (account.uid == CurrentLoginUID)
                    {
                        accountexist = true;
                        AccountListView.SelectedItem = account;
                        break;
                    }
                }
            }
            if (accountexist == false && CurrentLoginUID != "0" && CurrentLoginUID != "目前未登录ID")
            {
                // 创建一个新的 Account 对象，用于表示不存在于 accounts 列表中的 UID
                Account newAccount = new Account { uid = CurrentLoginUID,name = "未保存" };

                // 将新的 Account 对象添加到 accounts 列表中
                accounts.Add(newAccount);

                // 将 accounts 列表设置为 AccountListView 的 ItemsSource
                AccountListView.ItemsSource = accounts;

                // 将新的 Account 对象作为 AccountListView 的 SelectedItem
                AccountListView.SelectedItem = newAccount;
                saveAccount.IsEnabled = true;
                renameAccount.IsEnabled = false;
            }
            AccountListView.SelectionChanged += AccountListView_SelectionChanged;
        }

        private async void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountListView.SelectedItem != null)
            {
                Account selectedAccount = (Account)AccountListView.SelectedItem;
                string command = $"/RestoreUser {selectedAccount.uid} {selectedAccount.name}";
                await ProcessRun.SRToolsHelperAsync(command);

            }
            await LoadData(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
        }


        private async void SaveAccount(object sender, RoutedEventArgs e)
        {
            var CurrentLoginUID = await ProcessRun.SRToolsHelperAsync("/GetCurrentLogin");
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
            bool found = false;
            if (accounts != null)
            {
                foreach (Account account in accounts)
                {
                    if (account.uid == CurrentLoginUID)
                    {
                        found = true;
                        break;
                    }
                }
                if (found)
                {
                    saveAccountFail.IsOpen = true;
                }
                else
                {
                    saveAccountUID.Text = "将要保存的UID为:"+ CurrentLoginUID;
                    saveAccountName.IsOpen = true;
                }
            }
            else
            {
                saveAccountUID.Text = "将要保存的UID为:"+ CurrentLoginUID;
                saveAccountName.IsOpen = true;
            }
            
        }

        private async void SaveAccount_C(object sender, RoutedEventArgs e)
        {
            saveAccountName.IsOpen = false;
            saveAccountSuccess.Subtitle = await ProcessRun.SRToolsHelperAsync("/BackupUser " + saveAccountNameInput.Text);
            saveAccountSuccess.IsOpen = true;
            renameAccount.IsEnabled = true;
            saveAccount.IsEnabled = false;
            saveAccountNameInput.Text = "";
            await LoadData(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
        }

        private async void RenameAccount_C(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            renameAccountSuccess.Subtitle = await ProcessRun.SRToolsHelperAsync($"/RenameUser {selectedAccount.uid} {selectedAccount.name} {renameAccountNameInput.Text}");
            renameAccountSuccess.IsOpen = true;
            await LoadData(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
        }

        private async void RemoveAccount_C(TeachingTip sender, object args)
        {
            removeAccountCheck.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            removeAccountSuccess.Subtitle = await ProcessRun.SRToolsHelperAsync($"/RemoveUser {selectedAccount.uid} {selectedAccount.name}");
            removeAccountSuccess.IsOpen = true;
            await LoadData(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
            AccountListView.ItemsSource = accounts;
            if (accounts is not null)
            {
                AccountListView.SelectedIndex = 0;
            }
        }

        private async void GetCurrentAccount(object sender, RoutedEventArgs e)
        {
            currentAccountTip.IsOpen = true;
            currentAccountTip.Subtitle = "当前UID为:"+ await ProcessRun.SRToolsHelperAsync("/GetCurrentLogin");
        }

        private void DeleteAccount(object sender, RoutedEventArgs e)
        {
            removeAccountCheck.IsOpen = true;
        }

        private void RenameAccount(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = true;
        }

        private async void RefreshAccount(object sender, RoutedEventArgs e)
        {
            await LoadData(await ProcessRun.SRToolsHelperAsync("/GetAllBackup"));
        }


    }
    public class Account
    {
        public string uid { get; set; }
        public string name { get; set; }
    }

}