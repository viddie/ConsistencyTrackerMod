using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class GetPlaceholderListResponse : Response
    {
        public List<Placeholder> placeholders { get; set; }

        public class Placeholder
        {
            public string name { get; set; }
            public string description { get; set; }
        }
    }
}
