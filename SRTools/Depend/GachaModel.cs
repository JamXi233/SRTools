using System;
using System.Collections.Generic;

namespace SRTools.Depend
{
    public class GachaModel
    {
        public class GachaRecord
        {
            public string Uid { get; set; }
            public string GachaId { get; set; }
            public string GachaType { get; set; }
            public string ItemId { get; set; }
            public string Count { get; set; }
            public DateTime Time { get; set; }
            public string Name { get; set; }
            public string Lang { get; set; }
            public string ItemType { get; set; }
            public string RankType { get; set; }
            public string Id { get; set; }
        }

        public class GachaData
        {
            public List<GachaRecord> Records { get; set; }
        }

        public class CardPoolInfo
        {
            public int? FiveStarPity { get; set; }
            public int? FourStarPity { get; set; }
        }
    }
}
