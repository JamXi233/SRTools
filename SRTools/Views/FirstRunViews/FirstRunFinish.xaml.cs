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
using System.Threading.Tasks;
using SRTools.Depend;

namespace SRTools.Views.FirstRunViews
{
    public sealed partial class FirstRunFinish : Page
    {
        public FirstRunFinish()
        {
            this.InitializeComponent();
            Logging.Write("Switch to FirstRunFinish", 0);
            Logging.Write("Thanks For Using SRTools!", 0);
            _ = SetFirstRunCompletedAsync();

        }

        private async Task SetFirstRunCompletedAsync()
        {
            // 延迟两秒
            await Task.Delay(2000);

            // 两秒后执行的操作
            AppDataController.SetFirstRun(0);

        }

    }

}