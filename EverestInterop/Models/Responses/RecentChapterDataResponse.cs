using Celeste.Mod.ConsistencyTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses {
    public class RecentChapterDataResponse : Response {
        public List<ChapterData> data { get; set; }

        public class ChapterData {
            public ChapterStats stats { get; set; }
            public PathInfo path { get; set; }
        }
    }

    
}
