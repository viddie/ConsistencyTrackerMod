using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;

namespace Celeste.Mod.ConsistencyTracker.Stats {

    /*
     Stats to implement:
     {room:chokeRate}
     {room:chokeRateSession}
     {checkpoint:chokeRate}
     {checkpoint:chokeRateSession}
         */

    public class ChokeRateStat : Stat {

        public static string RoomChokeRate = "{room:chokeRate}";
        public static string RoomChokeRateSession = "{room:chokeRateSession}";
        public static string CheckpointChokeRate = "{checkpoint:chokeRate}";
        public static string CheckpointChokeRateSession = "{checkpoint:chokeRateSession}";
        public static string RoomGoldenSuccessRate = "{room:goldenSuccessRate}";
        public static string RoomGoldenSuccessRateSession = "{room:goldenSuccessRateSession}";
        public static string CheckpointGoldenSuccessRate = "{checkpoint:goldenSuccessRate}";
        public static string CheckpointGoldenSuccessRateSession = "{checkpoint:goldenSuccessRateSession}";

        public static string RoomGoldenEntries = "{room:goldenEntries}";
        public static string RoomGoldenEntriesSession = "{room:goldenEntriesSession}";
        public static string RoomGoldenSuccesses = "{room:goldenSuccesses}";
        public static string RoomGoldenSuccessesSession = "{room:goldenSuccessesSession}";

        public static string RoomGoldenEntryChance = "{room:goldenEntryChance}";
        public static string RoomGoldenEntryChanceSession = "{room:goldenEntryChanceSession}";
        public static List<string> IDs = new List<string>() {
            RoomChokeRate, RoomChokeRateSession,
            CheckpointChokeRate, CheckpointChokeRateSession,
            RoomGoldenSuccessRate, RoomGoldenSuccessRateSession,
            CheckpointGoldenSuccessRate, CheckpointGoldenSuccessRateSession,
            
            RoomGoldenEntries, RoomGoldenEntriesSession,
            RoomGoldenSuccesses, RoomGoldenSuccessesSession,

            RoomGoldenEntryChance, RoomGoldenEntryChanceSession
        };

        public ChokeRateStat() : base(IDs) { }

        public override string FormatStat(PathInfo chapterPath, ChapterStats chapterStats, string format) {
            if (chapterPath == null) {
                format = StatManager.MissingPathFormat(format, RoomChokeRate);
                format = StatManager.MissingPathFormat(format, RoomChokeRateSession);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRate);
                format = StatManager.MissingPathFormat(format, CheckpointChokeRateSession);

                format = StatManager.MissingPathFormat(format, RoomGoldenSuccessRate);
                format = StatManager.MissingPathFormat(format, RoomGoldenSuccessRateSession);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenSuccessRate);
                format = StatManager.MissingPathFormat(format, CheckpointGoldenSuccessRateSession);

                format = StatManager.MissingPathFormat(format, RoomGoldenEntries);
                format = StatManager.MissingPathFormat(format, RoomGoldenEntriesSession);
                format = StatManager.MissingPathFormat(format, RoomGoldenSuccesses);
                format = StatManager.MissingPathFormat(format, RoomGoldenSuccessesSession);

                format = StatManager.MissingPathFormat(format, RoomGoldenEntryChance);
                format = StatManager.MissingPathFormat(format, RoomGoldenEntryChanceSession);

