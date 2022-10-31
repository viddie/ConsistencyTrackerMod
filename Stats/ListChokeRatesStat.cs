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

    public class ListChokeRatesStat : Stat {

        public static string ListChokeRates = "{list:chokeRates}";
        public static string ListChokeRatesSession = "{list:chokeRatesSession}";

        public static List<string> IDs = new List<string>() {
            ListChokeRates, ListChokeRatesSession
        };

        public ListChokeRatesStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, ListChokeRates);
                format = StatManager.MissingPathFormat(format, ListChokeRatesSession);
                return format;
            }

            List<string> chokeRates = new List<string>();
            List<string> chokeRatesSession = new List<string>();

            //For every room
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    //We go through all other rooms to calc choke rate
                    bool pastRoom = false;
                    int[] goldenDeathsInRoom = new int[] { 0, 0 };
                    int[] goldenDeathsAfterRoom = new int[] { 0, 0 };

                    foreach (CheckpointInfo cpInfoTemp in chapterPath.Checkpoints) {
                        foreach (RoomInfo rInfoTemp in cpInfoTemp.Rooms) {
                            RoomStats rStats = chapterStats.GetRoom(rInfoTemp.DebugRoomName);

                            if (pastRoom) {
                                goldenDeathsAfterRoom[0] += rStats.GoldenBerryDeaths;
                                goldenDeathsAfterRoom[1] += rStats.GoldenBerryDeathsSession;
                            }

                            if (rInfoTemp.DebugRoomName == rInfo.DebugRoomName) {
                                pastRoom = true;
                                goldenDeathsInRoom[0] = rStats.GoldenBerryDeaths;
                                goldenDeathsInRoom[1] = rStats.GoldenBerryDeathsSession;
                            }
                        }
                    }

                    float crRoom, crRoomSession;

                    //Calculate
                    if (goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0] == 0) crRoom = 0;
                    else crRoom = (float)goldenDeathsInRoom[0] / (goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0]);

                    if (goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1] == 0) crRoomSession = 0;
                    else crRoomSession = (float)goldenDeathsInRoom[1] / (goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1]);

                    //Format
                    switch (StatManager.ListOutputFormat) {
                        case ListFormat.Plain:
                            chokeRates.Add(StatManager.FormatPercentage(crRoom));
                            chokeRatesSession.Add(StatManager.FormatPercentage(crRoomSession));
                            break;
                        case ListFormat.Json:
                            chokeRates.Add(crRoom.ToString());
                            chokeRatesSession.Add(crRoomSession.ToString());
                            break;
                    }
                }
            }

            string output = string.Join(", ", chokeRates);
            string outputSession = string.Join(", ", chokeRatesSession);

            if (StatManager.ListOutputFormat == ListFormat.Plain) {
                format = format.Replace(ListChokeRates, $"{output}");
                format = format.Replace(ListChokeRatesSession, $"{outputSession}");

            } else if (StatManager.ListOutputFormat == ListFormat.Json) {
                format = format.Replace(ListChokeRates, $"[{output}]");
                format = format.Replace(ListChokeRatesSession, $"[{outputSession}]");
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
