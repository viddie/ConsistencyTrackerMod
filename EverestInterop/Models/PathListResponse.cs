using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models {
    public class PathListResponse : Response {
        public List<string> mapNames { get; set; }
    }
}
