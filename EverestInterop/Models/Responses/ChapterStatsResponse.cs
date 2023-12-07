using Celeste.Mod.ConsistencyTracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses
{
    public class ChapterStatsResponse : Response
    {
        public ChapterStats chapterStats { get; set; }
        public GameData gameData { get; set; }

        public class GameData {
            public bool completed { get; set; }
            public bool fullClear { get; set; }
            public long totalTime { get; set; }
            public long totalDeaths { get; set; }
        }
    }
}
