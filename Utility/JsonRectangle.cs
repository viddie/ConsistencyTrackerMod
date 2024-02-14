using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class JsonRectangle {
        [JsonProperty("x")]
        public float X { get; set; }
        
        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("w")]
        public float Width { get; set; }

        [JsonProperty("h")]
        public float Height { get; set; }
    }

    public class JsonColliderHitbox {
        [JsonProperty("t")]
        public string Type {
            get => "b";
            private set => throw new InvalidOperationException("Cannot set type of JsonColliderHitbox");
        }

        [JsonProperty("b")]
        public JsonRectangle Hitbox { get; set; }
    }
}
