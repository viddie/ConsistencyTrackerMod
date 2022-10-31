using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     
         */

    public class ListSuccessRatesStat : Stat {

        public static string ListSuccessRates = "{list:successRates}";

        public static List<string> IDs = new List<string>() {
            ListSuccessRates,
        };

        public ListSuccessRatesStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ListSuccessRates);
                return format;
            }

            List<string> rooms = new List<string>();
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    switch (StatManager.ListOutputFormat) {
                        case ListFormat.Plain:
                            rooms.Add(StatManager.FormatPercentage(chapterStats.GetRoom(rInfo.DebugRoomName).AverageSuccessOverN(StatManager.AttemptCount)));
                            break;
                        case ListFormat.Json:
                            rooms.Add(chapterStats.GetRoom(rInfo.DebugRoomName).AverageSuccessOverN(StatManager.AttemptCount).ToString());
                            break;
                    }
                }
            }

            string output = string.Join(", ", rooms);
            if (StatManager.ListOutputFormat == ListFormat.Plain) {
                format = format.Replace(ListSuccessRates, $"{output}");

            } else if (StatManager.ListOutputFormat == ListFormat.Json) {
                format = format.Replace(ListSuccessRates, $"[{output}]");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                //new KeyValuePair<string, string>(PathSuccessRatesJson, "Outputs the current path as JSON array"),
            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                //new StatFormat("path-json", $"{PathSuccessRatesJson}"),
            };
        }
    }
}
