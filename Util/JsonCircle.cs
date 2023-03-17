using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class JsonCircle {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("radius")]
        public float Radius { get; set; }
    }

    public class JsonColliderCircle {
        [JsonProperty("type")]
        public string Type {
            get => "hitcircle";
            private set => throw new InvalidOperationException("Cannot set type of JsonColliderCircle");
        }

        [JsonProperty("hitcircle")]
        public JsonCircle HitCircle { get; set; }
    }
}
