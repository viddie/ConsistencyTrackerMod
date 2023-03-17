using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class PathRecorder {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        public static string DefaultCheckpointName = $"Start";

        //Remember all previously visited rooms. Rooms only get added to the first checkpoint they appear in.
        public HashSet<string> VisitedRooms { get; set; } = new HashSet<string>();

        public List<List<string>> Checkpoints { get; set; } = new List<List<string>>() {};
        public List<string> CheckpointNames = new List<string>() {};
        public List<string> CheckpointAbbreviations = new List<string>() {};

        public HashSet<Vector2> CheckpointsVisited { get; set; } = new HashSet<Vector2>();

        public int TotalRecordedRooms => VisitedRooms.Count;

        public void AddRoom(string name) {
            if (VisitedRooms.Contains(name)) {
                return;
            }

            Mod.Log($"Adding room to path: {name} (checkpoint: {CheckpointNames.Last()})");

            VisitedRooms.Add(name);
            Checkpoints.Last().Add(name);
        }

        public void AddCheckpoint(Checkpoint cp, string name) {
            if (Checkpoints.Count != 0) {
                if (cp != null && CheckpointsVisited.Contains(cp.Position)) return;
                CheckpointsVisited.Add(cp.Position);

                string lastRoom = Checkpoints.Last().Last();
                Checkpoints.Last().Remove(lastRoom);
                Checkpoints.Add(new List<string>() { lastRoom });
            } else {
                Checkpoints.Add(new List<string>() { });
            }

            if (name == null) {
                Mod.Log($"Added checkpoint to path: CP{Checkpoints.Count}");
                CheckpointNames.Add($"CP{Checkpoints.Count}");
                CheckpointAbbreviations.Add($"CP{Checkpoints.Count}");
            } else {
                Mod.Log($"Added checkpoint to path: {name}");
                CheckpointNames.Add(name);
                CheckpointAbbreviations.Add(AbbreviateName(name));
            }
        }

        public PathInfo ToPathInfo() {
            PathInfo toRet = new PathInfo();

            int checkpointIndex = 0;
            foreach (List<string> checkpoint in Checkpoints) {
                string cpName = CheckpointNames[checkpointIndex];
                string cpAbbreviation = CheckpointAbbreviations[checkpointIndex];

                if (checkpointIndex == 0) {
                    if (Checkpoints.Count == 1) {
                        cpName = "Room";
                        cpAbbreviation = "R";
                    }
                }

                CheckpointInfo cpInfo = new CheckpointInfo() {
                    Name = cpName,
                    Abbreviation = cpAbbreviation,
                };

                foreach (string roomName in checkpoint) {
                    cpInfo.Rooms.Add(new RoomInfo() { DebugRoomName = roomName });
                }

                toRet.Checkpoints.Add(cpInfo);

                checkpointIndex++;
            }

            return toRet;
        }

        public string AbbreviateName(string name, int letterCount=2) {
            string[] words = name.Split(' ');

            if (words.Length == 1) {
                return words[0].Substring(0, letterCount).ToUpper();
            } else {
                string abbr = "";
                foreach (string word in words) {
                    abbr += word[0];
                }

                return abbr.ToUpper();
            }
        }
    }
}
