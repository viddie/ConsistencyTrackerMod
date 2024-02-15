using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class JsonVector2 {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }
        
        //Equality check
        public override bool Equals(object obj) {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }
            JsonVector2 other = (JsonVector2)obj;
            return X == other.X && Y == other.Y;
        }
        
        //Hash code
        public override int GetHashCode() {
            return X.GetHashCode() ^ Y.GetHashCode();
        }
    }
}
