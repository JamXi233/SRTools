using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using SRTools.Depend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SRTools.App;

namespace SRTools.Views.SGViews
{
    public sealed partial class AccountView : Page
    {
        public AccountView()
        {
            this.InitializeComponent();
            Logging.Write("Switch to AccountView", 0);
            InitData();
        }

        private async Task InitData(string mode = null)
        {
            saveAccount.IsEnabled = false;
            renameAccount.IsEnabled = false;
            deleteAccount.IsEnabled = false;
            currentAccount.IsEnabled = false;
            updateAccount.Visibility = Visibility.Collapsed;

            var backupDataTask = ProcessRun.SRToolsHelperAsync($"/GetAllBackup {StartGameView.GameRegion}");
            var currentLoginTask = ProcessRun.SRToolsHelperAsync($"/GetCurrentLogin {StartGameView.GameRegion}");

            await LoadData(await backupDataTask, await currentLoginTask);

            Account_Load.Visibility = Visibility.Collapsed;
            refreshAccount.Visibility = Visibility.Visible;
            refreshAccount_Loading.Visibility = Visibility.Collapsed;
        }

        private async Task LoadData(string jsonData, string CurrentLoginUID)
        {
            var accountexist = false;
            AccountListView.SelectionChanged -= AccountListView_SelectionChanged;
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(jsonData);

            if (accounts == null)
            {
                accounts = new List<Account>();
            }

            AccountListView.ItemsSource = accounts;
            if (accounts.Count > 0)
            {
                foreach (Account account in accounts)
                {
                    if (account.uid == CurrentLoginUID)
                    {
                        accountexist = true;
                        AccountListView.SelectedItem = account;
                        saveAccount.IsEnabled = true;
                        renameAccount.IsEnabled = true;
                        deleteAccount.IsEnabled = true;
                        currentAccount.IsEnabled = true;

                        // 检查是否为旧版本账户
                        if (!account.nuser)
                        {
                            updateAccount.Visibility = Visibility.Visible;
                        }
                        break;
                    }
                }
            }

            if (!accountexist)
            {
                saveAccount.IsEnabled = false;
                renameAccount.IsEnabled = false;
                deleteAccount.IsEnabled = false;
                currentAccount.IsEnabled = true;
            }

            if (!accountexist && CurrentLoginUID != "0" && CurrentLoginUID != "目前未登录ID")
            {
                Account newAccount = new Account { uid = CurrentLoginUID, name = "未保存", nuser = true };
                accounts.Add(newAccount);
                AccountListView.ItemsSource = accounts;
                AccountListView.SelectedItem = newAccount;
                saveAccount.IsEnabled = true;
                renameAccount.IsEnabled = false;
                currentAccount.IsEnabled = true;
                deleteAccount.IsEnabled = false;
            }

            AccountListView.SelectionChanged += AccountListView_SelectionChanged;
            refreshAccount.Visibility = Visibility.Visible;
            refreshAccount_Loading.Visibility = Visibility.Collapsed;
        }

