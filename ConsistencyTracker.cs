using Celeste.Mod.ConsistencyTracker.Models;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Celeste.Mod.ConsistencyTracker {
    public class ConsistencyTrackerModule : EverestModule {
        
        public static ConsistencyTrackerModule Instance;

        public static readonly string ModVersion = "1.1.0";

        public override Type SettingsType => typeof(ConsistencyTrackerSettings);
        public ConsistencyTrackerSettings ModSettings => (ConsistencyTrackerSettings)this._Settings;

        private string CurrentChapterName;
        private string PreviousRoomName;
        private string CurrentRoomName;

        private string DisabledInRoomName;
        private bool DidRestart = false;

        private HashSet<string> ChaptersThisSession = new HashSet<string>();

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
            CheckFolderExists(GetPathToFolder("logs"));
            CheckFolderExists(GetPathToFolder("summaries"));

            LogInit();
            Log($"~~~===============~~~");
            ChapterStats.LogCallback = Log;

            On.Celeste.Level.Begin += Level_Begin;
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
            Log($"[Checkpoint.TurnOn] cp.Position={cp.Position}");
            if (ModSettings.Enabled && DoRecordPath) {
                Path.AddCheckpoint();
            }
        }

        //Not triggered when teleporting via debug map
        private void Level_TeleportTo(On.Celeste.Level.orig_TeleportTo orig, Level level, Player player, string nextLevel, Player.IntroTypes introType, Vector2? nearestSpawn) {
            orig(level, player, nextLevel, introType, nearestSpawn);
            Log($"[Level.TeleportTo] level.Session.LevelData.Name={level.Session.LevelData.Name}");
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            string newCurrentRoom = level.Session.LevelData.Name;
            bool holdingGolden = PlayerIsHoldingGoldenBerry(level.Tracker.GetEntity<Player>());

            Log($"[Level.OnLoadLevel] level.Session.LevelData.Name={newCurrentRoom}, playerIntro={playerIntro} | CurrentRoomName: '{CurrentRoomName}', PreviousRoomName: '{PreviousRoomName}'");
            if (playerIntro == Player.IntroTypes.Respawn) { //Changing room via golden berry death or debug map teleport
                if (CurrentRoomName != null && newCurrentRoom != CurrentRoomName) {
                    SetNewRoom(newCurrentRoom, false, holdingGolden);
                }
            }

            if (DidRestart) {
                Log($"\tRequested reset of PreviousRoomName to null");
                DidRestart = false;
                SetNewRoom(level.Session.LevelData.Name, false, holdingGolden);
                PreviousRoomName = null;
            }
        }

        private bool PlayerIsHoldingGoldenBerry(Player player) {
            return player.Leader.Followers.Any((f) => {
                if (!(f.Entity is Strawberry))
                    return false;

                Strawberry berry = (Strawberry)f.Entity;

                if (!berry.Golden || berry.Winged)
                    return false;

                return true;
            });
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            Log($"[Level.OnExit] mode={mode}, snow={snow}");
            if (mode == LevelExit.Mode.Restart) {
                DidRestart = true;
            } else if (mode == LevelExit.Mode.GoldenBerryRestart) {
                DidRestart = true;

                if (ModSettings.Enabled && !ModSettings.PauseDeathTracking) { //Only count golden berry deaths when enabled
                    CurrentChapterStats?.AddGoldenBerryDeath();
                    if (ModSettings.OnlyTrackWithGoldenBerry) {
                        CurrentChapterStats.AddAttempt(false);
                    }
                }
            }
        }

        private void Level_OnComplete(Level level) {
            Log($"[Level.OnComplete] Incrementing {CurrentChapterStats?.CurrentRoom.DebugRoomName}");
            if(!ModSettings.PauseDeathTracking)
                CurrentChapterStats?.AddAttempt(true);
            SaveChapterStats();
        }

        private void Level_Begin(On.Celeste.Level.orig_Begin orig, Level level) {
            Log($"[Level.Begin] Calling ChangeChapter with 'level.Session'");
            ChangeChapter(level.Session);

            orig(level);
        }

        private void ChangeChapter(Session session) {
            Log($"[ChangeChapter] Level->{session.Level}, session.Area.GetSID()->{session.Area.GetSID()}, session.Area.Mode->{session.Area.Mode}");

            CurrentChapterName = ($"{session.MapData.Data.SID}_{session.Area.Mode}").Replace("/", "_");


            PreviousRoomName = null;
            CurrentRoomName = session.Level;

            CurrentChapterStats = GetCurrentChapterStats();

            SetNewRoom(CurrentRoomName, false, false);

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
            Log($"[Level.OnTransitionTo] levelData.Name->{levelDataNext.Name}, level.Completed->{level.Completed}, level.NewLevel->{level.NewLevel}, level.Session.StartCheckpoint->{level.Session.StartCheckpoint}");
            bool holdingGolden = PlayerIsHoldingGoldenBerry(level.Tracker.GetEntity<Player>());
            SetNewRoom(levelDataNext.Name, true, holdingGolden);
        }

        private void Player_OnDie(Player player) {
            bool holdingGolden = PlayerIsHoldingGoldenBerry(player);

            Log($"[Player.OnDie] Player died. (holdingGolden: {holdingGolden})");
            if (ModSettings.Enabled) {
                if(!ModSettings.PauseDeathTracking && (!ModSettings.OnlyTrackWithGoldenBerry || holdingGolden))
                    CurrentChapterStats?.AddAttempt(false);
                SaveChapterStats();
            }
        }

        public override void Unload() {
        }

        public void SetNewRoom(string newRoomName, bool countDeath=true, bool holdingGolden=false) {
            if (PreviousRoomName == newRoomName) { //Entering previous room
                Log($"[SetNewRoom] Entered previous room '{PreviousRoomName}'");
                PreviousRoomName = CurrentRoomName;
                CurrentRoomName = newRoomName;
                CurrentChapterStats?.SetCurrentRoom(newRoomName);
                SaveChapterStats();
                return;
            }


            Log($"[SetNewRoom] Entered new room '{newRoomName}' | Holding golden: '{holdingGolden}'");

            PreviousRoomName = CurrentRoomName;
            CurrentRoomName = newRoomName;

            if (DoRecordPath) {
                Path.AddRoom(newRoomName);
            }

            if (ModSettings.Enabled && CurrentChapterStats != null) {
                if (countDeath && !ModSettings.PauseDeathTracking && (!ModSettings.OnlyTrackWithGoldenBerry || holdingGolden)) {
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


        public bool PathInfoExists() {
            string path = GetPathToFile($"paths/{CurrentChapterName}.txt");
            return File.Exists(path);
        }
        public PathInfo GetPathInputInfo() {
            Log($"[GetPathInputInfo] Fetching path info for chapter '{CurrentChapterName}'");

            string path = GetPathToFile($"paths/{CurrentChapterName}.txt");
            Log($"\tSearching for path '{path}'");

            if (File.Exists(path)) { //Parse File
                Log($"\tFound file, parsing...");
                string content = File.ReadAllText(path);
                return PathInfo.ParseString(content, Log);

            } else { //Create new
                Log($"\tDidn't find file, created new PathInfo.");
                PathInfo toRet = new PathInfo() {};
                return toRet;
            }
        }

        public ChapterStats GetCurrentChapterStats() {
            string path = GetPathToFile($"stats/{CurrentChapterName}.txt");

            bool hasEnteredThisSession = ChaptersThisSession.Contains(CurrentChapterName);
            ChaptersThisSession.Add(CurrentChapterName);
            Log($"[GetCurrentChapterStats] CurrentChapterName: '{CurrentChapterName}', hasEnteredThisSession: '{hasEnteredThisSession}', ChaptersThisSession: '{string.Join(", ", ChaptersThisSession)}'");

            ChapterStats toRet;

            if (File.Exists(path)) { //Parse File
                string content = File.ReadAllText(path);
                toRet = ChapterStats.ParseString(content);
                toRet.ChapterName = CurrentChapterName;

            } else { //Create new
                toRet = new ChapterStats() {
                    ChapterName = CurrentChapterName,
                };
                toRet.SetCurrentRoom(CurrentRoomName);
            }

            if (!hasEnteredThisSession) {
                toRet.ResetCurrentSession();
                Log("Resetting session for GB deaths");
            } else {
                Log("Not resetting session for GB deaths");
            }

            return toRet;
        }

        public void SaveChapterStats() {
            if (CurrentChapterStats == null) {
                Log($"[SaveChapterStats] Aborting saving chapter stats as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            string path = GetPathToFile($"stats/{CurrentChapterName}.txt");
            File.WriteAllText(path, CurrentChapterStats.ToChapterStatsString());

            string modStatePath = GetPathToFile($"stats/modState.txt");

            string content = $"{CurrentChapterStats.CurrentRoom}\n{CurrentChapterStats.ChapterName};{ModSettings.PauseDeathTracking};{ModSettings.RecordPath};{ModVersion}\n";
            File.WriteAllText(modStatePath, content);
        }

        public void WipeChapterData() {
            if (CurrentChapterStats == null) {
                Log($"[WipeChapterData] Aborting wiping chapter data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"[WipeChapterData] Wiping death data for chapter '{CurrentChapterName}'");

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

        public void WipeChapterGoldenBerryDeaths() {
            if (CurrentChapterStats == null) {
                Log($"[WipeChapterGoldenBerryDeaths] Aborting wiping chapter data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"[WipeChapterGoldenBerryDeaths] Wiping golden berry death data for chapter '{CurrentChapterName}'");

            foreach (string debugName in CurrentChapterStats.Rooms.Keys) {
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeaths = 0;
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeathsThisSession = 0;
            }

            SaveChapterStats();
        }

        public void WipeRoomData() {
            if (CurrentChapterStats == null) {
                Log($"[WipeRoomData] Aborting wiping room data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            Log($"[WipeRoomData] Wiping room data for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.CurrentRoom.PreviousAttempts.Clear();
            SaveChapterStats();
        }

        public void RemoveLastDeathStreak() {
            if (CurrentChapterStats == null) {
                Log($"[RemoveLastDeathStreak] Aborting removing death streak as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            Log($"[RemoveLastDeathStreak] Removing death streak for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            while (CurrentChapterStats.CurrentRoom.LastAttempt == false) {
                CurrentChapterStats.CurrentRoom.RemoveLastAttempt();
            }

            SaveChapterStats();
        }

        public void RemoveLastAttempt() {
            if (CurrentChapterStats == null) {
                Log($"[RemoveLastAttempt] Aborting removing death streak as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            Log($"[RemoveLastAttempt] Removing last attempt for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.CurrentRoom.RemoveLastAttempt();
            SaveChapterStats();
        }

        public void SaveRoomPath() {
            DisabledInRoomName = CurrentRoomName;
            string relativeOutPath = $"paths/{CurrentChapterName}.txt";
            string outPath = GetPathToFile(relativeOutPath);
            File.WriteAllText(outPath, Path.ToString());
            Log($"Wrote path data to '{relativeOutPath}'");
        }


        public void CreateChapterSummary(int attemptCount) {
            Log($"[CreateChapterSummary(attemptCount={attemptCount})] Attempting to create tracker summary");

            bool hasPathInfo = PathInfoExists();

            string relativeOutPath = $"summaries/{CurrentChapterName}.txt";
            string outPath = GetPathToFile(relativeOutPath);

            if (!hasPathInfo) {
                Log($"Called CreateChapterSummary without chapter path info. Please create a path before using this feature");
                File.WriteAllText(outPath, "No path info was found for the current chapter.\nPlease create a path before using the summary feature");
                return;
            }

            PathInfo info = GetPathInputInfo();
            CurrentChapterStats?.OutputSummary(outPath, info, attemptCount);
        }


        private static readonly int LOG_FILE_COUNT = 10;
        public void LogInit() {
            string logFileMax = GetPathToFile($"logs/log_old{LOG_FILE_COUNT}.txt");
            if (File.Exists(logFileMax)) {
                File.Delete(logFileMax);
            }

            for (int i = LOG_FILE_COUNT - 1; i >= 1; i--) {
                string logFilePath = GetPathToFile($"logs/log_old{i}.txt");
                if (File.Exists(logFilePath)) {
                    string logFileNewPath = GetPathToFile($"logs/log_old{i+1}.txt");
                    File.Move(logFilePath, logFileNewPath);
                }
            }

            string lastFile = GetPathToFile("logs/log.txt");
            if (File.Exists(lastFile)) {
                string logFileNewPath = GetPathToFile($"logs/log_old{1}.txt");
                File.Move(lastFile, logFileNewPath);
            }
        }
        public void Log(string log) {
            string path = GetPathToFile("logs/log.txt");
            File.AppendAllText(path, log+"\n");
        }

        public class ConsistencyTrackerSettings : EverestModuleSettings {
            public bool Enabled { get; set; } = false;

            public bool PauseDeathTracking {
                get => _PauseDeathTracking;
                set {
                    _PauseDeathTracking = value;
                    Instance.SaveChapterStats();
                }
            }
            private bool _PauseDeathTracking { get; set; } = false;

            public bool OnlyTrackWithGoldenBerry { get; set; } = false;
            public void CreateOnlyTrackWithGoldenBerryEntry(TextMenu menu, bool inGame) {
                var toggle = new TextMenu.OnOff("Only Track Deaths With Golden Berry", this.OnlyTrackWithGoldenBerry);
                toggle.OnValueChange = v => {
                    this.OnlyTrackWithGoldenBerry = v;
                };
                menu.Add(toggle);
            }

            public bool RecordPath { get; set; } = false;
            public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                var toggle = new TextMenu.OnOff("Record Path", this.RecordPath);
                toggle.OnValueChange = v => {
                    if (v)
                        Instance.Log($"Recording chapter path...");
                    else
                        Instance.Log($"Stopped recording path. Outputting info...");

                    this.RecordPath = v;
                    Instance.DoRecordPath = v;
                    Instance.SaveChapterStats();
                };

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Path Recording", false);
                subMenu.Add(new TextMenu.SubHeader("!!!Existing paths will be overwritten!!!"));
                subMenu.Add(toggle);

                menu.Add(subMenu);
            }


            public bool WipeChapter { get; set; } = false;
            public void CreateWipeChapterEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("!!Data Wipe!!", false);
                subMenu.Add(new TextMenu.SubHeader("These actions cannot be reverted!"));


                subMenu.Add(new TextMenu.SubHeader("Manage Last Attempts Of Current Room"));
                var buttonLastAttempt = new TextMenu.Button("Remove Last Attempt");
                buttonLastAttempt.OnPressed = () => {
                    Instance.RemoveLastAttempt();
                };
                subMenu.Add(buttonLastAttempt);

                var button0 = new TextMenu.Button("Remove Last Death Streak");
                button0.OnPressed = () => {
                    Instance.RemoveLastDeathStreak();
                };
                subMenu.Add(button0);


                subMenu.Add(new TextMenu.SubHeader("Wipe All Data"));
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

                var button3 = new TextMenu.Button("Wipe Chapter Golden Berry Deaths");
                button3.OnPressed = () => {
                    Instance.WipeChapterGoldenBerryDeaths();
                };
                subMenu.Add(button3);

                menu.Add(subMenu);
            }


            private int SelectedAttemptCount { get; set; } = 20;
            public bool CreateSummary { get; set; } = false;
            public void CreateCreateSummaryEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Tracker Summary", false);
                subMenu.Add(new TextMenu.SubHeader("Outputs some cool data of the current chapter in a readable .txt format"));


                List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
                TextMenuExt.EnumerableSlider<int> attemptSlider = new TextMenuExt.EnumerableSlider<int>("Summary Over X Attempts", AttemptCounts, 20);
                attemptSlider.OnValueChange = (value) => {
                    SelectedAttemptCount = value;
                };
                subMenu.Add(attemptSlider);

                subMenu.Add(new TextMenu.SubHeader("When calculating the consistency stats, only the last X attempts will be counted"));

                var button1 = new TextMenu.Button("Create Chapter Summary");
                button1.OnPressed = () => {
                    Instance.CreateChapterSummary(SelectedAttemptCount);
                };
                subMenu.Add(button1);

                menu.Add(subMenu);
            }
        }
    }
}
