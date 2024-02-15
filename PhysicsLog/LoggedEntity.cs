using Celeste.Mod.ConsistencyTracker.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog {
    public class LoggedEntity {
        
        [JsonProperty("i")]
        public int ID { get; set; }

        [JsonProperty("a")] public int AttachedTo { get; set; } = -1;

        [JsonProperty("t")] public string Type { get; set; } = "";

        [JsonProperty("p")]
        public JsonVector2 Position { get; set; }

        [JsonProperty("r")]
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        
        public LoggedEntity() {
        }
        
        public LoggedEntity(LoggedEntity cloneFrom) {
            ID = cloneFrom.ID;
            AttachedTo = cloneFrom.AttachedTo;
            Type = cloneFrom.Type;
            Position = new JsonVector2() {
                X = cloneFrom.Position.X,
                Y = cloneFrom.Position.Y
            };
            Properties = new Dictionary<string, object>(cloneFrom.Properties);
        }
    }
}
