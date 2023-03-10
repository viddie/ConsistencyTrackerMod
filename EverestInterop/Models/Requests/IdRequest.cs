using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Requests
{
    public class IdRequest
    {

        [JsonProperty("id")]
        public int ID { get; set; }

    }
}
