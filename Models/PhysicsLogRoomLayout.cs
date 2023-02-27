using Celeste.Mod.ConsistencyTracker.Util;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class PhysicsLogRoomLayout {

        [JsonProperty("debugRoomName")]
        public string DebugRoomName { get; set; }
        
        [JsonProperty("levelBounds")]
        public JsonRectangle LevelBounds { get; set; }

        [JsonProperty("solidTiles")]
        public List<int[]> SolidTiles { get; set; }

        [JsonProperty("entities")]
        public List<LoggedEntity> Entities { get; set; }

        [JsonProperty("otherEntities")]
        public List<LoggedEntity> OtherEntities { get; set; }



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
            public Dictionary<string, object> Properties { get; set; }
        }
    }
}
