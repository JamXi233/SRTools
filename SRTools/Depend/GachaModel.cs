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
