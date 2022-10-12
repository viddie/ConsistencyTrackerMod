using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
        Red: {chapter:color-red}
        Yellow: {chapter:color-yellow}
        Green: {chapter:color-green}
        Light-Green: {chapter:color-lightGreen}

         */

    public class SuccessRateColorsStat : Stat {

        public static string ColorRed = "{chapter:color-red}";
        public static string ColorYellow = "{chapter:color-yellow}";
        public static string ColorGreen = "{chapter:color-green}";
        public static string ColorLightGreen = "{chapter:color-lightGreen}";
        public static List<string> IDs = new List<string>() { ColorRed, ColorYellow, ColorGreen, ColorLightGreen };

        public SuccessRateColorsStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ColorRed);
                format = StatManager.MissingPathFormat(format, ColorYellow);
                format = StatManager.MissingPathFormat(format, ColorGreen);
                format = StatManager.MissingPathFormat(format, ColorLightGreen);
                return format;
            }

            //Light Green, Green, Yellow, Red
            int[] colorCounts = new int[] { 0, 0, 0, 0 };

            //Walk path
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);

                    float successRate = rStats.AverageSuccessOverSelectedN();

                    if (successRate >= 0.95) colorCounts[0]++;
                    else if (successRate >= 0.8) colorCounts[1]++;
                    else if (successRate >= 0.5) colorCounts[2]++;
                    else colorCounts[3]++;
                }
            }

            format = format.Replace(ColorLightGreen, $"{colorCounts[0]}");
            format = format.Replace(ColorGreen, $"{colorCounts[1]}");
            format = format.Replace(ColorYellow, $"{colorCounts[2]}");
            format = format.Replace(ColorRed, $"{colorCounts[3]}");

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }
    }
}
