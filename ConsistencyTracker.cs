using Celeste.Mod.ConsistencyTracker.Models;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Celeste.Mod.ConsistencyTracker {
    public class ConsistencyTrackerModule : EverestModule {
        
        public static ConsistencyTrackerModule Instance;

        public override Type SettingsType => typeof(ConsistencyTrackerSettings);
        public ConsistencyTrackerSettings ModSettings => (ConsistencyTrackerSettings)this._Settings;

        private string CurrentChapterName;
        private string PreviousRoomName;
        private string CurrentRoomName;

        private string DisabledInRoomName;

        public bool DoRecordPath {
            get => _DoRecordPath;
            set {
                if (value) {
                    if (DisabledInRoomName != CurrentRoomName) {
                        RoomPath.Clear();
                        RoomPath.Add(CurrentRoomName);
                        RoomPathDuplicates.Clear();
                        RoomPathDuplicates.Add(CurrentRoomName);
                    }
                } else {
                    SaveRoomPath();
                } 

                _DoRecordPath = value;
            }
        }
        private bool _DoRecordPath = false;
        List<string> RoomPath = new List<string>();
        List<string> RoomPathDuplicates = new List<string>();

        private ChapterStats CurrentChapterStats;


        public ConsistencyTrackerModule() {
            Instance = this;
        }

        public override void Load() {
            CheckFolderExists(baseFolderPath);
            CheckFolderExists(GetPathToFolder("paths"));
            CheckFolderExists(GetPathToFolder("stats"));

            TestLog($"~~~===============~~~");
            ChapterStats.LogCallback = TestLog;

            Everest.Events.Level.OnEnter += Level_OnEnter;
            Everest.Events.Level.OnTransitionTo += Level_OnTransitionTo;
            Everest.Events.Player.OnDie += Player_OnDie;
        }

        private void Level_OnEnter(Session session, bool fromSaveData) {
            TestLog($"Level.OnEnter: Level->{session.Level} [fromSaveData({fromSaveData})]");

            CurrentChapterName = session.MapData.Data.Name.Replace("/", "_");

            PreviousRoomName = CurrentRoomName;
            CurrentRoomName = session.Level;

            CurrentChapterStats = GetCurrentChapterStats();
            string read = CurrentChapterStats == null ? "null" : CurrentChapterStats.ToCurrentRoomString();
            TestLog($"Read stats from file:\n{read}\n\n");

            SetNewRoom(CurrentRoomName);

            if (!DoRecordPath && ModSettings.RecordPath) {
                DoRecordPath = true;
            }
        }

        private string FormatMapData(MapData data) {
            if (data == null) return "No MapData attached...";

            string metaPath = "";
            if (metaPath != null) {
                metaPath = data.Meta.Path ?? "null";
            }

            string filename = data.Filename ?? "null";
            string filepath = data.Filepath ?? "null";

            string areaChapterIndex = "";
            string areaMode = "";
            if (data.Area != null) {
                areaChapterIndex = data.Area.ChapterIndex.ToString();
                areaMode = data.Area.Mode.ToString();
            }

            string dataName = data.Data.Name ?? "null";
            string dataScreenName = data.Data.CompleteScreenName ?? "null";
            string dataSID = data.Data.SID ?? "null";

            return $"Data.SID->{dataSID}, Data.CompleteScreenName->{dataScreenName}, Data.Name->{dataName}, Meta.Path->{metaPath}, Filename->{filename}, Filepath->{filepath}, Area.ChapterIndex->{areaChapterIndex}, Area.Mode->{areaMode}";
        }

        private void Level_OnTransitionTo(Level level, LevelData levelDataNext, Vector2 direction) {
            TestLog($"Level.OnTransitionTo: LevelData.Name->{levelDataNext.Name}, Level.Completed->{level.Completed}, Level.NewLevel->{level.NewLevel} [direction({direction})]");
            SetNewRoom(levelDataNext.Name);
        }

        private void Player_OnDie(Player player) {
            TestLog("Player died.");
            if (ModSettings.Enabled) {
                CurrentChapterStats.AddAttempt(false);
                TestLog($"Current Room Stats: {CurrentChapterStats.CurrentRoom}");
                SaveChapterStats();
            }
        }

        public override void Unload() {
        }

        public void SetNewRoom(string newRoomName) {
            if (PreviousRoomName == newRoomName) { //Entering previous room
                PreviousRoomName = CurrentRoomName;
                CurrentRoomName = newRoomName;
                CurrentChapterStats.SetCurrentRoom(newRoomName);
                SaveChapterStats();
                return;
            }

            PreviousRoomName = CurrentRoomName;
            CurrentRoomName = newRoomName;

            if (DoRecordPath) {
                if (!RoomPath.Contains(newRoomName)) {
                    RoomPath.Add(newRoomName);
                }
                RoomPathDuplicates.Add(newRoomName);
            }

            if (ModSettings.Enabled && CurrentChapterStats != null) {
                CurrentChapterStats.AddAttempt(true);
                CurrentChapterStats.SetCurrentRoom(newRoomName);
                SaveChapterStats();
            }
        }

        string baseFolderPath = "./ConsistencyTracker/";
        public string GetPathToFile(string file) {
            return baseFolderPath + file;
        }
        public string GetPathToFolder(string folder) {
            return baseFolderPath + folder + "/";
        }
        public void CheckFolderExists(string folderPath) {
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
            }
        }

        public PathInfo GetPathInputInfo() {
            string path = GetPathToFile($"paths/{CurrentChapterName}.txt");

            if (File.Exists(path)) { //Parse File
                string content = File.ReadAllText(path);
                return PathInfo.ParseString(content);

            } else { //Create new
                PathInfo toRet = new PathInfo() {};
                return toRet;
            }
        }

        public ChapterStats GetCurrentChapterStats() {
            string path = GetPathToFile($"stats/{CurrentChapterName}.txt");

            if (File.Exists(path)) { //Parse File
                string content = File.ReadAllText(path);
                return ChapterStats.ParseString(content);

            } else { //Create new
                ChapterStats toRet = new ChapterStats() {
                    ChapterName = CurrentChapterName,
                };
                toRet.SetCurrentRoom(CurrentRoomName);
                return toRet;
            }
        }

        public void SaveChapterStats() {
            string path = GetPathToFile($"stats/{CurrentChapterName}.txt");
            File.WriteAllText(path, CurrentChapterStats.ToChapterStatsString());

            string currentRoom = GetPathToFile($"stats/current_room.txt");
            File.WriteAllText(currentRoom, CurrentChapterStats.ToCurrentRoomString());
        }

        public void WipeChapterData() {
            RoomStats currentRoom = CurrentChapterStats.CurrentRoom;
            List<string> toRemove = new List<string>();

            foreach (string debugName in CurrentChapterStats.Rooms.Keys) {
                if (debugName == currentRoom.DebugRoomName) continue;
                toRemove.Add(debugName);
            }

            foreach (string debugName in toRemove) {
                CurrentChapterStats.Rooms.Remove(debugName);
            }

            WipeRoomData();
        }

        public void WipeRoomData() {
            CurrentChapterStats.CurrentRoom.PreviousAttempts.Clear();
            SaveChapterStats();
        }

        public void SaveRoomPath() {
            DisabledInRoomName = CurrentRoomName;
            PathInfo roomPathInfo = GetPathInputInfo();
            string relativeOutPath = $"paths/{CurrentChapterName}.txt";
            string outPath = GetPathToFile(relativeOutPath);

            if (roomPathInfo.Checkpoints.Count == 0) {
                File.WriteAllLines(outPath, RoomPathDuplicates);
                TestLog($"Wrote simplified path data to '{relativeOutPath}'");

            } else {
                int roomPathIndex = 0;
                try {
                    foreach (CheckpointInfo cpInfo in roomPathInfo.Checkpoints) {
                        for (int cpRoomIndex = 0; cpRoomIndex < cpInfo.RoomCount; cpRoomIndex++) {
                            cpInfo.Rooms.Add(new RoomInfo() { DebugRoomName = RoomPath[roomPathIndex] });
                            roomPathIndex++;
                        }
                    }
                } catch (Exception ex) {
                    TestLog($"Mismatch between path input/output: {ex.ToString()}");
                    roomPathInfo.ParseError = ex.ToString();
                }
                File.WriteAllText(outPath, roomPathInfo.ToString());
                TestLog($"Wrote path data to '{relativeOutPath}'");
            }
        }

        public void TestLog(string log) {
            string path = GetPathToFile("log.txt");
            File.AppendAllText(path, log+"\n");
        }

        public class ConsistencyTrackerSettings : EverestModuleSettings {
            public bool Enabled { get; set; } = false;
            public bool RecordPath { get; set; } = false;
            public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                var toggle = new TextMenu.OnOff("Record Path", this.RecordPath);
                toggle.OnValueChange = v => {
                    if (v)
                        Instance.TestLog($"Recording chapter path...");
                    else
                        Instance.TestLog($"Stopped recording path. Outputting info...");

                    this.RecordPath = v;
                    Instance.DoRecordPath = v;
                };
                menu.Add(toggle);
            }


            public bool WipeChapter { get; set; } = false;
            public void CreateWipeChapterEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("!!Data Wipe!!", false);
                subMenu.Add(new TextMenu.SubHeader("These actions cannot be reverted"));

                var button1 = new TextMenu.Button("Wipe Room Data");
                button1.OnPressed = () => {
                    Instance.WipeRoomData();
                };
                subMenu.Add(button1);

                var button2 = new TextMenu.Button("Wipe Chapter Data");
                button2.OnPressed = () => {
                    Instance.WipeChapterData();
                };
                subMenu.Add(button2);

                menu.Add(subMenu);
            }
        }
    }
}
