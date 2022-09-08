using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.ConsistencyTracker.Models {
    public class PathRecorder {

        //Remember all previously visited rooms. Rooms only get added to the first checkpoint they appear in.
        public HashSet<string> VisitedRooms { get; set; } = new HashSet<string>();

        public List<List<string>> Checkpoints { get; set; } = new List<List<string>>() {
            new List<string>(),
        };
        public HashSet<Vector2> CheckpointsVisited { get; set; } = new HashSet<Vector2>();

        public void AddRoom(string name) {
            if (VisitedRooms.Contains(name)) return;

            VisitedRooms.Add(name);
            Checkpoints.Last().Add(name);
        }

        public void AddCheckpoint(Checkpoint cp) {
            if (CheckpointsVisited.Contains(cp.Position)) return;
            CheckpointsVisited.Add(cp.Position);
            

            string lastRoom = Checkpoints.Last().Last();
            Checkpoints.Last().Remove(lastRoom);

            Checkpoints.Add(new List<string>() { lastRoom });
        }

        public PathInfo ToPathInfo() {
            PathInfo toRet = new PathInfo();

            int checkpointIndex = 0;
            foreach (List<string> checkpoint in Checkpoints) {
                string cpName = $"CP{checkpointIndex + 1}";
                string cpAbbreviation = $"CP{checkpointIndex + 1}";

                if (checkpointIndex == 0) {
                    if (Checkpoints.Count == 1) {
                        cpName = "Room";
                        cpAbbreviation = "R";
                    } else {
                        cpName = "Start";
                        cpAbbreviation = "ST";
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

        //public override string ToString() {
        //    List<string> lines = new List<string>();

        //    int checkpointIndex = 0;
        //    foreach (List<string> checkpoint in Checkpoints) {
        //        if (checkpointIndex == 0) {
        //            lines.Add($"Start;ST;{checkpoint.Count};" + string.Join(",", checkpoint));
        //        } else {
        //            lines.Add($"CP{checkpointIndex + 1};CP{checkpointIndex + 1};{checkpoint.Count};" + string.Join(",", checkpoint));
        //        }
        //        checkpointIndex++;
        //    }

        //    return string.Join("\n", lines)+"\n";
        //}
    }
}
