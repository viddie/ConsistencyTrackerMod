using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class StatsUtil {

        public static RoomInfo GetFurthestGoldenRun(PathInfo path, ChapterStats stats) {
            RoomInfo toRet = null;
            foreach (RoomInfo rInfo in path.WalkPath()) {
                RoomStats rStats = stats.GetRoom(rInfo);
                if (rStats.GoldenBerryDeaths > 0) {
                    toRet = rInfo;
                }
            }
            return toRet;
        }
        
    }
}