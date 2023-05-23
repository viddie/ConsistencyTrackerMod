using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class PathSegment {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("path")]
        public PathInfo Path { get; set; }
    }
}