        private async void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccountListView.SelectedItem != null)
            {
                Account selectedAccount = (Account)AccountListView.SelectedItem;
                string command = selectedAccount.nuser
                                 ? $"/RestoreNUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name}"
                                 : $"/RestoreUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name}";
                var result = await ProcessRun.SRToolsHelperAsync(command);

                if (result.Contains("切换失败"))
                {
                    NotificationManager.RaiseNotification("账号切换失败", result, InfoBarSeverity.Error, false, 3);
                    await InitData();
                    return;
                }
                else
                {
                    NotificationManager.RaiseNotification("账号切换成功", $"成功切换到 UID: {selectedAccount.uid}", InfoBarSeverity.Success, false, 3);
                }

                // 仅获取当前UID
                var currentLoginUIDTask = ProcessRun.SRToolsHelperAsync($"/GetCurrentLogin {StartGameView.GameRegion}");
                var CurrentLoginUID = await currentLoginUIDTask;

                saveAccount.IsEnabled = selectedAccount.uid == CurrentLoginUID;
                renameAccount.IsEnabled = selectedAccount.uid == CurrentLoginUID;
                deleteAccount.IsEnabled = selectedAccount.uid == CurrentLoginUID;
                currentAccount.IsEnabled = true;

                // 检查是否为旧版本账户
                updateAccount.Visibility = selectedAccount.nuser ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private async void UpdateAccount(object sender, RoutedEventArgs e)
        {
            if (AccountListView.SelectedItem != null)
            {
                Account selectedAccount = (Account)AccountListView.SelectedItem;
                string command = $"/RestoreUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name}";
                var result = await ProcessRun.SRToolsHelperAsync(command);

                if (result.Contains("切换失败"))
                {
                    NotificationManager.RaiseNotification("账号更新失败", result, InfoBarSeverity.Error, false, 3);
                }
                else
                {
                    NotificationManager.RaiseNotification("账号更新成功", $"成功更新 UID: {selectedAccount.uid}", InfoBarSeverity.Success, false, 3);
                }
                await InitData();
            }
        }

        private async void SaveAccount(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentLoginUIDTask = ProcessRun.SRToolsHelperAsync($"/GetCurrentLogin {StartGameView.GameRegion}");
                var accountsTask = ProcessRun.SRToolsHelperAsync($"/GetAllBackup {StartGameView.GameRegion}");

                var CurrentLoginUID = await currentLoginUIDTask;
                List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(await accountsTask);

                var currentAccount = accounts?.FirstOrDefault(account => account.uid == CurrentLoginUID);

                if (currentAccount != null)
                {
                    DialogManager.RaiseDialog(XamlRoot, "账号已经存在", $"是否要覆盖保存UID: {CurrentLoginUID}", true, "确定覆盖", SaveAccount_Override);
                }
                else
                {
                    saveAccountUID.Text = $"将要保存的UID为: {CurrentLoginUID}";
                    saveAccountName.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("保存账户失败", ex.Message, InfoBarSeverity.Error, false, 3);
            }
        }

        private async void SaveAccount_Override()
        {
            try
            {
                var currentLoginUIDTask = ProcessRun.SRToolsHelperAsync($"/GetCurrentLogin {StartGameView.GameRegion}");
                var CurrentLoginUID = await currentLoginUIDTask;

                string currentName = await GetAccountNameByUID(CurrentLoginUID);

                if (!string.IsNullOrEmpty(currentName))
                {
                    string backupResult = await ProcessRun.SRToolsHelperAsync($"/BackupNUser {StartGameView.GameRegion} {currentName}");
                    NotificationManager.RaiseNotification("覆盖保存成功", $"成功保存 UID: {CurrentLoginUID}", InfoBarSeverity.Success, false, 3);
                    Console.WriteLine(backupResult);
                }
                else
                {
                    NotificationManager.RaiseNotification("覆盖保存失败", "未找到当前 UID 的别名，无法进行覆盖保存。", InfoBarSeverity.Error, false, 3);
                }
            }
            catch (Exception ex)
            {
                NotificationManager.RaiseNotification("覆盖保存失败", ex.Message, InfoBarSeverity.Error, false, 3);
            }
        }

        public static async Task<string> GetAccountNameByUID(string UID)
        {
            string jsonData = await ProcessRun.SRToolsHelperAsync($"/GetAllBackup {StartGameView.GameRegion}");
            List<Account> accounts = JsonConvert.DeserializeObject<List<Account>>(jsonData);
            Account account = accounts?.Find(acc => acc.uid == UID);
            return account?.name ?? "";
        }

        private async void SaveAccount_C(object sender, RoutedEventArgs e)
        {
            saveAccountName.IsOpen = false;
            var result = await ProcessRun.SRToolsHelperAsync($"/BackupNUser {StartGameView.GameRegion} " + saveAccountNameInput.Text);
            NotificationManager.RaiseNotification("保存账户成功", result, InfoBarSeverity.Success, false, 3);
            saveAccountSuccess.Subtitle = result;
            saveAccountSuccess.IsOpen = true;
            renameAccount.IsEnabled = true;
            saveAccount.IsEnabled = true;
            saveAccountNameInput.Text = "";
            await InitData();
        }

        private async void RenameAccount_C(object sender, RoutedEventArgs e)
        {
            renameAccountTip.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            string NUser = selectedAccount.DisplayName.Contains("旧版本") ? "0" : "1";
            var result = await ProcessRun.SRToolsHelperAsync($"/RenameUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name} {renameAccountNameInput.Text} {NUser}");
            NotificationManager.RaiseNotification("重命名账户成功", result, InfoBarSeverity.Success, false, 3);
            renameAccountSuccess.Subtitle = result;
            renameAccountSuccess.IsOpen = true;
            await InitData();
        }

        private async void RemoveAccount_C(TeachingTip sender, object args)
        {
            removeAccountCheck.IsOpen = false;
            Account selectedAccount = (Account)AccountListView.SelectedItem;
            string NUser = selectedAccount.DisplayName.Contains("旧版本") ? "0" : "1";
            var result = await ProcessRun.SRToolsHelperAsync($"/RemoveUser {StartGameView.GameRegion} {selectedAccount.uid} {selectedAccount.name} {NUser}");
            NotificationManager.RaiseNotification("移除账户成功", result, InfoBarSeverity.Success, false, 3);
            removeAccountSuccess.Subtitle = result;
            removeAccountSuccess.IsOpen = true;
            await InitData();
        }

        private async void GetCurrentAccount(object sender, RoutedEventArgs e)
        {
            var result = await ProcessRun.SRToolsHelperAsync($"/GetCurrentLogin {StartGameView.GameRegion}");
            NotificationManager.RaiseNotification("当前账户查询", "UID:"+result, InfoBarSeverity.Success, false, 3);
            currentAccountTip.IsOpen = true;
            currentAccountTip.Subtitle = "当前UID为:" + result;
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
            refreshAccount.Visibility = Visibility.Collapsed;
            refreshAccount_Loading.Visibility = Visibility.Visible;
            await InitData();
        }
    }

    public class Account
    {
        public string uid { get; set; }
        public string name { get; set; }
        public bool nuser { get; set; }

        public string DisplayName
        {
            get
            {
                return nuser ? name : name + " (旧版本)";
            }
        }
    }
}
