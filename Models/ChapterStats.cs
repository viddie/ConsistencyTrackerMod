using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Models {
    [Serializable]
    public class ChapterStats {
        public static readonly int MAX_ATTEMPT_COUNT = 100;
        public static Action<string> LogCallback;

        public string ChapterName { get; set; }
        public RoomStats CurrentRoom { get; set; }
        public Dictionary<string, RoomStats> Rooms { get; set; } = new Dictionary<string, RoomStats>();

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

        public string ToCurrentRoomString() {
            return $"{CurrentRoom}\n{ChapterName}\n";
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
    }

    public class RoomStats {
        public string DebugRoomName { get; set; }
        public List<bool> PreviousAttempts { get; set; } = new List<bool>();
        public float LastFiveRate { get => AverageSuccessOverN(5); }
        public float LastTenRate { get => AverageSuccessOverN(10); }
        public float LastTwentyRate { get => AverageSuccessOverN(20); }
        public float MaxRate { get => AverageSuccessOverN(ChapterStats.MAX_ATTEMPT_COUNT); }

        public float AverageSuccessOverN(int n) {
            int countSucceeded = 0;
            int countTotal = 0;

            for (int i = 0; i < n; i++) {
                int neededIndex = PreviousAttempts.Count - 1 - i;
                if (neededIndex < 0) break;

                countTotal++;
                if (PreviousAttempts[neededIndex]) countSucceeded++;
            }

            return (float)countSucceeded / countTotal;
        }

        public void AddAttempt(bool success) {
            if (PreviousAttempts.Count >= ChapterStats.MAX_ATTEMPT_COUNT) {
                PreviousAttempts.RemoveAt(0);
            }

            PreviousAttempts.Add(success);
        }

        public override string ToString() {
            string attemptList = string.Join(",", PreviousAttempts);
            return $"{DebugRoomName};{LastFiveRate};{LastTenRate};{LastTwentyRate};{MaxRate};{attemptList}";
        }

        public static RoomStats ParseString(string line) {
            //ChapterStats.LogCallback($"RoomStats -> Parsing line '{line}'");

            List<string> lines = line.Split(new string[] { ";" }, StringSplitOptions.None).ToList();
            //ChapterStats.LogCallback($"\tlines.Count = {lines.Count}");
            string name = lines[0];
            //ChapterStats.LogCallback($"RoomStats -> Read debug name: '{name}'");
            string attemptListString = lines[5];
            //ChapterStats.LogCallback($"RoomStats -> Read attempts as string: '{attemptListString}'");

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
            };
        }
    }
}
