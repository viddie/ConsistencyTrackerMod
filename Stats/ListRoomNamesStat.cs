using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     
     {path:names-json}

     'ST-1', 'ST-2', 'ST-3', 'CR-1', 'CR-2'
         */

    public class ListRoomNamesStat : Stat {

        public static string ListRoomNames = "{list:roomNames}";

        public static List<string> IDs = new List<string>() {
            ListRoomNames,
        };

        public ListRoomNamesStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ListRoomNames);
                return format;
            }

            List<string> rooms = new List<string>();
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    rooms.Add($"{StatManager.GetFormattedRoomName(rInfo)}");
                }
            }

            if (StatManager.ListOutputFormat == ListFormat.Plain) {
                string output = string.Join(", ", rooms);
                format = format.Replace(ListRoomNames, $"{output}");

            } else if (StatManager.ListOutputFormat == ListFormat.Json) {
                string output = string.Join("', '", rooms);
                format = format.Replace(ListRoomNames, $"['{output}']");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(ListRoomNames, "Outputs the current path as list"),
                new KeyValuePair<string, string>(ListSuccessRatesStat.ListSuccessRates, "Outputs the success rate for all rooms on the current path as list"),
                new KeyValuePair<string, string>(ListChokeRatesStat.ListChokeRates, "Outputs the choke rates for all rooms on the current path as list"),
            };
        }
        public override List<StatFormat> GetStatExamples() {
            return new List<StatFormat>() {
                new StatFormat("list-room-names", $"Names: {ListRoomNames}\\nSuccess Rates: {ListSuccessRatesStat.ListSuccessRates}\\nChoke Rates: {ListChokeRatesStat.ListChokeRates}\\n"),
            };
        }
    }
}
