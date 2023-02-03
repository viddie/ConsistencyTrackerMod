using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models {
    internal class GetPlaceholderListResponse : Response {
        public List<KeyValuePair<string, string>> placeholders { get; set; }
    }
}
