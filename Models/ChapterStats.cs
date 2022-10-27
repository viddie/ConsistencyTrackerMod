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
        public static Action<string> LogCallback;

        public string CampaignName { get; set; }
        public string ChapterName { get; set; }
        public string ChapterSID { get; set; }
        public string ChapterSIDDialogSanitized { get; set; }
        public string ChapterDebugName { get; set; }
        public RoomStats CurrentRoom { get; set; }
        public Dictionary<string, RoomStats> Rooms { get; set; } = new Dictionary<string, RoomStats>();

        public ModState ModState { get; set; } = new ModState();

        /// <summary>Adds the attempt to the specified room.</summary>
        /// <param name="debugRoomName">debug name of the room.</param>
        /// <param name="success">if the attempt was successful.</param>
        public void AddAttempt(string debugRoomName, bool success) {
            RoomStats targetRoom = GetRoom(debugRoomName);
            targetRoom.AddAttempt(success);
        }

        /// <summary>Adds the attempt to the current room</summary>
        /// <param name="success">if the attempt was successful.</param>
        public void AddAttempt(bool success) {
            CurrentRoom.AddAttempt(success);
        }

        public void AddGoldenBerryDeath() {
            CurrentRoom.GoldenBerryDeaths++;
            CurrentRoom.GoldenBerryDeathsSession++;
        }

        public void SetCurrentRoom(string debugRoomName) {
            RoomStats targetRoom = GetRoom(debugRoomName);
            CurrentRoom = targetRoom;
        }

        public RoomStats GetRoom(string debugRoomName) {
            RoomStats targetRoom = null;
            if (Rooms.ContainsKey(debugRoomName)) {
                targetRoom = Rooms[debugRoomName];
            } else {
                targetRoom = new RoomStats() { DebugRoomName = debugRoomName };
                Rooms[debugRoomName] = targetRoom;
            }
            return targetRoom;
        }

        public void ResetCurrentSession() {
            foreach (string name in Rooms.Keys) {
                RoomStats room = Rooms[name];
                room.GoldenBerryDeathsSession = 0;
            }
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
    }

    public class RoomStats {
        public string DebugRoomName { get; set; }
        public int GoldenBerryDeaths { get; set; } = 0;
        public int GoldenBerryDeathsSession { get; set; } = 0;
        public List<bool> PreviousAttempts { get; set; } = new List<bool>();
        public bool IsUnplayed { get => PreviousAttempts.Count == 0; }
        public bool LastAttempt { get => PreviousAttempts[PreviousAttempts.Count - 1]; }
        public float LastFiveRate { get => AverageSuccessOverN(5); }
        public float LastTenRate { get => AverageSuccessOverN(10); }
        public float LastTwentyRate { get => AverageSuccessOverN(20); }
        public float MaxRate { get => AverageSuccessOverN(ChapterStats.MAX_ATTEMPT_COUNT); }

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
            if (PreviousAttempts.Count >= ChapterStats.MAX_ATTEMPT_COUNT) {
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

        public override string ToString() {
            string attemptList = string.Join(",", PreviousAttempts);
            return $"{DebugRoomName};{GoldenBerryDeaths};{GoldenBerryDeathsSession};{LastFiveRate};{LastTenRate};{LastTwentyRate};{MaxRate};{attemptList}";
        }

        public static RoomStats ParseString(string line) {
            //ChapterStats.LogCallback($"RoomStats -> Parsing line '{line}'");

            List<string> lines = line.Split(new string[] { ";" }, StringSplitOptions.None).ToList();
            //ChapterStats.LogCallback($"\tlines.Count = {lines.Count}");
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

            //if (name == "a-01") {
            //    ChapterStats.LogCallback($"RoomStats.ParseString -> 'a-01': GB Deaths Session: {gbDeathsSession}");
            //}

            //int gbDeaths = int.Parse(lines[1]);
            //string attemptListString = lines[6];

            List<bool> attemptList = new List<bool>();
            foreach (string boolStr in attemptListString.Split(new char[] { ',' })) {
                //ChapterStats.LogCallback($"\tIn loop -> {boolStr}");
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
}