                return format;
                
            } else if (chapterPath.CurrentRoom == null) { //Player is not on the path
                format = StatManager.NotOnPathFormatPercent(format, RoomChokeRate);
                format = StatManager.NotOnPathFormatPercent(format, RoomChokeRateSession);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointChokeRate);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointChokeRateSession);

                format = StatManager.NotOnPathFormatPercent(format, RoomGoldenSuccessRate);
                format = StatManager.NotOnPathFormatPercent(format, RoomGoldenSuccessRateSession);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointGoldenSuccessRate);
                format = StatManager.NotOnPathFormatPercent(format, CheckpointGoldenSuccessRateSession);

                format = StatManager.NotOnPathFormat(format, RoomGoldenEntries);
                format = StatManager.NotOnPathFormat(format, RoomGoldenEntriesSession);
                format = StatManager.NotOnPathFormat(format, RoomGoldenSuccesses);
                format = StatManager.NotOnPathFormat(format, RoomGoldenSuccessesSession);
                
                format = StatManager.NotOnPathFormatPercent(format, RoomGoldenEntryChance);
                format = StatManager.NotOnPathFormatPercent(format, RoomGoldenEntryChanceSession);

                return format;
            }

            //======== Room ========
            int[] goldenDeathsInRoom = new int[] {0, 0};
            int[] goldenDeathsAfterRoom = new int[] {0, 0};

            bool pastRoom = false;

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = chapterStats.GetRoom(rInfo.DebugRoomName);

                    if (pastRoom) {
                        goldenDeathsAfterRoom[0] += rStats.GoldenBerryDeaths;
                        goldenDeathsAfterRoom[1] += rStats.GoldenBerryDeathsSession;
                    }

                    if (rInfo.DebugRoomName == chapterStats.CurrentRoom.DebugRoomName) {
                        pastRoom = true;
                        goldenDeathsInRoom[0] = rStats.GoldenBerryDeaths;
                        goldenDeathsInRoom[1] = rStats.GoldenBerryDeathsSession;
                    }
                }
            }

            //Account for winning runs
            goldenDeathsAfterRoom[0] += chapterStats.GoldenCollectedCount;
            goldenDeathsAfterRoom[1] += chapterStats.GoldenCollectedCountSession;

            float crRoom, crRoomSession;
            float goldenSuccessRateRoom, goldenSuccessRateRoomSession;

            //Calculate
            int roomEntries = goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0];
            int roomEntriesSession = goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1];
            bool inGoldenRun = chapterStats.ModState.PlayerIsHoldingGolden;
            int roomSuccesses = goldenDeathsAfterRoom[0];
            int roomSuccessesSession = goldenDeathsAfterRoom[1];

            if (roomEntries == 0) crRoom = float.NaN;
            else crRoom = (float)goldenDeathsInRoom[0] / (roomEntries);

            if (roomEntriesSession == 0) crRoomSession = float.NaN;
            else crRoomSession = (float)goldenDeathsInRoom[1] / (roomEntriesSession);

            if (float.IsNaN(crRoom)) goldenSuccessRateRoom = float.NaN;
            else goldenSuccessRateRoom = 1 - crRoom;

            if (float.IsNaN(crRoomSession)) goldenSuccessRateRoomSession = float.NaN;
            else goldenSuccessRateRoomSession = 1 - crRoomSession;

            //Format
            if (float.IsNaN(crRoom)) { //pastRoom is false when player is not on path
                format = format.Replace(RoomChokeRate, $"-%");
            } else {
                format = format.Replace(RoomChokeRate, $"{StatManager.FormatPercentage(crRoom)}");
            }

            if (float.IsNaN(crRoomSession)) {
                format = format.Replace(RoomChokeRateSession, $"-%");
            } else {
                format = format.Replace(RoomChokeRateSession, $"{StatManager.FormatPercentage(crRoomSession)}");
            }

            //Golden Success Rate
            if (float.IsNaN(goldenSuccessRateRoom)) {
                format = format.Replace(RoomGoldenSuccessRate, $"-%");
            } else {
                format = format.Replace(RoomGoldenSuccessRate, $"{StatManager.FormatPercentage(goldenSuccessRateRoom)}");
            }

            if (float.IsNaN(goldenSuccessRateRoomSession)) {
                format = format.Replace(RoomGoldenSuccessRateSession, $"-%");
            } else {
                format = format.Replace(RoomGoldenSuccessRateSession, $"{StatManager.FormatPercentage(goldenSuccessRateRoomSession)}");
            }


            // + 1 if you want to count the current run as well, but idk feels weird :(
            format = format.Replace(RoomGoldenEntries, inGoldenRun ? (roomEntries + 0).ToString() : roomEntries.ToString());
            format = format.Replace(RoomGoldenEntriesSession, inGoldenRun ? (roomEntriesSession + 0).ToString() : roomEntriesSession.ToString());

            format = format.Replace(RoomGoldenSuccesses, roomSuccesses.ToString());
            format = format.Replace(RoomGoldenSuccessesSession, roomSuccessesSession.ToString());

            //Format Golden Run Chance
            int totalGoldenDeaths = chapterPath.Stats.GoldenBerryDeaths + chapterStats.GoldenCollectedCount;
            int totalGoldenDeathsSession = chapterPath.Stats.GoldenBerryDeathsSession + chapterStats.GoldenCollectedCountSession;
            if (totalGoldenDeaths == 0) {
                format = format.Replace(RoomGoldenEntryChance, $"-%");
            } else {
                format = format.Replace(RoomGoldenEntryChance, StatManager.FormatPercentage((float)roomEntries / totalGoldenDeaths));
            }

            if (totalGoldenDeathsSession == 0) {
                format = format.Replace(RoomGoldenEntryChanceSession, $"-%");
            } else {
                format = format.Replace(RoomGoldenEntryChanceSession, StatManager.FormatPercentage((float)roomEntriesSession / totalGoldenDeathsSession));
            }


            //======== Checkpoint ========

            CheckpointInfo currentCpInfo = chapterPath.CurrentRoom.Checkpoint;
            
            int[] goldenDeathsInCP = new int[] { 0, 0 };
            int[] goldenDeathsAfterCP = new int[] { 0, 0 };

            bool pastCP = false;

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                if (pastCP) {
                    goldenDeathsAfterCP[0] += cpInfo.Stats.GoldenBerryDeaths;
                    goldenDeathsAfterCP[1] += cpInfo.Stats.GoldenBerryDeathsSession;
                }

                if (cpInfo == currentCpInfo) {
                    pastCP = true;
                    goldenDeathsInCP[0] = cpInfo.Stats.GoldenBerryDeaths;
                    goldenDeathsInCP[1] = cpInfo.Stats.GoldenBerryDeathsSession;
                }
            }

            //Account for winning runs
            goldenDeathsAfterCP[0] += chapterStats.GoldenCollectedCount;
            goldenDeathsAfterCP[1] += chapterStats.GoldenCollectedCountSession;

            float crCheckpoint, crCheckpointSession;
            float goldenSuccessRateCp, goldenSuccessRateCpSession;

            //Calculate
            if (goldenDeathsInCP[0] + goldenDeathsAfterCP[0] == 0) crCheckpoint = float.NaN;
            else crCheckpoint = (float)goldenDeathsInCP[0] / (goldenDeathsInCP[0] + goldenDeathsAfterCP[0]);

            if (goldenDeathsInCP[1] + goldenDeathsAfterCP[1] == 0) crCheckpointSession = float.NaN;
            else crCheckpointSession = (float)goldenDeathsInCP[1] / (goldenDeathsInCP[1] + goldenDeathsAfterCP[1]);

            if (float.IsNaN(crCheckpoint)) goldenSuccessRateCp = float.NaN;
            else goldenSuccessRateCp = 1 - crCheckpoint;

            if (float.IsNaN(crCheckpointSession)) goldenSuccessRateCpSession = float.NaN;
            else goldenSuccessRateCpSession = 1 - crCheckpointSession;

            //Format
            if (float.IsNaN(crCheckpoint)) {
                format = format.Replace(CheckpointChokeRate, $"-%");
            } else {
                format = format.Replace(CheckpointChokeRate, $"{StatManager.FormatPercentage(crCheckpoint)}");
            }

            if (float.IsNaN(crCheckpointSession)) {
                format = format.Replace(CheckpointChokeRateSession, $"-%");
            } else {
                format = format.Replace(CheckpointChokeRateSession, $"{StatManager.FormatPercentage(crCheckpointSession)}");
            }

            //Golden Success Rate Checkpoint
            if (float.IsNaN(goldenSuccessRateCp)) {
                format = format.Replace(CheckpointGoldenSuccessRate, $"-%");
            } else {
                format = format.Replace(CheckpointGoldenSuccessRate, $"{StatManager.FormatPercentage(goldenSuccessRateCp)}");
            }

            if (float.IsNaN(goldenSuccessRateCpSession)) {
                format = format.Replace(CheckpointGoldenSuccessRateSession, $"-%");
            } else {
                format = format.Replace(CheckpointGoldenSuccessRateSession, $"{StatManager.FormatPercentage(goldenSuccessRateCpSession)}");
            }

            return format;
        }

        public override string FormatSummary(PathInfo chapterPath, ChapterStats chapterStats) {
            return null;
        }

        /// <summary>
        /// Gets the following data for every room: Golden Entries, Golden Success Rate, Golden Entries Session, Golden Success Rate Session
        /// </summary>
        public static Dictionary<RoomInfo, Tuple<int, float, int, float>> GetRoomData(PathInfo chapterPath, ChapterStats chapterStats) {
            Dictionary<RoomInfo, Tuple<int, float, int, float>> roomData = new Dictionary<RoomInfo, Tuple<int, float, int, float>>();

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {

                    bool pastRoom = false;
                    int[] goldenDeathsInRoom = new int[] { 0, 0 };
                    int[] goldenDeathsAfterRoom = new int[] { 0, 0 };

                    foreach (CheckpointInfo cpInfoTemp in chapterPath.Checkpoints) {
                        foreach (RoomInfo rInfoTemp in cpInfoTemp.Rooms) {
                            RoomStats rStats = chapterStats.GetRoom(rInfoTemp);

                            if (pastRoom) {
                                goldenDeathsAfterRoom[0] += rStats.GoldenBerryDeaths;
                                goldenDeathsAfterRoom[1] += rStats.GoldenBerryDeathsSession;
                            }

                            if (rInfo == rInfoTemp) {
                                pastRoom = true;
                                goldenDeathsInRoom[0] = rStats.GoldenBerryDeaths;
                                goldenDeathsInRoom[1] = rStats.GoldenBerryDeathsSession;
                            }
                        }
                    }

                    goldenDeathsAfterRoom[0] += chapterStats.GoldenCollectedCount;
                    goldenDeathsAfterRoom[1] += chapterStats.GoldenCollectedCountSession;

                    float crRoom, crRoomSession;
                    //Calculate
                    if (goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0] == 0) crRoom = float.NaN;
                    else crRoom = (float)goldenDeathsInRoom[0] / (goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0]);

                    if (goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1] == 0) crRoomSession = float.NaN;
                    else crRoomSession = (float)goldenDeathsInRoom[1] / (goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1]);

                    //Format
                    roomData.Add(rInfo, Tuple.Create(goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0],
                                                            1 - crRoom,
                                                            goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1],
                                                            1 - crRoomSession));
                }
            }

            return roomData;
        }

        /// <summary>
        /// Gets the following data for every room: Golden Entries, Golden Passes, Golden Entries Session, Golden Passes Session
        /// </summary>
        public static Dictionary<RoomInfo, Tuple<int, int, int, int>> GetRoomDataInts(PathInfo chapterPath, ChapterStats chapterStats) {
            Dictionary<RoomInfo, Tuple<int, int, int, int>> roomData = new Dictionary<RoomInfo, Tuple<int, int, int, int>>();

            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {

                    bool pastRoom = false;
                    int[] goldenDeathsInRoom = new int[] { 0, 0 };
                    int[] goldenDeathsAfterRoom = new int[] { 0, 0 };

                    foreach (CheckpointInfo cpInfoTemp in chapterPath.Checkpoints) {
                        foreach (RoomInfo rInfoTemp in cpInfoTemp.Rooms) {
                            RoomStats rStats = chapterStats.GetRoom(rInfoTemp);

                            if (pastRoom) {
                                goldenDeathsAfterRoom[0] += rStats.GoldenBerryDeaths;
                                goldenDeathsAfterRoom[1] += rStats.GoldenBerryDeathsSession;
                            }

                            if (rInfo == rInfoTemp) {
                                pastRoom = true;
                                goldenDeathsInRoom[0] = rStats.GoldenBerryDeaths;
                                goldenDeathsInRoom[1] = rStats.GoldenBerryDeathsSession;
                            }
                        }
                    }

                    goldenDeathsAfterRoom[0] += chapterStats.GoldenCollectedCount;
                    goldenDeathsAfterRoom[1] += chapterStats.GoldenCollectedCountSession;

                    //Format
                    roomData.Add(rInfo, Tuple.Create(goldenDeathsInRoom[0] + goldenDeathsAfterRoom[0],
                                                            goldenDeathsAfterRoom[0],
                                                            goldenDeathsInRoom[1] + goldenDeathsAfterRoom[1],
                                                            goldenDeathsAfterRoom[1]));
                }
            }

            return roomData;
        }


        /// <summary>
        /// Gets the following data for every room: Golden Entries Session, Golden Success Rate Session
        /// </summary>
        public static Dictionary<RoomInfo, Tuple<int, float>> GetRoomDataSession(PathInfo chapterPath, List<RoomInfo> lastRuns) {
            //Count golden deaths for old session
            Dictionary<RoomInfo, int> goldenDeathsSession = new Dictionary<RoomInfo, int>();
            int goldenCollectionsSession = 0;
            
            foreach (RoomInfo rInfo in lastRuns) {
                if (rInfo == null) {
                    goldenCollectionsSession++;
                    continue;
                }
                
                if (goldenDeathsSession.ContainsKey(rInfo)) {
                    goldenDeathsSession[rInfo]++;
                } else {
                    goldenDeathsSession.Add(rInfo, 1);
                }
            }

            //Calculate stats
            Dictionary<RoomInfo, Tuple<int, float>> roomData = new Dictionary<RoomInfo, Tuple<int, float>>();
            foreach (CheckpointInfo cpInfo in chapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {

                    bool pastRoom = false;
                    int goldenDeathsInRoom = 0;
                    int goldenDeathsAfterRoom = 0;

                    foreach (CheckpointInfo cpInfoTemp in chapterPath.Checkpoints) {
                        foreach (RoomInfo rInfoTemp in cpInfoTemp.Rooms) {
                            int roomGoldenBerryDeathsSession = 0;
                            if (goldenDeathsSession.ContainsKey(rInfoTemp)) {
                                roomGoldenBerryDeathsSession = goldenDeathsSession[rInfoTemp];
                            }

                            if (pastRoom) {
                                goldenDeathsAfterRoom += roomGoldenBerryDeathsSession;
                            }

                            if (rInfo == rInfoTemp) {
                                pastRoom = true;
                                goldenDeathsInRoom = roomGoldenBerryDeathsSession;
                            }
                        }
                    }

                    goldenDeathsAfterRoom += goldenCollectionsSession;

                    float crRoomSession;

                    //Calculate
                    if (goldenDeathsInRoom + goldenDeathsAfterRoom == 0) crRoomSession = float.NaN;
                    else crRoomSession = (float)goldenDeathsInRoom / (goldenDeathsInRoom + goldenDeathsAfterRoom);

                    //Format
                    roomData.Add(rInfo, Tuple.Create(goldenDeathsInRoom + goldenDeathsAfterRoom, 1 - crRoomSession));
                }
            }

            return roomData;
        }


        public override List<KeyValuePair<string, string>> GetPlaceholderExplanations() {
            return new List<KeyValuePair<string, string>>() {
                new KeyValuePair<string, string>(RoomChokeRate, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_CHOKE_RATE")),
                new KeyValuePair<string, string>(RoomChokeRateSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_CHOKE_RATE_SESSION")),
                new KeyValuePair<string, string>(CheckpointChokeRate, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_CHECKPOINT_CHOKE_RATE")),
                new KeyValuePair<string, string>(CheckpointChokeRateSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_CHECKPOINT_CHOKE_RATE_SESSION")),
                new KeyValuePair<string, string>(RoomGoldenSuccessRate, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_SUCCESS_RATE")),
                new KeyValuePair<string, string>(RoomGoldenSuccessRateSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_SUCCESS_RATE_SESSION")),
                new KeyValuePair<string, string>(CheckpointGoldenSuccessRate, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_CHECKPOINT_GOLDEN_SUCCESS_RATE")),
                new KeyValuePair<string, string>(CheckpointGoldenSuccessRateSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_CHECKPOINT_GOLDEN_SUCCESS_RATE_SESSION")),

                new KeyValuePair<string, string>(RoomGoldenEntries, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_ENTRIES")),
                new KeyValuePair<string, string>(RoomGoldenEntriesSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_ENTRIES_SESSION")),
                new KeyValuePair<string, string>(RoomGoldenSuccesses, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_SUCCESSES")),
                new KeyValuePair<string, string>(RoomGoldenSuccessesSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_SUCCESSES_SESSION")),

                new KeyValuePair<string, string>(RoomGoldenEntryChance, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_ENTRY_CHANCE")),
                new KeyValuePair<string, string>(RoomGoldenEntryChanceSession, Dialog.Clean("CCT_STAT_CHOKE_RATE_EXPLANATIONS_ROOM_GOLDEN_ENTRY_CHANCE_SESSION")),
            };
        }
        public override List<StatFormat> GetDefaultFormats() {
            return new List<StatFormat>() {
                new StatFormat(Dialog.Clean("CCT_STAT_CHOKE_RATE_FORMAT_TITLE_BASIC_CHOKE_RATE"), $"{Dialog.Clean("CCT_STAT_CHOKE_RATE_FORMAT_CONTENT_BASIC_CHOKE_RATE_1")}: {RoomChokeRate} ({Dialog.Clean("CCT_STAT_CHOKE_RATE_FORMAT_CONTENT_BASIC_CHOKE_RATE_2")}: {CheckpointChokeRate})"),
                new StatFormat(Dialog.Clean("CCT_STAT_CHOKE_RATE_FORMAT_TITLE_BASIC_GOLDEN_SUCCESS_RATE"), $"{Dialog.Clean("CCT_STAT_CHOKE_RATE_FORMAT_CONTENT_BASIC_GOLDEN_SUCCESS_RATE")}: {RoomGoldenSuccessRate} ({RoomGoldenSuccesses}/{RoomGoldenEntries})"),
            };
        }
    }
}
