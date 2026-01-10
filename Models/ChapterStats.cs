using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.EverestInterop;
using Celeste.Mod.ConsistencyTracker.Stats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Celeste.Mod.ConsistencyTracker.Models {
    [Serializable]
    public class ChapterStats {
        public static readonly int MAX_ATTEMPT_COUNT = 100;

        [JsonProperty("campaignName")]
        public string CampaignName { get; set; }

        [JsonProperty("chapterName")]
        public string ChapterName { get; set; }

        [JsonProperty("chapterSID")]
        public string ChapterSID { get; set; }

        [JsonProperty("chapterSIDDialogSanitized")]
        public string ChapterSIDDialogSanitized { get; set; }

        [JsonProperty("mapBin")]
        public string MapBin { get; set; }

        [JsonProperty("chapterDebugName")]
        public string ChapterDebugName { get; set; }

        [JsonProperty("sideName")]
        public string SideName { get; set; }

        [JsonProperty("goldenCollectedCount")]
        public int GoldenCollectedCount { get; set; } = 0;
        [JsonProperty("goldenCollectedCountSession")]
        public int GoldenCollectedCountSession { get; set; } = 0;
        [JsonProperty("goldenType")]
        public GoldenType GoldenBerryType { get; set; } = GoldenType.Golden;
        [JsonIgnore]
        public bool GoldenCollectedThisRun { get; set; } = false;

        [JsonProperty("sessionStarted")]
        public DateTime SessionStarted { get; set; }

        [JsonProperty("currentRoom")]
        public RoomStats CurrentRoom { get; set; }

        [JsonProperty("rooms")]
        public Dictionary<string, RoomStats> Rooms { get; set; } = new Dictionary<string, RoomStats>();

        //A list of debug room names of all golden runs in the session
        //Special value: null as entry in this list is a golden win!
        [JsonProperty("lastGoldenRuns")]
        public List<string> LastGoldenRuns { get; set; } = new List<string>();

        [JsonProperty("oldSessions")]
        public List<OldSession> OldSessions { get; set; } = new List<OldSession>();

        //[JsonProperty("modState")] //Uhhh i forgot to rename this. should check if this is used anywhere before adding JsonProperty
        public ModState ModState { get; set; } = new ModState();

        [JsonProperty("gameData")]
        public GameData GameData { get; set; } = new GameData();

        /// <summary>Adds the attempt to the specified room.</summary>
        /// <param name="debugRoomName">debug name of the room.</param>
        /// <param name="success">if the attempt was successful.</param>
        public void AddAttempt(bool success, string debugRoomName) {
            RoomStats targetRoom = GetRoom(debugRoomName);
            targetRoom.AddAttempt(success);
        }

        /// <summary>Adds the attempt to the current room</summary>
        /// <param name="success">if the attempt was successful.</param>
        public void AddAttempt(bool success) {
            CurrentRoom.AddAttempt(success);

            bool doNegativeStreakTracking = ConsistencyTrackerModule.Instance.ModSettings.TrackNegativeStreaks;
            if (success) {
                if (CurrentRoom.SuccessStreak <= 0) {
                    CurrentRoom.SuccessStreak = 1;
                } else { 
                    CurrentRoom.SuccessStreak++;
                }
                
                if (CurrentRoom.SuccessStreak > CurrentRoom.SuccessStreakBest) {
                    CurrentRoom.SuccessStreakBest = CurrentRoom.SuccessStreak;
                }
            } else {
                if (CurrentRoom.SuccessStreak > 0 || !doNegativeStreakTracking) {
                    CurrentRoom.SuccessStreak = 0;
                } else if (CurrentRoom.SuccessStreak == 0) {
                    CurrentRoom.SuccessStreak = -2;
                } else {
                    CurrentRoom.SuccessStreak--;
                }
            }
        }

        public void AddGoldenBerryDeath(string debugRoomName=null) {
            RoomStats rStats = CurrentRoom;
            if (debugRoomName != null) {
                if (Rooms.ContainsKey(debugRoomName)) {
                    rStats = Rooms[debugRoomName];
                }
            }
            
            LastGoldenRuns.Add(rStats.DebugRoomName);
            rStats.GoldenBerryDeaths++;
            rStats.GoldenBerryDeathsSession++;
        }

        public void CollectedGolden(GoldenType type) {
            GoldenCollectedCount++;
            GoldenCollectedCountSession++;
            GoldenCollectedThisRun = true;
            LastGoldenRuns.Add(null);
            GoldenBerryType = type;
        }

        public void SetCurrentRoom(string debugRoomName) {
            RoomStats targetRoom = GetRoom(debugRoomName);
            CurrentRoom = targetRoom;
        }

        public RoomStats GetRoom(RoomInfo rInfo) {
            return GetRoom(rInfo.DebugRoomName);
        }
        public RoomStats GetRoom(string debugRoomName) {
            RoomStats targetRoom;
            if (Rooms.ContainsKey(debugRoomName)) {
                targetRoom = Rooms[debugRoomName];
            } else {
                targetRoom = new RoomStats() { DebugRoomName = debugRoomName };
                Rooms[debugRoomName] = targetRoom;
            }
            return targetRoom;
        }

        /// <summary>
        /// Resets the current session stats. Called once per session on chapter entry
        /// </summary>
        public void ResetCurrentSession(PathInfo chapterPath) {
            //Store current session data in session history first
            try {
                StoreOldSessionData(chapterPath);
            } catch (Exception ex) {
                ConsistencyTrackerModule.Instance.Log($"There was an exception when storing old session data for chapter '{ChapterDebugName}': {ex}");
                ConsistencyTrackerModule.Instance.Log($"Old Session count: {OldSessions.Count}", isFollowup: true);
                ConsistencyTrackerModule.Instance.Log($"Path is null: {chapterPath == null}", isFollowup: true);
            }

            //Then reset current session data
            foreach (string name in Rooms.Keys) {
                RoomStats room = Rooms[name];
                room.GoldenBerryDeathsSession = 0;
            }

            LastGoldenRuns = new List<string>(); //Create new list since old one is used in old session data
            GoldenCollectedCountSession = 0;
            GoldenCollectedThisRun = false;
            SessionStarted = DateTime.Now;
        }

        /// <summary>
        /// Stores the current stats in the session data history.
        /// </summary>
        public void StoreOldSessionData(PathInfo chapterPath) {
            //Backwards compatibility. If the session started is null, it means the session was started before the session history was implemented
            if (SessionStarted == null || SessionStarted == DateTime.MinValue) {
                return;
            }
            //No path = no session trackable
            if (chapterPath == null) {
                return;
            }

            Tuple<double, double> runDistanceAvgs = AverageLastRunsStat.GetAverageRunDistance(chapterPath, this);
            
            RoomInfo alltimePB = PersonalBestStat.GetFurthestDeathRoom(chapterPath, this);
            int alltimePBDeaths = 0;
            if (alltimePB != null) {
                alltimePBDeaths = GetRoom(alltimePB).GoldenBerryDeaths;
            }
            Tuple<RoomInfo, int> sessionPB = PersonalBestStat.GetSessionPBRoomFromLastRuns(chapterPath, LastGoldenRuns);
            RoomInfo sessionPBRoom = null;
            int sessionPBDeaths = 0;
            if (sessionPB != null) {
                sessionPBRoom = sessionPB.Item1;
                sessionPBDeaths = sessionPB.Item2;
            }

            OldSession oldSession = new OldSession() {
                SessionStarted = SessionStarted,
                TotalGoldenDeaths = chapterPath.Stats.GoldenBerryDeaths,
                TotalGoldenDeathsSession = chapterPath.Stats.GoldenBerryDeathsSession,
                TotalSuccessRate = chapterPath.Stats.SuccessRate,
                LastGoldenRuns = LastGoldenRuns,
                AverageRunDistance = (float)runDistanceAvgs.Item1,
                AverageRunDistanceSession = (float)runDistanceAvgs.Item2,

                PBRoomName = alltimePB?.DebugRoomName,
                PBRoomDeaths = alltimePBDeaths,
                SessionPBRoomName = sessionPBRoom?.DebugRoomName,
                SessionPBRoomDeaths = sessionPBDeaths,

                TotalGoldenCollections = GoldenCollectedCount,
                TotalGoldenCollectionsSession = GoldenCollectedCountSession,
            };

            if (OldSessions.Count == 0) {
                OldSessions.Add(oldSession);
                return;
            }

            OldSession olderSession = OldSessions[OldSessions.Count - 1];
            if (OldSession.IsSessionEmpty(oldSession, olderSession)) {
                ConsistencyTrackerModule.Instance.Log($"Old session data from '{SessionStarted}' was empty, not saving...");
                return;
            }
            
            ConsistencyTrackerModule.Instance.Log($"Saving old session data from '{SessionStarted}'");
            if (olderSession.SessionStarted.Date != oldSession.SessionStarted.Date) {
                ConsistencyTrackerModule.Instance.Log($"Saving as new session", isFollowup: true);
                OldSessions.Add(oldSession);
                return;
            }
            
            ConsistencyTrackerModule.Instance.Log($"Merging into previous session '{olderSession.SessionStarted}' from the same day", isFollowup: true);
            //Calculate new session average
            int osRuns = olderSession.TotalGoldenDeathsSession + olderSession.TotalGoldenCollections;
            int nsRuns = oldSession.TotalGoldenDeathsSession + oldSession.TotalGoldenCollectionsSession;
            float osAvg = olderSession.AverageRunDistanceSession;
            float nsAvg = oldSession.AverageRunDistanceSession;
            float newAvg = (osRuns * osAvg + nsRuns * nsAvg) / (osRuns + nsRuns);
            olderSession.AverageRunDistanceSession = newAvg;

            //Copy over stats
            olderSession.TotalGoldenDeaths = oldSession.TotalGoldenDeaths;
            olderSession.TotalGoldenDeathsSession += oldSession.TotalGoldenDeathsSession;
            olderSession.TotalSuccessRate = oldSession.TotalSuccessRate;
            olderSession.LastGoldenRuns.AddRange(oldSession.LastGoldenRuns);
            olderSession.AverageRunDistance = oldSession.AverageRunDistance;

            //Copy over golden collection stats
            olderSession.TotalGoldenCollections = oldSession.TotalGoldenCollections;
            olderSession.TotalGoldenCollectionsSession += oldSession.TotalGoldenCollectionsSession;

            //Copy over PBs
            olderSession.PBRoomName = oldSession.PBRoomName; //Always use newer session global PB, since PBs can't go backwards
            RoomInfo olderSessionPB = chapterPath.GetRoom(olderSession.SessionPBRoomName);
            RoomInfo oldSessionPB = chapterPath.GetRoom(oldSession.SessionPBRoomName);

            if (olderSessionPB == null) { //If older session didn't have a PB
                olderSession.SessionPBRoomName = oldSession.SessionPBRoomName;
                olderSession.SessionPBRoomDeaths = oldSession.SessionPBRoomDeaths;
            } else if (oldSessionPB == null) { //If newer session didn't have a PB
                //Do nothing, use older session stats
            } else if (oldSessionPB.RoomNumberInChapter > olderSessionPB.RoomNumberInChapter) { //If newer session PB is better than older session PB
                olderSession.SessionPBRoomName = oldSession.SessionPBRoomName;
                olderSession.SessionPBRoomDeaths = oldSession.SessionPBRoomDeaths;
            }
        }
        
        /// <summary>
        /// Resets the current run stats. Called on every chapter entry
        /// </summary>
        public void ResetCurrentRun() {
            foreach (string name in Rooms.Keys) {
                RoomStats room = Rooms[name];
                room.DeathsInCurrentRun = 0;
            }
            GoldenCollectedThisRun = false;
        }
        
        public string ToChapterStatsString() {
            List<string> lines = new List<string>();
            lines.Add($"{CurrentRoom}");

            foreach (string key in Rooms.Keys) {
                lines.Add($"{Rooms[key]}");
            }

            return string.Join("\n", lines)+"\n";
        }

        public static ChapterStats ParseString(string content) {
            List<string> lines = content.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();
            ChapterStats stats = new ChapterStats();

            foreach (string line in lines) {
                if (line.Trim() == "") break; //Skip last line

                RoomStats room = RoomStats.ParseString(line);
                if (stats.Rooms.Count == 0) { //First element is always the current room
                    stats.CurrentRoom = room;
                }

                if(!stats.Rooms.ContainsKey(room.DebugRoomName)) //Skip duplicate entries, as the current room is always the first line and also found later on
                    stats.Rooms.Add(room.DebugRoomName, room);
            }

            return stats;
        }

        private Dictionary<string, int> UnvisitedRoomsToRoomNumber = new Dictionary<string, int>();
        public void OutputSummary(string outPath, PathInfo info, int attemptCount) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Tracker summary for chapter '{ChapterDebugName}'");
            sb.AppendLine($"");
            sb.AppendLine($"--- Golden Berry Deaths ---"); //Room->Checkpoint->Chapter + 1

            int chapterDeaths = 0;
            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) continue; //Skip rooms the player has not yet visited.

                    RoomStats rStats = Rooms[rInfo.DebugRoomName];
                    chapterDeaths += rStats.GoldenBerryDeaths;
                }
            }

            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                int checkpointDeaths = 0;

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) continue; //Skip rooms the player has not yet visited.

                    RoomStats rStats = Rooms[rInfo.DebugRoomName];
                    checkpointDeaths += rStats.GoldenBerryDeaths;
                }


                string percentStr = (checkpointDeaths / (double)chapterDeaths).ToString("P2", CultureInfo.InvariantCulture);
                sb.AppendLine($"{cpInfo.Name}: {checkpointDeaths} ({percentStr})");

                int roomNumber = 0;
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    roomNumber++;

                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) {
                        UnvisitedRoomsToRoomNumber.Add(rInfo.DebugRoomName, roomNumber);
                        sb.AppendLine($"\t{cpInfo.Abbreviation}-{roomNumber}: 0");
                    } else {
                        RoomStats rStats = Rooms[rInfo.DebugRoomName];
                        rStats.RoomNumber = roomNumber;
                        sb.AppendLine($"\t{cpInfo.Abbreviation}-{roomNumber}: {rStats.GoldenBerryDeaths}");
                    }
                }
            }
            sb.AppendLine($"Total Golden Berry Deaths: {chapterDeaths}");


            sb.AppendLine($"");
            sb.AppendLine($"");


            sb.AppendLine($"--- Consistency Stats ---");
            sb.AppendLine($"- Success Rate"); //Room->Checkpoint->Chapter

            double chapterSuccessRateSum = 0f;
            int chapterRoomCount = 0;
            int chapterSuccesses = 0;
            int chapterAttempts = 0;
            double chapterGoldenChance = 1;

            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                double checkpointSuccessRateSum = 0f;
                int checkpointRoomCount = 0;
                int checkpointSuccesses = 0;
                int checkpointAttempts = 0;

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) continue; //Skip rooms the player has not yet visited.

                    RoomStats rStats = Rooms[rInfo.DebugRoomName];
                    float rRate = rStats.AverageSuccessOverN(attemptCount);

                    checkpointSuccessRateSum += rRate;
                    checkpointRoomCount++;

                    chapterSuccessRateSum += rRate;
                    chapterRoomCount++;

                    int rSuccesses = rStats.SuccessesOverN(attemptCount);
                    int rAttempts = rStats.AttemptsOverN(attemptCount);

                    checkpointSuccesses += rSuccesses;
                    checkpointAttempts += rAttempts;

                    chapterSuccesses += rSuccesses;
                    chapterAttempts += rAttempts;

                    cpInfo.GoldenChance *= rRate;
                    chapterGoldenChance *= rRate;
                }

                string cpPercentStr = (checkpointSuccessRateSum / checkpointRoomCount).ToString("P2", CultureInfo.InvariantCulture);
                sb.AppendLine($"{cpInfo.Name}: {cpPercentStr} ({checkpointSuccesses} successes / {checkpointAttempts} attempts)");

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) {
                        string rPercentStr = 0.ToString("P2", CultureInfo.InvariantCulture);
                        sb.AppendLine($"\t{cpInfo.Abbreviation}-{UnvisitedRoomsToRoomNumber[rInfo.DebugRoomName]}: {rPercentStr} (0 / 0)");
                    } else {
                        RoomStats rStats = Rooms[rInfo.DebugRoomName];
                        string rPercentStr = rStats.AverageSuccessOverN(attemptCount).ToString("P2", CultureInfo.InvariantCulture);
                        sb.AppendLine($"\t{cpInfo.Abbreviation}-{rStats.RoomNumber}: {rPercentStr} ({rStats.SuccessesOverN(attemptCount)} / {rStats.AttemptsOverN(attemptCount)})");
                    }
                }
            }
            string cPercentStr = (chapterSuccessRateSum / chapterRoomCount).ToString("P2", CultureInfo.InvariantCulture);
            sb.AppendLine($"Total Success Rate: {cPercentStr} ({chapterSuccesses} successes / {chapterAttempts} attempts)");


            sb.AppendLine($"");


            sb.AppendLine($"- Choke Rate"); //Choke Rate
            Dictionary<CheckpointInfo, int> cpChokeRates = new Dictionary<CheckpointInfo, int>();
            Dictionary<RoomInfo, int> roomChokeRates = new Dictionary<RoomInfo, int>();

            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                cpChokeRates.Add(cpInfo, 0);

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    roomChokeRates.Add(rInfo, 0);

                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) continue; //Skip rooms the player has not yet visited.
                    roomChokeRates[rInfo] = Rooms[rInfo.DebugRoomName].GoldenBerryDeaths;
                    cpChokeRates[cpInfo] += Rooms[rInfo.DebugRoomName].GoldenBerryDeaths;
                }
            }

            sb.AppendLine($"");
            sb.AppendLine($"Room Name,Choke Rate,Golden Runs to Room,Room Deaths");
            bool goldenAchieved = true;

            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) { //For every room

                    int goldenRunsToRoom = 0;
                    bool foundRoom = false;

                    foreach (CheckpointInfo cpInfoTemp in info.Checkpoints) { //Iterate all remaining rooms and sum up their golden deaths
                        foreach (RoomInfo rInfoTemp in cpInfoTemp.Rooms) {
                            if (rInfoTemp == rInfo) foundRoom = true;
                            if (foundRoom) {
                                goldenRunsToRoom += roomChokeRates[rInfoTemp];
                            }
                        }
                    }

                    if (goldenAchieved) goldenRunsToRoom++;

                    int roomNumber = -1;
                    if (Rooms.ContainsKey(rInfo.DebugRoomName)) {
                        roomNumber = Rooms[rInfo.DebugRoomName].RoomNumber;
                    } else {
                        roomNumber = UnvisitedRoomsToRoomNumber[rInfo.DebugRoomName];
                    }

                    float roomChokeRate = 0f;
                    if (goldenRunsToRoom != 0) {
                        roomChokeRate = (float)roomChokeRates[rInfo] / (float)goldenRunsToRoom;
                    }

                    sb.AppendLine($"{cpInfo.Abbreviation}-{roomNumber},{roomChokeRate},{goldenRunsToRoom},{roomChokeRates[rInfo]}");

                }
            }


            sb.AppendLine($"");


            sb.AppendLine($"- Golden Chance"); //Checkpoint->Chapter
            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                string cpPercentStr = cpInfo.GoldenChance.ToString("P2", CultureInfo.InvariantCulture);
                sb.AppendLine($"{cpInfo.Name}: {cpPercentStr}");
            }
            cPercentStr = chapterGoldenChance.ToString("P2", CultureInfo.InvariantCulture);
            sb.AppendLine($"Total Golden Chance: {cPercentStr}");


            sb.AppendLine($"");

            StringBuilder sbGoldenRun = new StringBuilder();
            sbGoldenRun.AppendLine($"Room #,Room Name,Start->Room,Room->End");
            int roomIndexNumber = 0;

            sb.AppendLine($"- Golden Chance Over A Run"); //Room-wise from start to room / room to end
            foreach (CheckpointInfo cpInfo in info.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    roomIndexNumber++;
                    if (!Rooms.ContainsKey(rInfo.DebugRoomName)) {
                        string gcToPercentI = 0.ToString("P2", CultureInfo.InvariantCulture);
                        string gcFromPercentI = 0.ToString("P2", CultureInfo.InvariantCulture);
                        sb.AppendLine($"\t{cpInfo.Abbreviation}-{UnvisitedRoomsToRoomNumber[rInfo.DebugRoomName]}:\tStart -> Room: '{gcToPercentI}',\tRoom -> End '{gcFromPercentI}'");
                        sbGoldenRun.AppendLine($"{roomIndexNumber},{cpInfo.Abbreviation}-{UnvisitedRoomsToRoomNumber[rInfo.DebugRoomName]},{0},{0}");
                        continue;
                    }

                    RoomStats rStats = Rooms[rInfo.DebugRoomName];

                    double gcToRoom = 1;
                    double gcFromRoom = 1;

                    bool hasReachedRoom = false;
                    foreach (CheckpointInfo innerCpInfo in info.Checkpoints) {
                        foreach (RoomInfo innerRInfo in innerCpInfo.Rooms) {
                            if (innerRInfo.DebugRoomName == rInfo.DebugRoomName) hasReachedRoom = true;

                            if (!Rooms.ContainsKey(innerRInfo.DebugRoomName)) {
                                if (hasReachedRoom) {
                                    gcFromRoom *= 0;
                                } else {
                                    gcToRoom *= 0;
                                }

                            } else {
                                RoomStats innerRStats = Rooms[innerRInfo.DebugRoomName];
                                if (hasReachedRoom) {
                                    gcFromRoom *= innerRStats.AverageSuccessOverN(attemptCount);
                                } else {
                                    gcToRoom *= innerRStats.AverageSuccessOverN(attemptCount);
                                }
                            }
                        }
                    }

                    string gcToPercent = gcToRoom.ToString("P2", CultureInfo.InvariantCulture);
                    string gcFromPercent = gcFromRoom.ToString("P2", CultureInfo.InvariantCulture);
                    sb.AppendLine($"\t{cpInfo.Abbreviation}-{rStats.RoomNumber}:\tStart -> Room: '{gcToPercent}',\tRoom -> End '{gcFromPercent}'");
                    sbGoldenRun.AppendLine($"{roomIndexNumber},{cpInfo.Abbreviation}-{rStats.RoomNumber},{gcToRoom},{gcFromRoom}");
                }
            }
            sb.AppendLine($"- Golden Chance Over A Run (Google Sheets pastable values)"); //Room-wise from start to room / room to end
            sb.AppendLine(sbGoldenRun.ToString());

            sb.AppendLine($"");


            File.WriteAllText(outPath, sb.ToString());
        }

        public void SetChapterMetaInfo(ChapterMetaInfo chapterInfo) {
            ChapterDebugName = chapterInfo.ChapterDebugName;
            CampaignName = chapterInfo.CampaignName;
            ChapterName = chapterInfo.ChapterName;
            MapBin = chapterInfo.MapBin;
            ChapterSID = chapterInfo.ChapterSID;
            ChapterSIDDialogSanitized = chapterInfo.ChapterSIDDialogSanitized;
            SideName = chapterInfo.SideName;
        }

        /*
         Format for Sankey Diagram (https://sankeymatic.com/build/)
0m [481] Death //cp1
0m [508] 500m //totalDeaths - cp1 + 1
500m [172] Death //cp2
500m [336] 1000m //totalDeaths - (cp1+cp2) + 1
1000m [136] Death //...
1000m [200] 1500m
1500m [65] Death
1500m [135] 2000m
2000m [48] Death
2000m [87] 2500m
2500m [41] Death
2500m [46] 3000m
3000m [45] Death
3000m [1] Golden Berry
         */
        public void MakeFgrChanges(string uid) {
            var newRooms = new Dictionary<string, RoomStats>();
            foreach (var roomEntry in Rooms) {
                string name = roomEntry.Key;
                RoomStats rStats = roomEntry.Value;
                
                // Just to make sure the same object is used in case of recent deserialization
                if (rStats.DebugRoomName == CurrentRoom.DebugRoomName) {
                    CurrentRoom = rStats;
                }
                
                string newName = ConsistencyTrackerModule.GetRoomName(name, true, uid);
                rStats.DebugRoomName = newName;
                newRooms[newName] = rStats;
            }
            Rooms = newRooms;
            
            // Reset golden collects
            GoldenCollectedCount = 0;
            GoldenCollectedCountSession = 0;
            
            // Fix old sessions
            foreach (OldSession oldSession in OldSessions) {
                var newLastGoldenRuns = new List<string>();
                foreach (string run in oldSession.LastGoldenRuns) {
                    // Ignore winning runs in old sessions.
                    if (run != null) {
                        string newRunName = ConsistencyTrackerModule.GetRoomName(run, true, uid);
                        newLastGoldenRuns.Add(newRunName);
                    }
                }
                oldSession.LastGoldenRuns = newLastGoldenRuns;
                oldSession.TotalGoldenCollections = 0;
                oldSession.TotalGoldenCollectionsSession = 0;
                
                // Adjust pb room names
                if (oldSession.PBRoomName != null) {
                    oldSession.PBRoomName = ConsistencyTrackerModule.GetRoomName(oldSession.PBRoomName, true, uid);
                }
                if (oldSession.SessionPBRoomName != null) {
                    oldSession.SessionPBRoomName =
                        ConsistencyTrackerModule.GetRoomName(oldSession.SessionPBRoomName, true, uid);
                }
            }
        }
    }

    public class RoomStats {
        [JsonProperty("debugRoomName")]
        public string DebugRoomName { get; set; }

        [JsonIgnore]
        public string ActualDebugRoomName {
            get {
                string[] split = DebugRoomName.Split(':');
                if (split.Length != 2) return DebugRoomName;
                return split[1];
            }
        }

        [JsonProperty("goldenBerryDeaths")]
        public int GoldenBerryDeaths { get; set; } = 0;

        [JsonProperty("goldenBerryDeathsSession")]
        public int GoldenBerryDeathsSession { get; set; } = 0;

        [JsonProperty("previousAttempts")]
        public List<bool> PreviousAttempts { get; set; } = new List<bool>();

        [JsonIgnore]
        public bool IsUnplayed { get => PreviousAttempts.Count == 0; }

        [JsonIgnore]
        public bool LastAttempt {
            get {
                if (IsUnplayed) return false;
                return PreviousAttempts[PreviousAttempts.Count - 1];
            } 
        }

        [JsonProperty("lastFiveRate")]
        public float LastFiveRate { get => AverageSuccessOverN(5); }

        [JsonProperty("lastTenRate")]
        public float LastTenRate { get => AverageSuccessOverN(10); }

        [JsonProperty("lastTwentyRate")]
        public float LastTwentyRate { get => AverageSuccessOverN(20); }

        [JsonProperty("maxRate")]
        public float MaxRate { get => AverageSuccessOverN(ChapterStats.MAX_ATTEMPT_COUNT); }

        [JsonProperty("successStreak")]
        public int SuccessStreak { get; set; } = 0;

        [JsonProperty("successStreakBest")]
        public int SuccessStreakBest { get; set; } = 0;


        [JsonProperty("deathsInCurrentRun")]
        public int DeathsInCurrentRun { get; set; } = 0;

        [JsonProperty("timeSpentInRoom")]
        public long TimeSpentInRoom { get; set; } = 0;

        [JsonProperty("timeSpentInRoomFirstPlaythrough")]
        public long TimeSpentInRoomFirstPlaythrough { get; set; } = 0;

        [JsonProperty("timeSpentInRoomInRuns")]
        public long TimeSpentInRoomInRuns { get; set; } = 0;

        [JsonIgnore]
        public int RoomNumber { get; set; }

        public float AverageSuccessOverN(int n) {
            int countSucceeded = 0;
            int countTotal = 0;

            for (int i = 0; i < n; i++) {
                int neededIndex = PreviousAttempts.Count - 1 - i;
                if (neededIndex < 0) break;

                countTotal++;
                if (PreviousAttempts[neededIndex]) countSucceeded++;
            }

            if (countTotal == 0) return 0;

            return (float)countSucceeded / countTotal;
        }
        public float AverageSuccessOverSelectedN() {
            int attemptCount = ConsistencyTrackerModule.Instance.ModSettings.SummarySelectedAttemptCount;
            return AverageSuccessOverN(attemptCount);
        }

        public int SuccessesOverN(int n) {
            int countSucceeded = 0;
            for (int i = 0; i < n; i++) {
                int neededIndex = PreviousAttempts.Count - 1 - i;
                if (neededIndex < 0) break;
                if (PreviousAttempts[neededIndex]) countSucceeded++;
            }
            return countSucceeded;
        }
        public int SuccessesOverSelectedN() {
            int attemptCount = ConsistencyTrackerModule.Instance.ModSettings.SummarySelectedAttemptCount;
            return SuccessesOverN(attemptCount);
        }

        public int AttemptsOverN(int n) {
            int countTotal = 0;
            for (int i = 0; i < n; i++) {
                int neededIndex = PreviousAttempts.Count - 1 - i;
                if (neededIndex < 0) break;
                countTotal++;
            }
            return countTotal;
        }
        public int AttemptsOverSelectedN() {
            int attemptCount = ConsistencyTrackerModule.Instance.ModSettings.SummarySelectedAttemptCount;
            return AttemptsOverN(attemptCount);
        }

        public void AddAttempt(bool success) {
            while (PreviousAttempts.Count >= ChapterStats.MAX_ATTEMPT_COUNT) {
                PreviousAttempts.RemoveAt(0);
            }

            PreviousAttempts.Add(success);
        }

        public void RemoveLastAttempt() {
            if (PreviousAttempts.Count <= 0) {
                return;
            }
            PreviousAttempts.RemoveAt(PreviousAttempts.Count-1);
        }

        public long GetTimeForCategory(TimeCategory category) {
            if (category == TimeCategory.FirstPlaythrough) {
                return TimeSpentInRoomFirstPlaythrough;
            } else if (category == TimeCategory.Practice) {
                return TimeSpentInRoom - TimeSpentInRoomFirstPlaythrough - TimeSpentInRoomInRuns;
            } else if (category == TimeCategory.Runs) {
                return TimeSpentInRoomInRuns;
            } else {
                return TimeSpentInRoom;
            }
        }

        public override string ToString() {
            string attemptList = string.Join(",", PreviousAttempts);
            return $"{DebugRoomName};{GoldenBerryDeaths};{GoldenBerryDeathsSession};{LastFiveRate};{LastTenRate};{LastTwentyRate};{MaxRate};{attemptList}";
        }

        public static RoomStats ParseString(string line) {
            //ChapterStats.LogCallback($"RoomStats -> Parsing line '{line}'");

            List<string> lines = line.Split(new string[] { ";" }, StringSplitOptions.None).ToList();
            string name = lines[0];
            int gbDeaths = 0;
            int gbDeathsSession = 0;
            string attemptListString;

            try {
                attemptListString = lines[7];
                gbDeaths = int.Parse(lines[1]);
                gbDeathsSession = int.Parse(lines[2]);
            } catch (Exception) {
                try {
                    attemptListString = lines[6];
                    gbDeaths = int.Parse(lines[1]);
                } catch (Exception) {
                    attemptListString = lines[5];
                }
            }

            List<bool> attemptList = new List<bool>();
            foreach (string boolStr in attemptListString.Split(new char[] { ',' })) {
                if (boolStr.Trim() == "") continue;

                bool value = bool.Parse(boolStr);
                attemptList.Add(value);
            }

            return new RoomStats() {
                DebugRoomName = name,
                PreviousAttempts = attemptList,
                GoldenBerryDeaths = gbDeaths,
                GoldenBerryDeathsSession = gbDeathsSession,
            };
        }
    }

    public class GameData {
        [JsonProperty("completed")]
        public bool Completed { get; set; }
        
        [JsonProperty("fullClear")]
        public bool FullClear { get; set; }

        [JsonProperty("totalTime")]
        public long TotalTime { get; set; }
        
        [JsonProperty("totalDeaths")]
        public long TotalDeaths { get; set; }
    }
}
