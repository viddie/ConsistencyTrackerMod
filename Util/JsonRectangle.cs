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

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }
    }

    public class JsonColliderHitbox {
        [JsonProperty("type")]
        public string Type {
            get => "hitbox";
            private set => throw new InvalidOperationException("Cannot set type of JsonColliderHitbox");
        }

        [JsonProperty("hitbox")]
        public JsonRectangle Hitbox { get; set; }
    }
}
