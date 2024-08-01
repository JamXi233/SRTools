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

using System.Collections.Generic;

namespace SRTools.Depend
{
    public static class GameUpdateModel
    {
        public static string LatestVersion { get; set; }
        public static List<string> LatestPaths { get; set; }
        public static List<string> LatestSizes { get; set; }
        public static List<string> LatestMD5s { get; set; }
        public static string DiffsVersion { get; set; }
        public static string DiffsPath { get; set; }
        public static string DiffsSize { get; set; }
        public static string DiffsMD5 { get; set; }
        public static string LatestVoicePacksPath { get; set; }
        public static string LatestVoicePacksSize { get; set; }
        public static string LatestVoicePacksMD5 { get; set; }
        public static string DiffsVoicePacksPath { get; set; }
        public static string DiffsVoicePacksSize { get; set; }
        public static string DiffsVoicePacksMD5 { get; set; }
        public static bool isGetUpdateInfo { get; set; }
    }
}