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


        /// <summary>
        /// Removes the segment at the given index, unless it's the only segment
        /// </summary>
        /// <param name="index"></param>
        public void RemoveSegment(int index) {
            //Check if index is in range
            if (index < 0 || index >= SegmentStats.Count || SegmentStats.Count <= 1) return;

            //Remove the segment
            SegmentStats.RemoveAt(index);
        }
    }
}
