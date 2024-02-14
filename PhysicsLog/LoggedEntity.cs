using Celeste.Mod.ConsistencyTracker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog {
    public class LoggedEntity {
        [JsonProperty("t")]
        public string Type { get; set; }

        [JsonProperty("p")]
        public JsonVector2 Position { get; set; }

        [JsonProperty("r")]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
