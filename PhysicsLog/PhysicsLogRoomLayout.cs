﻿using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog
{
    public class PhysicsLogRoomLayout
    {

        [JsonProperty("debugRoomName")]
        public string DebugRoomName { get; set; }

        [JsonProperty("levelBounds")]
        public JsonRectangle LevelBounds { get; set; }

        [JsonProperty("solidTiles")]
        public List<int[]> SolidTiles { get; set; }

        [JsonProperty("entities")]
        public Dictionary<int, LoggedEntity> Entities { get; set; }

        [JsonProperty("movableEntities")]
        public List<LoggedEntity> MovableEntities { get; set; }

    }
}
