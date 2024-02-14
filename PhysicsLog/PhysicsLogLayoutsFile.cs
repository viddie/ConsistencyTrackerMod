using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog {
    public class PhysicsLogLayoutsFile {

        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("chapterName")]
        public string ChapterName { get; set; }

        [JsonProperty("sideName")]
        public string SideName { get; set; }

        [JsonProperty("frameCount")]
        public int FrameCount { get; set; }

        [JsonProperty("recordingStarted")]
        public DateTime RecordingStarted { get; set; }

        [JsonProperty("rooms")]
        public List<PhysicsLogRoomLayout> Rooms { get; set; }
        
        [JsonProperty("usesMovableEntities")]
        public bool UsesMovableEntities { get; set; }

    }
}
