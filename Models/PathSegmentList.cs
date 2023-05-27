using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class PathSegmentList {

        [JsonProperty(Required = Required.Always, PropertyName = "selectedIndex")]
        public int SelectedIndex { get; set; }
        
        [JsonProperty(Required = Required.Always, PropertyName = "segments")]
        public List<PathSegment> Segments { get; set; }

        [JsonIgnore]
        public PathSegment CurrentSegment { 
            get {
                if (Segments == null || Segments.Count == 0 || SelectedIndex < 0 || SelectedIndex >= Segments.Count)
                    return null;
                return Segments[SelectedIndex];
            }
            set {
                if (Segments == null || Segments.Count == 0 || SelectedIndex < 0 || SelectedIndex >= Segments.Count)
                    return;
                Segments[SelectedIndex] = value;
            }
        }
        
        [JsonIgnore]
        public PathInfo CurrentPath {
            get => CurrentSegment?.Path;
            set {
                PathSegment segment = CurrentSegment;
                if (segment == null) return;
                segment.Path = value;
            }
        }

        /// <summary>
        /// Removes the segment at the given index, unless it's the only segment
        /// </summary>
        /// <param name="index"></param>
        public void RemoveSegment(int index) {
            //Check if index is in range
            if (index < 0 || index >= Segments.Count || Segments.Count <= 1) return;

            bool isSelectedIndex = index == SelectedIndex;

            //Remove the segment
            Segments.RemoveAt(index);

            //Update the selected index
            if (isSelectedIndex) { 
                SelectedIndex = Segments.Count - 1;
            }
        }

        /// <summary>
        /// Create a new PathSegmentList with a default segment.
        /// </summary>
        /// <returns></returns>
        public static PathSegmentList Create() {
            return new PathSegmentList() {
                SelectedIndex = 0,
                Segments = new List<PathSegment>() {
                    new PathSegment(){
                        Name = "Default",
                        Path = null,
                    },
                }
            };
        }
    }
}
