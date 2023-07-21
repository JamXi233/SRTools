using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;

namespace SRTools.Views.SGViews
{
    public sealed partial class AccountView : Page
    {
        string userDocumentsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public AccountView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to AccountView", 0);
            LoadData(SRToolsHelper("/GetAllBackup"));

        }

        private void LoadData(string jsonData)
        {
            var accountexist = false;
            AccountListView.SelectionChanged -= AccountListView_SelectionChanged;
            var CurrentLoginUID = SRToolsHelper("/GetCurrentLogin");
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

        private void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountListView.SelectedItem != null)
            {
                Account selectedAccount = (Account)AccountListView.SelectedItem;
                string command = $"/RestoreUser {selectedAccount.uid} {selectedAccount.name}";
                SRToolsHelper(command);

            }
            LoadData(SRToolsHelper("/GetAllBackup"));
        }


        private void SaveAccount(object sender, RoutedEventArgs e)
        {
            var CurrentLoginUID = SRToolsHelper("/GetCurrentLogin");
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(SRToolsHelper("/GetAllBackup"));
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

        private void SaveAccount_C(object sender, RoutedEventArgs e)
        {
            saveAccountName.IsOpen = false;
            saveAccountSuccess.Subtitle = SRToolsHelper("/BackupUser " + saveAccountNameInput.Text);
            saveAccountSuccess.IsOpen = true;
            renameAccount.IsEnabled = true;
            saveAccount.IsEnabled = false;
            saveAccountNameInput.Text = "";
            LoadData(SRToolsHelper("/GetAllBackup"));
        }

        private void RenameAccount_C(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            renameAccountSuccess.Subtitle = SRToolsHelper($"/RenameUser {selectedAccount.uid} {selectedAccount.name} {renameAccountNameInput.Text}");
            renameAccountSuccess.IsOpen = true;
            LoadData(SRToolsHelper("/GetAllBackup"));
        }

        private void RemoveAccount_C(TeachingTip sender, object args)
        {
            removeAccountCheck.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            removeAccountSuccess.Subtitle = SRToolsHelper($"/RemoveUser {selectedAccount.uid} {selectedAccount.name}");
            removeAccountSuccess.IsOpen = true;
            LoadData(SRToolsHelper("/GetAllBackup"));
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(SRToolsHelper("/GetAllBackup"));
            AccountListView.ItemsSource = accounts;
            if (accounts is not null)
            {
                AccountListView.SelectedIndex = 0;
            }
        }

        private void GetCurrentAccount(object sender, RoutedEventArgs e)
        {
            currentAccountTip.IsOpen = true;
            currentAccountTip.Subtitle = "当前UID为:"+SRToolsHelper("/GetCurrentLogin");
        }

        private void DeleteAccount(object sender, RoutedEventArgs e)
        {
            removeAccountCheck.IsOpen = true;
        }

        private void RenameAccount(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = true;
        }

        private void RefreshAccount(object sender, RoutedEventArgs e)
        {
            LoadData(SRToolsHelper("/GetAllBackup"));
        }

        private string SRToolsHelper(string Args)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = userDocumentsFolderPath + @"\JSG-LLC\SRTools\Depends\SRToolsHelper\SRToolsHelper.exe";
            process.StartInfo.Arguments = Args;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Logging.Write(output.Trim(), 3,"SRToolsHelper");
            return output.Trim();
        }

    }
    public class Account
    {
        public string uid { get; set; }
        public string name { get; set; }
    }

}