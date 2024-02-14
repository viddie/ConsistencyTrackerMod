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

        [JsonProperty("r")]
        public float Radius { get; set; }
    }

    public class JsonColliderCircle {
        [JsonProperty("t")]
        public string Type {
            get => "c";
            private set => throw new InvalidOperationException("Cannot set type of JsonColliderCircle");
        }

        [JsonProperty("c")]
        public JsonCircle HitCircle { get; set; }
    }
}
