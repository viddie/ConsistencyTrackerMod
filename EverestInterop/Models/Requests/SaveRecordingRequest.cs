using Celeste.Mod.ConsistencyTracker.PhysicsLog;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Requests
{
    public class SaveRecordingRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("layoutFile")]
        public PhysicsLogLayoutsFile LayoutFile { get; set; }

        [JsonProperty("physicsLog")]
        public List<string> PhysicsLog { get; set; }
    }
}
