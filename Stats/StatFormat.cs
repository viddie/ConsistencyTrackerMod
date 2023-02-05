using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Stats {
    public class StatFormat {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        public StatFormat(string pName, string pFormat) {
            Name = pName;
            Format = pFormat;
        }

    }
}
