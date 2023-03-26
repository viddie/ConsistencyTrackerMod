using Microsoft.Xna.Framework;
using System;
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
        public List<string> ObsoleteNames = new List<string>();

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

            string nameToAdd = null;
            string abbrToAdd = null;

            if (name == null) {
                nameToAdd = $"CP{Checkpoints.Count}";
                abbrToAdd = $"CP{Checkpoints.Count}";

            } else if (name != null && CheckpointNames.Contains(name)) {
                Mod.Log($"Checkpoint name '{name}' already exists on path! Using default checkpoint naming scheme...");
                nameToAdd = $"CP{Checkpoints.Count}";
                abbrToAdd = $"CP{Checkpoints.Count}";
                
            } else {
                bool foundName = false;
                int letterCount = 2;
                int runsThrough = 0;
                while (!foundName) {
                    nameToAdd = name;
                    abbrToAdd = AbbreviateName(name, letterCount);

                    if (CheckpointAbbreviations.Contains(abbrToAdd)) {
                        //Rename the other checkpoint and try again.
                        ObsoleteNames.Add(abbrToAdd);

                        int index = CheckpointAbbreviations.IndexOf(abbrToAdd);
                        string newAbbr = AbbreviateName(CheckpointNames[index], letterCount + 1);
                        CheckpointAbbreviations[index] = newAbbr;
                    } else if (ObsoleteNames.Contains(abbrToAdd)) {
                        letterCount++;
                    } else {
                        foundName = true;
                    }

                    //If search continued for too long, just stop and let the user fix the issue.
                    if (letterCount == 6) {
                        foundName = true;
                    }

                    runsThrough++;
                    if (runsThrough > 10) {
                        Mod.Log($"Could not find a unique name for checkpoint: {name}");
                        nameToAdd = $"CP{Checkpoints.Count}";
                        abbrToAdd = $"CP{Checkpoints.Count}";
                        foundName = true;
                    }
                }
            }

            Mod.Log($"Added checkpoint to path: {nameToAdd} ({abbrToAdd})");
            CheckpointNames.Add(nameToAdd);
            CheckpointAbbreviations.Add(abbrToAdd);
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
                return words[0].Substring(0, Math.Min(letterCount, words[0].Length)).ToUpper();
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
