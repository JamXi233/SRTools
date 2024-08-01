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

namespace SRTools.Depend
{
    public class GachaModel
    {
        public class GroupedRecord
        {
            public string name { get; set; }
            public int count { get; set; }
        }
        public class GachaRecord
        {
            public string gachaId { get; set; }
            public string gachaType { get; set; }
            public string itemId { get; set; }
            public string count { get; set; }
            public string time { get; set; }
            public string name { get; set; }
            public string lang { get; set; }
            public string itemType { get; set; }
            public string rankType { get; set; }
            public string id { get; set; }
        }

        public class GachaInfo
        {
            public string uid { get; set; }
        }

        public class GachaData
        {
            public GachaInfo info { get; set; }
            public List<GachaPool> list { get; set; }
        }

        public class GachaPool
        {
            public int cardPoolId { get; set; }
            public string cardPoolType { get; set; }
            public List<GachaRecord> records { get; set; }
        }

        public class CardPoolInfo
        {
            public List<CardPool> CardPools { get; set; }
            public int? FiveStarPity { get; set; }
            public int? FourStarPity { get; set; }
        }

        public class CardPool
        {
            public int CardPoolId { get; set; }
            public string CardPoolType { get; set; }
            public int? FiveStarPity { get; set; }
            public int? FourStarPity { get; set; }
            public bool? isPityEnable { get; set; }
        }
    }
}
