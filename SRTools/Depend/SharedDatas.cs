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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRTools.Depend
{
    class SharedDatas
    {
        public static class Gacha
        {
            private static double _fiveStarPity = 0;
            public static double FiveStarPity
            {
                get
                {
                    return _fiveStarPity;
                }
                set
                {
                    _fiveStarPity = value;
                }
            }
        }

        public static class UpdateSRGF
        {
            private static string _updateSRGFFilePath = null;
            public static string UpdateSRGFFilePath
            {
                get
                {
                    return _updateSRGFFilePath;
                }
                set
                {
                    _updateSRGFFilePath = value;
                }
            }

            private static string _updateSRGFUID = null;
            public static string UpdateSRGFUID
            {
                get
                {
                    return _updateSRGFUID;
                }
                set
                {
                    _updateSRGFUID = value;
                }
            }
        }
        

        public static class ScreenShotData
        {
            private static string _screenShotPath = null;
            public static string ScreenShotPath
            {
                get
                {
                    return _screenShotPath;
                }
                set
                {
                    _screenShotPath = value;
                }
            }
        }
    }
}
