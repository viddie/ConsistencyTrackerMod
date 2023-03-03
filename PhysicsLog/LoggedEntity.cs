using Celeste.Mod.ConsistencyTracker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog {
    public class LoggedEntity {
        [JsonProperty("type")]
        public string Type { get; set; }

        //[JsonProperty("x")]
        //public float X { get; set; }

        //[JsonProperty("y")]
        //public float Y { get; set; }

        [JsonProperty("position")]
        public JsonVector2 Position { get; set; }

        [JsonProperty("properties")]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
