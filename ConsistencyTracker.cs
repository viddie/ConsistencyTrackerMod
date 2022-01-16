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
        private bool DidRestart = false;

        public bool DoRecordPath {
            get => _DoRecordPath;
            set {
                if (value) {
                    if (DisabledInRoomName != CurrentRoomName) {
                        Path = new PathRecorder();
                        Path.AddRoom(CurrentRoomName);
                    }
                } else {
                    SaveRoomPath();
                } 

                _DoRecordPath = value;
            }
        }
        private bool _DoRecordPath = false;
        private PathRecorder Path;

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
            Everest.Events.Level.OnExit += Level_OnExit;
            Everest.Events.Level.OnComplete += Level_OnComplete;
            Everest.Events.Level.OnTransitionTo += Level_OnTransitionTo;
            Everest.Events.Player.OnDie += Player_OnDie;
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            On.Celeste.Level.TeleportTo += Level_TeleportTo;
            On.Celeste.Checkpoint.TurnOn += Checkpoint_TurnOn;


            //On.Celeste.Strawberry.Added += Strawberry_Added;
        }

        //private void Strawberry_Added(On.Celeste.Strawberry.orig_Added orig, Strawberry berry, Monocle.Scene scene) { //Triggered when dying with the golden berry
        //    orig(berry, scene);
        //    TestLog($"Strawberry.Added -> berry.Golden={berry.Golden}, berry.ReturnHomeWhenLost={berry.ReturnHomeWhenLost}");
        //}

        private void Checkpoint_TurnOn(On.Celeste.Checkpoint.orig_TurnOn orig, Checkpoint cp, bool animate) {
            orig(cp, animate);
            TestLog($"Checkpoint.TurnOn -> cp.Position={cp.Position}");
            if (ModSettings.Enabled && DoRecordPath) {
                Path.AddCheckpoint();
            }
        }

        //Not triggered when teleporting via debug map
        private void Level_TeleportTo(On.Celeste.Level.orig_TeleportTo orig, Level level, Player player, string nextLevel, Player.IntroTypes introType, Vector2? nearestSpawn) {
            orig(level, player, nextLevel, introType, nearestSpawn);
            TestLog($"Level.TeleportTo -> level.Session.LevelData.Name={level.Session.LevelData.Name}");
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            TestLog($"Level.OnLoadLevel -> level.Session.LevelData.Name={level.Session.LevelData.Name}, playerIntro={playerIntro}");
            if (playerIntro == Player.IntroTypes.Respawn) { //Changing room via golden berry death or debug map teleport
                if (PreviousRoomName != null && PreviousRoomName != CurrentRoomName) {
                    SetNewRoom(level.Session.LevelData.Name, false);
                }
            }

            if (DidRestart) {
                DidRestart = false;
                SetNewRoom(level.Session.LevelData.Name, false);
                PreviousRoomName = null;
            }
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            TestLog($"Level.OnExit -> mode={mode}, snow={snow}");
            if (mode == LevelExit.Mode.Restart) {
                DidRestart = true;
            }
        }

        private void Level_OnComplete(Level level) {
            TestLog($"Level.OnComplete -> Incrementing {CurrentChapterStats.CurrentRoom.DebugRoomName}");
            if(!ModSettings.PauseDeathTracking)
                CurrentChapterStats.AddAttempt(true);
            SaveChapterStats();
        }

        private void Level_OnEnter(Session session, bool fromSaveData) {
            TestLog($"Level.OnEnter: Level->{session.Level}, session.Area.GetSID()->{session.Area.GetSID()}, session.Area.Mode->{session.Area.Mode}");

            CurrentChapterName = ($"{session.MapData.Data.SID}_{session.Area.Mode}").Replace("/", "_");
            

            PreviousRoomName = null;
            CurrentRoomName = session.Level;

            CurrentChapterStats = GetCurrentChapterStats();
            string read = CurrentChapterStats == null ? "null" : CurrentChapterStats.ToCurrentRoomString();
            //TestLog($"Read stats from file:\n{read}\n");

            SetNewRoom(CurrentRoomName, false);

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
            TestLog($"Level.OnTransitionTo: levelData.Name->{levelDataNext.Name}, level.Completed->{level.Completed}, level.NewLevel->{level.NewLevel}, level.Session.StartCheckpoint->{level.Session.StartCheckpoint}");
            SetNewRoom(levelDataNext.Name);
        }

        private void Player_OnDie(Player player) {
            TestLog("Player died.");
            if (ModSettings.Enabled) {
                if(!ModSettings.PauseDeathTracking)
                    CurrentChapterStats.AddAttempt(false);
                SaveChapterStats();
            }
        }

        public override void Unload() {
        }

        public void SetNewRoom(string newRoomName, bool countDeath=true) {
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
                Path.AddRoom(newRoomName);
            }

            if (ModSettings.Enabled && CurrentChapterStats != null) {
                if (countDeath && !ModSettings.PauseDeathTracking) {
                    CurrentChapterStats.AddAttempt(true);
                }
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
                ChapterStats toRet = ChapterStats.ParseString(content);
                toRet.ChapterName = CurrentChapterName;
                return toRet;

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
            string relativeOutPath = $"paths/{CurrentChapterName}.txt";
            string outPath = GetPathToFile(relativeOutPath);
            File.WriteAllText(outPath, Path.ToString());
            TestLog($"Wrote path data to '{relativeOutPath}'");
        }

        public void TestLog(string log) {
            string path = GetPathToFile("log.txt");
            File.AppendAllText(path, log+"\n");
        }

        public class ConsistencyTrackerSettings : EverestModuleSettings {
            public bool Enabled { get; set; } = false;
            public bool PauseDeathTracking { get; set; } = false;
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
