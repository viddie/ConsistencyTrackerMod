using Newtonsoft.Json;
using System.Collections.Generic;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class ChapterStatsList {

        [JsonProperty(Required = Required.Always, PropertyName = "segmentStats")]
        public List<ChapterStats> SegmentStats { get; set; } = new List<ChapterStats>();

        public ChapterStats GetStats(int index) {
            if (index < 0) {
                return null;
            }

            while (SegmentStats.Count <= index) {
                SegmentStats.Add(new ChapterStats());
            }

            return SegmentStats[index];
        }

        public void SetStats(int index, ChapterStats stats) {
            if (index < 0) {
                return;
            }

            while (SegmentStats.Count <= index) {
                SegmentStats.Add(new ChapterStats());
            }

            SegmentStats[index] = stats;
        }
    }
}
