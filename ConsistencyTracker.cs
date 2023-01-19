using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.ConsistencyTracker.ThirdParty;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.EverestInterop;
using Celeste.Mod.ConsistencyTracker.Properties;
using System.Drawing;

namespace Celeste.Mod.ConsistencyTracker {
    public class ConsistencyTrackerModule : EverestModule {

        public static ConsistencyTrackerModule Instance;
        private static readonly int LOG_FILE_COUNT = 10;

        public static readonly string OverlayVersion = "1.1.1";
        public static readonly string ModVersion = "1.4.0";

        public override Type SettingsType => typeof(ConsistencyTrackerSettings);
        public ConsistencyTrackerSettings ModSettings => (ConsistencyTrackerSettings)this._Settings;
        static string BaseFolderPath = "./ConsistencyTracker/";


        private bool DidRestart = false;
        private HashSet<string> ChaptersThisSession = new HashSet<string>();

        #region Path Recording Variables

        public bool DoRecordPath {
            get => _DoRecordPath;
            set {
                if (value) {
                    if (DisabledInRoomName != CurrentRoomName) {
                        Path = new PathRecorder();
                        InsertCheckpointIntoPath(null, LastRoomWithCheckpoint);
                        Path.AddRoom(CurrentRoomName);
                    }
                } else {
                    SaveRecordedRoomPath();
                }

                _DoRecordPath = value;
            }
        }
        private bool _DoRecordPath = false;
        private PathRecorder Path;
        private string DisabledInRoomName;

        #endregion

        #region State Variables

        //Used to cache and prevent unnecessary operations via DebugRC
        public long CurrentUpdateFrame;

        public PathInfo CurrentChapterPath;
        public ChapterStats CurrentChapterStats;

        public string CurrentChapterDebugName;
        public string PreviousRoomName;
        public string CurrentRoomName;

        private string LastRoomWithCheckpoint = null;

        private bool _CurrentRoomCompleted = false;
        private bool _CurrentRoomCompletedResetOnDeath = false;
        private bool _PlayerIsHoldingGolden = false;

        #endregion

        public StatManager StatsManager;


        public ConsistencyTrackerModule() {
            Instance = this;
        }

        #region Load/Unload Stuff

        public override void Load() {
            CheckFolderExists(BaseFolderPath);
            bool pathsFolderExisted = CheckFolderExists(GetPathToFolder("paths"));
            if (!pathsFolderExisted) {
                CreatePrepackagedPaths();
            }
            CheckFolderExists(GetPathToFolder("stats"));
            CheckFolderExists(GetPathToFolder("logs"));
            CheckFolderExists(GetPathToFolder("summaries"));

            bool overlayFolderExisted = CheckFolderExists(GetPathToFolder("external-overlay"));
            if (!overlayFolderExisted) {
                CreateExternalOverlay();
            }


            LogInit();
            Log($"~~~===============~~~");
            ChapterStats.LogCallback = Log;

            HookStuff();

            StatsManager = new StatManager();

            DebugRcPage.Load();
        }

        public override void Unload() {
            UnHookStuff();
            DebugRcPage.Unload();
        }

        private void HookStuff() {
            //Track where the player is
            On.Celeste.Level.Begin += Level_Begin;
            Everest.Events.Level.OnExit += Level_OnExit;
            Everest.Events.Level.OnComplete += Level_OnComplete;
            Everest.Events.Level.OnTransitionTo += Level_OnTransitionTo;
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            On.Celeste.Level.TeleportTo += Level_TeleportTo;
            //Track deaths
            Everest.Events.Player.OnDie += Player_OnDie;
            //Track checkpoints
            On.Celeste.Checkpoint.TurnOn += Checkpoint_TurnOn;

            //Track in-room events, to determine when exiting back into a previous room counts as success
            //E.g. Power Source rooms where you collect a key but exit back into the HUB room should be marked as success

            //Picking up a kye
            On.Celeste.Key.OnPlayer += Key_OnPlayer; //works

            //Activating Resort clutter switches
            On.Celeste.ClutterSwitch.OnDashed += ClutterSwitch_OnDashed; //works

            //Picking up a strawberry
            On.Celeste.Strawberry.OnCollect += Strawberry_OnCollect; //doesnt work :(
            On.Celeste.Strawberry.OnPlayer += Strawberry_OnPlayer; //sorta works, but triggers very often for a single berry

            //Changing lava/ice in Core
            On.Celeste.CoreModeToggle.OnChangeMode += CoreModeToggle_OnChangeMode; //works

            //Picking up a Cassette tape
            On.Celeste.Cassette.OnPlayer += Cassette_OnPlayer; //works

            //Open up key doors?
            //On.Celeste.Door.Open += Door_Open; //Wrong door (those are the resort doors)
            On.Celeste.LockBlock.TryOpen += LockBlock_TryOpen; //works
        }

        private void UnHookStuff() {
            On.Celeste.Level.Begin -= Level_Begin;
            Everest.Events.Level.OnExit -= Level_OnExit;
            Everest.Events.Level.OnComplete -= Level_OnComplete;
            Everest.Events.Level.OnTransitionTo -= Level_OnTransitionTo;
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
            On.Celeste.Level.TeleportTo -= Level_TeleportTo;

            //Track deaths
            Everest.Events.Player.OnDie -= Player_OnDie;

            //Track checkpoints
            On.Celeste.Checkpoint.TurnOn -= Checkpoint_TurnOn;

            //Picking up a kye
            On.Celeste.Key.OnPlayer -= Key_OnPlayer;

            //Activating Resort clutter switches
            On.Celeste.ClutterSwitch.OnDashed -= ClutterSwitch_OnDashed;

            //Picking up a strawberry
            On.Celeste.Strawberry.OnPlayer -= Strawberry_OnPlayer;

            //Changing lava/ice in Core
            On.Celeste.CoreModeToggle.OnChangeMode -= CoreModeToggle_OnChangeMode;

            //Picking up a Cassette tape
            On.Celeste.Cassette.OnPlayer -= Cassette_OnPlayer;

            //Open up key doors
            On.Celeste.LockBlock.TryOpen -= LockBlock_TryOpen;
        }

        public override void Initialize()
        {
            base.Initialize();

            // load SpeedrunTool if it exists
            if (Everest.Modules.Any(m => m.Metadata.Name == "SpeedrunTool")) {
                SpeedrunToolSupport.Load();
            }
        }

        #endregion

        #region Hooks

        private void LockBlock_TryOpen(On.Celeste.LockBlock.orig_TryOpen orig, LockBlock self, Player player, Follower fol) {
            orig(self, player, fol);
            Log($"[LockBlock.TryOpen] Opened a door");
            SetRoomCompleted(resetOnDeath: false);
        }

        private DashCollisionResults ClutterSwitch_OnDashed(On.Celeste.ClutterSwitch.orig_OnDashed orig, ClutterSwitch self, Player player, Vector2 direction) {
            Log($"[ClutterSwitch.OnDashed] Activated a clutter switch");
            SetRoomCompleted(resetOnDeath: false);
            return orig(self, player, direction);
        }

        private void Key_OnPlayer(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
            Log($"[Key.OnPlayer] Picked up a key");
            orig(self, player);
            SetRoomCompleted(resetOnDeath: false);
        }

        private void Cassette_OnPlayer(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
            Log($"[Cassette.OnPlayer] Collected a cassette tape");
            orig(self, player);
            SetRoomCompleted(resetOnDeath: false);
        }

        private readonly List<EntityID> TouchedBerries = new List<EntityID>();
        // All touched berries need to be reset on death, since they either:
        // - already collected
        // - disappeared on death
        private void Strawberry_OnPlayer(On.Celeste.Strawberry.orig_OnPlayer orig, Strawberry self, Player player) {
            orig(self, player);

            if (TouchedBerries.Contains(self.ID)) return; //to not spam the log
            TouchedBerries.Add(self.ID);

            Log($"[Strawberry.OnPlayer] Strawberry on player");
            SetRoomCompleted(resetOnDeath: true);
        }

        private void Strawberry_OnCollect(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
            Log($"[Strawberry.OnCollect] Collected a strawberry");
            orig(self);
            SetRoomCompleted(resetOnDeath: false);
        }

        private void CoreModeToggle_OnChangeMode(On.Celeste.CoreModeToggle.orig_OnChangeMode orig, CoreModeToggle self, Session.CoreModes mode) {
            Log($"[CoreModeToggle.OnChangeMode] Changed core mode to '{mode}'");
            orig(self, mode);
            SetRoomCompleted(resetOnDeath:true);
        }

        private void Checkpoint_TurnOn(On.Celeste.Checkpoint.orig_TurnOn orig, Checkpoint cp, bool animate) {
            orig(cp, animate);
            Log($"[Checkpoint.TurnOn] cp.Position={cp.Position}, LastRoomWithCheckpoint={LastRoomWithCheckpoint}");
            if (ModSettings.Enabled && DoRecordPath) {
                InsertCheckpointIntoPath(cp, LastRoomWithCheckpoint);
            }
        }

        //Not triggered when teleporting via debug map
        private void Level_TeleportTo(On.Celeste.Level.orig_TeleportTo orig, Level level, Player player, string nextLevel, Player.IntroTypes introType, Vector2? nearestSpawn) {
            orig(level, player, nextLevel, introType, nearestSpawn);
            Log($"[Level.TeleportTo] level.Session.LevelData.Name={SanitizeRoomName(level.Session.LevelData.Name)}");
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            string newCurrentRoom = SanitizeRoomName(level.Session.LevelData.Name);
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
                SetNewRoom(newCurrentRoom, false, holdingGolden);
                PreviousRoomName = null;
            }
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

            if (DoRecordPath) {
                DoRecordPath = false;
                ModSettings.RecordPath = false;
            }
        }

        private void Level_OnComplete(Level level) {
            Log($"[Level.OnComplete] Incrementing {CurrentChapterStats?.CurrentRoom.DebugRoomName}");
            if(ModSettings.Enabled && !ModSettings.PauseDeathTracking && (!ModSettings.OnlyTrackWithGoldenBerry || _PlayerIsHoldingGolden))
                CurrentChapterStats?.AddAttempt(true);
            CurrentChapterStats.ModState.ChapterCompleted = true;
            SaveChapterStats();
        }

        private void Level_Begin(On.Celeste.Level.orig_Begin orig, Level level) {
            Log($"[Level.Begin] Calling ChangeChapter with 'level.Session'");
            ChangeChapter(level.Session);
            orig(level);
        }

        private void Level_OnTransitionTo(Level level, LevelData levelDataNext, Vector2 direction) {
            if (levelDataNext.HasCheckpoint) {
                LastRoomWithCheckpoint = levelDataNext.Name;
            }

            string roomName = SanitizeRoomName(levelDataNext.Name);
            Log($"[Level.OnTransitionTo] levelData.Name->{roomName}, level.Completed->{level.Completed}, level.NewLevel->{level.NewLevel}, level.Session.StartCheckpoint->{level.Session.StartCheckpoint}");
            bool holdingGolden = PlayerIsHoldingGoldenBerry(level.Tracker.GetEntity<Player>());
            SetNewRoom(roomName, true, holdingGolden);
        }

        private void Player_OnDie(Player player) {
            TouchedBerries.Clear();
            bool holdingGolden = PlayerIsHoldingGoldenBerry(player);

            Log($"[Player.OnDie] Player died. (holdingGolden: {holdingGolden})");
            if (_CurrentRoomCompletedResetOnDeath) {
                _CurrentRoomCompleted = false;
            }

            if (ModSettings.Enabled) {
                if (!ModSettings.PauseDeathTracking && (!ModSettings.OnlyTrackWithGoldenBerry || holdingGolden))
                    CurrentChapterStats?.AddAttempt(false);

                if(CurrentChapterStats != null)
                    CurrentChapterStats.CurrentRoom.DeathsInCurrentRun++;

                SaveChapterStats();
            }
        }

        #endregion

        #region State Management

        private string SanitizeRoomName(string name) {
            name = name.Replace(";", "");
            return name;
        }

        private void ChangeChapter(Session session) {
            Log($"[ChangeChapter] Called chapter change");
            AreaData area = AreaData.Areas[session.Area.ID];
            string chapName = area.Name;
            string chapNameClean = chapName.DialogCleanOrNull() ?? chapName.SpacedPascalCase();
            string campaignName = DialogExt.CleanLevelSet(area.GetLevelSet());

            Log($"[ChangeChapter] Level->{session.Level}, session.Area.GetSID()->{session.Area.GetSID()}, session.Area.Mode->{session.Area.Mode}, chapterNameClean->{chapNameClean}, campaignName->{campaignName}");

            CurrentChapterDebugName = ($"{session.MapData.Data.SID}_{session.Area.Mode}").Replace("/", "_");

            //string test2 = Dialog.Get($"luma_farewellbb_FarewellBB_b_intro");
            //Log($"[ChangeChapter] Dialog Test 2: {test2}");

            PreviousRoomName = null;
            CurrentRoomName = session.Level;

            CurrentChapterPath = GetPathInputInfo();
            CurrentChapterStats = GetCurrentChapterStats();

            CurrentChapterStats.ChapterSID = session.MapData.Data.SID;
            CurrentChapterStats.ChapterSIDDialogSanitized = SanitizeSIDForDialog(session.MapData.Data.SID);
            CurrentChapterStats.ChapterName = chapNameClean;
            CurrentChapterStats.CampaignName = campaignName;

            TouchedBerries.Clear();

            SetNewRoom(CurrentRoomName, false, false);
            if (session.LevelData.HasCheckpoint) {
                LastRoomWithCheckpoint = CurrentRoomName;
            } else {
                LastRoomWithCheckpoint = null;
            }

            if (!DoRecordPath && ModSettings.RecordPath) {
                DoRecordPath = true;
            }
        }

        public void SetNewRoom(string newRoomName, bool countDeath=true, bool holdingGolden=false) {
            _PlayerIsHoldingGolden = holdingGolden;
            CurrentChapterStats.ModState.ChapterCompleted = false;

            if (PreviousRoomName == newRoomName && !_CurrentRoomCompleted) { //Don't complete if entering previous room and current room was not completed
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
            _CurrentRoomCompleted = false;

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

        private void SetRoomCompleted(bool resetOnDeath=false) {
            _CurrentRoomCompleted = true;
            _CurrentRoomCompletedResetOnDeath = resetOnDeath;
        }

        private bool PlayerIsHoldingGoldenBerry(Player player) {
            if (player == null || player.Leader == null || player.Leader.Followers == null)
                return false;

            return player.Leader.Followers.Any((f) => {
                if (!(f.Entity is Strawberry))
                    return false;

                Strawberry berry = (Strawberry)f.Entity;

                if (!berry.Golden || berry.Winged)
                    return false;

                return true;
            });
        }

        #region Speedrun Tool Save States

        public void SpeedrunToolSaveState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Type type = GetType();
            if (!savedvalues.ContainsKey(type)) {
                savedvalues.Add(type, new Dictionary<string, object>());
                savedvalues[type].Add(nameof(PreviousRoomName), PreviousRoomName);
                savedvalues[type].Add(nameof(CurrentRoomName), CurrentRoomName);
                savedvalues[type].Add(nameof(_CurrentRoomCompleted), _CurrentRoomCompleted);
                savedvalues[type].Add(nameof(_CurrentRoomCompletedResetOnDeath), _CurrentRoomCompletedResetOnDeath);
            } else {
                savedvalues[type][nameof(PreviousRoomName)] = PreviousRoomName;
                savedvalues[type][nameof(CurrentRoomName)] = CurrentRoomName;
                savedvalues[type][nameof(_CurrentRoomCompleted)] = _CurrentRoomCompleted;
                savedvalues[type][nameof(_CurrentRoomCompletedResetOnDeath)] = _CurrentRoomCompletedResetOnDeath;
            }
        }

        public void SpeedrunToolLoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Type type = GetType();
            if (!savedvalues.ContainsKey(type)) {
                Logger.Log(nameof(ConsistencyTrackerModule), "Trying to load state without prior saving a state...");
                return;
            }

            PreviousRoomName = (string) savedvalues[type][nameof(PreviousRoomName)];
            CurrentRoomName = (string) savedvalues[type][nameof(CurrentRoomName)];
            _CurrentRoomCompleted = (bool) savedvalues[type][nameof(_CurrentRoomCompleted)];
            _CurrentRoomCompletedResetOnDeath = (bool) savedvalues[type][nameof(_CurrentRoomCompletedResetOnDeath)];

            CurrentChapterStats.SetCurrentRoom(CurrentRoomName);
            SaveChapterStats();
        }

        public void SpeedrunToolClearState() {
            //No action
        }

        #endregion

        #endregion

        #region Data Import/Export

        public static string GetPathToFile(string file) {
            return BaseFolderPath + file;
        }
        public static string GetPathToFolder(string folder) {
            return BaseFolderPath + folder + "/";
        }
        /// <summary>Checks the folder exists.</summary>
        /// <param name="folderPath">The folder path.</param>
        /// <returns>true when the folder already existed, false when a new folder has been created.</returns>
        public static bool CheckFolderExists(string folderPath) {
            if (!Directory.Exists(folderPath)) {
                Directory.CreateDirectory(folderPath);
                return false;
            }

            return true;
        }


        public bool PathInfoExists() {
            string path = GetPathToFile($"paths/{CurrentChapterDebugName}.txt");
            return File.Exists(path);
        }
        public PathInfo GetPathInputInfo() {
            Log($"[GetPathInputInfo] Fetching path info for chapter '{CurrentChapterDebugName}'");

            string path = GetPathToFile($"paths/{CurrentChapterDebugName}.txt");
            Log($"\tSearching for path '{path}'");

            if (File.Exists(path)) { //Parse File
                Log($"\tFound file, parsing...");
                string content = File.ReadAllText(path);

                try {
                    return PathInfo.ParseString(content, Log);
                } catch (Exception) {
                    Log($"\tCouldn't read old path info, created new PathInfo. Old path info content:\n{content}");
                    PathInfo toRet = new PathInfo() { };
                    return toRet;
                }

            } else { //Create new
                Log($"\tDidn't find file, returned null.");
                return null;
            }
        }

        public ChapterStats GetCurrentChapterStats() {
            string path = GetPathToFile($"stats/{CurrentChapterDebugName}.txt");

            bool hasEnteredThisSession = ChaptersThisSession.Contains(CurrentChapterDebugName);
            ChaptersThisSession.Add(CurrentChapterDebugName);
            Log($"[GetCurrentChapterStats] CurrentChapterName: '{CurrentChapterDebugName}', hasEnteredThisSession: '{hasEnteredThisSession}', ChaptersThisSession: '{string.Join(", ", ChaptersThisSession)}'");

            ChapterStats toRet;

            if (File.Exists(path)) { //Parse File
                string content = File.ReadAllText(path);
                toRet = ChapterStats.ParseString(content);
                toRet.ChapterDebugName = CurrentChapterDebugName;

            } else { //Create new
                toRet = new ChapterStats() {
                    ChapterDebugName = CurrentChapterDebugName,
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

            CurrentUpdateFrame++;

            CurrentChapterStats.ModState.PlayerIsHoldingGolden = _PlayerIsHoldingGolden;
            CurrentChapterStats.ModState.GoldenDone = _PlayerIsHoldingGolden && CurrentChapterStats.ModState.ChapterCompleted;

            CurrentChapterStats.ModState.DeathTrackingPaused = ModSettings.PauseDeathTracking;
            CurrentChapterStats.ModState.RecordingPath = ModSettings.RecordPath;
            CurrentChapterStats.ModState.OverlayVersion = OverlayVersion;
            CurrentChapterStats.ModState.ModVersion = ModVersion;
            CurrentChapterStats.ModState.ChapterHasPath = CurrentChapterPath != null;


            string path = GetPathToFile($"stats/{CurrentChapterDebugName}.txt");
            File.WriteAllText(path, CurrentChapterStats.ToChapterStatsString());

            string modStatePath = GetPathToFile($"stats/modState.txt");

            string content = $"{CurrentChapterStats.CurrentRoom}\n{CurrentChapterStats.ChapterDebugName};{CurrentChapterStats.ModState}\n";
            File.WriteAllText(modStatePath, content);

            StatsManager.OutputFormats(CurrentChapterPath, CurrentChapterStats);
        }

        public void CreateChapterSummary(int attemptCount) {
            Log($"[CreateChapterSummary(attemptCount={attemptCount})] Attempting to create tracker summary");

            bool hasPathInfo = PathInfoExists();

            string relativeOutPath = $"summaries/{CurrentChapterDebugName}.txt";
            string outPath = GetPathToFile(relativeOutPath);

            if (!hasPathInfo) {
                Log($"Called CreateChapterSummary without chapter path info. Please create a path before using this feature");
                File.WriteAllText(outPath, "No path info was found for the current chapter.\nPlease create a path before using the summary feature");
                return;
            }

            CurrentChapterStats?.OutputSummary(outPath, CurrentChapterPath, attemptCount);
        }

        #endregion

        #region Default Path Creation

        public void CreatePrepackagedPaths() {
            CreatePathFile(nameof(Resources.Celeste_1_ForsakenCity_Normal), Resources.Celeste_1_ForsakenCity_Normal);
            CreatePathFile(nameof(Resources.Celeste_1_ForsakenCity_BSide), Resources.Celeste_1_ForsakenCity_BSide);
            CreatePathFile(nameof(Resources.Celeste_1_ForsakenCity_CSide), Resources.Celeste_1_ForsakenCity_CSide);

            CreatePathFile(nameof(Resources.Celeste_2_OldSite_Normal), Resources.Celeste_2_OldSite_Normal);
            CreatePathFile(nameof(Resources.Celeste_2_OldSite_BSide), Resources.Celeste_2_OldSite_BSide);
            CreatePathFile(nameof(Resources.Celeste_2_OldSite_CSide), Resources.Celeste_2_OldSite_CSide);

            CreatePathFile(nameof(Resources.Celeste_3_CelestialResort_Normal), Resources.Celeste_3_CelestialResort_Normal);
            CreatePathFile(nameof(Resources.Celeste_3_CelestialResort_BSide), Resources.Celeste_3_CelestialResort_BSide);
            CreatePathFile(nameof(Resources.Celeste_3_CelestialResort_CSide), Resources.Celeste_3_CelestialResort_CSide);

            CreatePathFile(nameof(Resources.Celeste_4_GoldenRidge_Normal), Resources.Celeste_4_GoldenRidge_Normal);
            CreatePathFile(nameof(Resources.Celeste_4_GoldenRidge_BSide), Resources.Celeste_4_GoldenRidge_BSide);
            CreatePathFile(nameof(Resources.Celeste_4_GoldenRidge_CSide), Resources.Celeste_4_GoldenRidge_CSide);

            CreatePathFile(nameof(Resources.Celeste_5_MirrorTemple_Normal), Resources.Celeste_5_MirrorTemple_Normal);
            CreatePathFile(nameof(Resources.Celeste_5_MirrorTemple_BSide), Resources.Celeste_5_MirrorTemple_BSide);
            CreatePathFile(nameof(Resources.Celeste_5_MirrorTemple_CSide), Resources.Celeste_5_MirrorTemple_CSide);

            CreatePathFile(nameof(Resources.Celeste_6_Reflection_Normal), Resources.Celeste_6_Reflection_Normal);
            CreatePathFile(nameof(Resources.Celeste_6_Reflection_BSide), Resources.Celeste_6_Reflection_BSide);
            CreatePathFile(nameof(Resources.Celeste_6_Reflection_CSide), Resources.Celeste_6_Reflection_CSide);

            CreatePathFile(nameof(Resources.Celeste_7_Summit_Normal), Resources.Celeste_7_Summit_Normal);
            CreatePathFile(nameof(Resources.Celeste_7_Summit_BSide), Resources.Celeste_7_Summit_BSide);
            CreatePathFile(nameof(Resources.Celeste_7_Summit_CSide), Resources.Celeste_7_Summit_CSide);

            CreatePathFile(nameof(Resources.Celeste_9_Core_Normal), Resources.Celeste_9_Core_Normal);
            CreatePathFile(nameof(Resources.Celeste_9_Core_BSide), Resources.Celeste_9_Core_BSide);
            CreatePathFile(nameof(Resources.Celeste_9_Core_CSide), Resources.Celeste_9_Core_CSide);

            CreatePathFile(nameof(Resources.Celeste_LostLevels_Normal), Resources.Celeste_LostLevels_Normal);

        }
        private void CreatePathFile(string name, string content) {
            string path = GetPathToFile($"paths/{name}.txt");
            File.WriteAllText(path, content);
        }

        public void CreateExternalOverlay() {
            CreateOverlayFile("CCTOverlay.css", Resources.CCTOverlay_CSS);
            CreateOverlayFile("CCTOverlay.html", Resources.CCTOverlay_HTML);
            CreateOverlayFile("CCTOverlay.js", Resources.CCTOverlay_JS);
            CreateOverlayFile("common.js", Resources.CCT_common_JS);
            CheckFolderExists(GetPathToFolder($"external-overlay/img"));
            Resources.goldberry_GIF.Save(GetPathToFile($"external-overlay/img/goldberry.gif"));
        }
        private void CreateOverlayFile(string name, string content) {
            string path = GetPathToFile($"external-overlay/{name}");
            File.WriteAllText(path, content);
        }

        #endregion

        #region Stats Data Control

        public void WipeChapterData() {
            if (CurrentChapterStats == null) {
                Log($"[WipeChapterData] Aborting wiping chapter data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"[WipeChapterData] Wiping death data for chapter '{CurrentChapterDebugName}'");

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

        public void RemoveRoomGoldenBerryDeaths() {
            if (CurrentChapterStats == null) {
                Log($"[WipeChapterGoldenBerryDeaths] Aborting wiping room golden berry deaths as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"[WipeChapterGoldenBerryDeaths] Wiping golden berry death data for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.CurrentRoom.GoldenBerryDeaths = 0;
            CurrentChapterStats.CurrentRoom.GoldenBerryDeathsSession = 0;

            SaveChapterStats();
        }
        public void WipeChapterGoldenBerryDeaths() {
            if (CurrentChapterStats == null) {
                Log($"[WipeChapterGoldenBerryDeaths] Aborting wiping chapter golden berry deaths as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"[WipeChapterGoldenBerryDeaths] Wiping golden berry death data for chapter '{CurrentChapterDebugName}'");

            foreach (string debugName in CurrentChapterStats.Rooms.Keys) {
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeaths = 0;
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeathsSession = 0;
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

            while (CurrentChapterStats.CurrentRoom.PreviousAttempts.Count > 0 && CurrentChapterStats.CurrentRoom.LastAttempt == false) {
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

        #endregion

        #region Path Management

        public void SaveRecordedRoomPath() {
            Log($"[{nameof(SaveRecordedRoomPath)}] Saving recorded path...");
            DisabledInRoomName = CurrentRoomName;
            CurrentChapterPath = Path.ToPathInfo();
            Log($"[{nameof(SaveRecordedRoomPath)}] Recorded path:\n{CurrentChapterPath.ToString()}");
            SaveRoomPath();
        }
        public void SaveRoomPath() {
            string relativeOutPath = $"paths/{CurrentChapterDebugName}.txt";
            string outPath = GetPathToFile(relativeOutPath);
            File.WriteAllText(outPath, CurrentChapterPath.ToString());
            Log($"Wrote path data to '{relativeOutPath}'");
        }

        public void RemoveRoomFromChapter() {
            if (CurrentChapterPath == null) {
                Log($"[RemoveRoomFromChapter] CurrentChapterPath was null");
                return;
            }

            bool foundRoom = false;
            foreach (CheckpointInfo cpInfo in CurrentChapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.DebugRoomName != CurrentRoomName) continue;

                    cpInfo.Rooms.Remove(rInfo);
                    foundRoom = true;
                    break;
                }

                if (foundRoom) break;
            }

            if (foundRoom) {
                SaveRoomPath();
            }
        }

        #endregion

        #region Logging

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

        #endregion

        #region Util
        public static string SanitizeSIDForDialog(string sid) {
            string bsSuffix = "_pqeijpvqie";
            string dialogCleaned = Dialog.Get($"{sid}{bsSuffix}");
            return dialogCleaned.Substring(1, sid.Length);
        }

        public void InsertCheckpointIntoPath(Checkpoint cp, string roomName) {
            if (roomName == null) {
                Path.AddCheckpoint(cp, PathRecorder.DefaultCheckpointName);
                return;
            }

            string cpDialogName = $"{CurrentChapterStats.ChapterSIDDialogSanitized}_{roomName}";
            //Log($"[Dialog Testing] cpDialogName: {cpDialogName}");
            string cpName = Dialog.Get(cpDialogName);
            //Log($"[Dialog Testing] Dialog.Get says: {cpName}");

            if (cpName.Length+1 >= cpDialogName.Length && cpName.Substring(1, cpDialogName.Length) == cpDialogName) cpName = null;

            Path.AddCheckpoint(cp, cpName);
        }
        #endregion


        public class ConsistencyTrackerSettings : EverestModuleSettings {
            public bool Enabled { get; set; } = true;

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
                menu.Add(new TextMenu.OnOff("Only Track Deaths With Golden Berry", this.OnlyTrackWithGoldenBerry) {
                    OnValueChange = v => {
                        this.OnlyTrackWithGoldenBerry = v;
                    }
                });
            }

            public bool RecordPath { get; set; } = false;
            public void CreateRecordPathEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Path Recording", false);

                subMenu.Add(new TextMenu.SubHeader("!!!Existing paths will be overwritten!!!"));
                subMenu.Add(new TextMenu.OnOff("Record Path", Instance.DoRecordPath) {
                    OnValueChange = v => {
                        if (v)
                            Instance.Log($"Recording chapter path...");
                        else
                            Instance.Log($"Stopped recording path. Outputting info...");

                        this.RecordPath = v;
                        Instance.DoRecordPath = v;
                        Instance.SaveChapterStats();
                    }
                });

                subMenu.Add(new TextMenu.SubHeader("Editing the path requires a reload of the Overlay"));
                subMenu.Add(new TextMenu.Button("Remove Current Room From Path") {
                    OnPressed = Instance.RemoveRoomFromChapter
                });

                menu.Add(subMenu);
            }


            public bool WipeChapter { get; set; } = false;
            public void CreateWipeChapterEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("!!Data Wipe!!", false);
                subMenu.Add(new TextMenu.SubHeader("These actions cannot be reverted!"));


                subMenu.Add(new TextMenu.SubHeader("Current Room"));
                subMenu.Add(new TextMenu.Button("Remove Last Attempt") {
                    OnPressed = () => {
                        Instance.RemoveLastAttempt();
                    }
                });

                subMenu.Add(new TextMenu.Button("Remove Last Death Streak") {
                    OnPressed = () => {
                        Instance.RemoveLastDeathStreak();
                    }
                });

                subMenu.Add(new TextMenu.Button("Remove All Attempts") {
                    OnPressed = () => {
                        Instance.WipeRoomData();
                    }
                });

                subMenu.Add(new TextMenu.Button("Remove Golden Berry Deaths") {
                    OnPressed = () => {
                        Instance.RemoveRoomGoldenBerryDeaths();
                    }
                });


                subMenu.Add(new TextMenu.SubHeader("Current Chapter"));
                subMenu.Add(new TextMenu.Button("Reset All Attempts") {
                    OnPressed = () => {
                        Instance.WipeChapterData();
                    }
                });

                subMenu.Add(new TextMenu.Button("Reset All Golden Berry Deaths") {
                    OnPressed = () => {
                        Instance.WipeChapterGoldenBerryDeaths();
                    }
                });


                menu.Add(subMenu);
            }


            public bool CreateSummary { get; set; } = false;
            public int SummarySelectedAttemptCount { get; set; } = 20;
            public void CreateCreateSummaryEntry(TextMenu menu, bool inGame) {
                if (!inGame) return;

                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Tracker Summary", false);
                subMenu.Add(new TextMenu.SubHeader("Outputs some cool data of the current chapter in a readable .txt format"));


                subMenu.Add(new TextMenu.SubHeader("When calculating the consistency stats, only the last X attempts will be counted"));
                List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
                subMenu.Add(new TextMenuExt.EnumerableSlider<int>("Summary Over X Attempts", AttemptCounts, SummarySelectedAttemptCount) {
                    OnValueChange = (value) => {
                        SummarySelectedAttemptCount = value;
                    }
                });


                subMenu.Add(new TextMenu.Button("Create Chapter Summary") {
                    OnPressed = () => {
                        Instance.CreateChapterSummary(SummarySelectedAttemptCount);
                    }
                });

                menu.Add(subMenu);
            }



            //Live Data Settings:
            //- Percentages digit cutoff (default: 2)
            //- Stats over X Attempts
            //- Reload format file
            //- Toggle name/abbreviation for e.g. PB Display
            public bool LiveData { get; set; } = false;
            public int LiveDataDecimalPlaces { get; set; } = 2;
            public int LiveDataSelectedAttemptCount { get; set; } = 20;

            //Types: 1 -> EH-3 | 2 -> Event-Horizon-3
            [SettingIgnore]
            public RoomNameDisplayType LiveDataRoomNameDisplayType { get; set; } = RoomNameDisplayType.AbbreviationAndRoomNumberInCP;

            //Types: 1 -> EH-3 | 2 -> Event-Horizon-3
            [SettingIgnore]
            public ListFormat LiveDataListOutputFormat { get; set; } = ListFormat.Json;

            [SettingIgnore]
            public bool LiveDataHideFormatsWithoutPath { get; set; } = false;

            [SettingIgnore]
            public bool LiveDataIgnoreUnplayedRooms { get; set; } = false;

            public void CreateLiveDataEntry(TextMenu menu, bool inGame) {
                TextMenuExt.SubMenu subMenu = new TextMenuExt.SubMenu("Live Data Settings", false);


                subMenu.Add(new TextMenu.SubHeader("Floating point numbers will be rounded to this decimal"));
                List<KeyValuePair<int, string>> DigitCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(0, "0"),
                    new KeyValuePair<int, string>(1, "1"),
                    new KeyValuePair<int, string>(2, "2"),
                    new KeyValuePair<int, string>(3, "3"),
                    new KeyValuePair<int, string>(4, "4"),
                    new KeyValuePair<int, string>(5, "5"),
                };
                subMenu.Add(new TextMenuExt.EnumerableSlider<int>("Max. Decimal Places", DigitCounts, LiveDataDecimalPlaces) {
                    OnValueChange = (value) => {
                        LiveDataDecimalPlaces = value;
                    }
                });


                subMenu.Add(new TextMenu.SubHeader("When calculating room consistency stats, only the last X attempts in each room will be counted"));
                List<KeyValuePair<int, string>> AttemptCounts = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>(5, "5"),
                    new KeyValuePair<int, string>(10, "10"),
                    new KeyValuePair<int, string>(20, "20"),
                    new KeyValuePair<int, string>(100, "100"),
                };
                subMenu.Add(new TextMenuExt.EnumerableSlider<int>("Consider Last X Attempts", AttemptCounts, LiveDataSelectedAttemptCount) {
                    OnValueChange = (value) => {
                        LiveDataSelectedAttemptCount = value;
                    }
                });


                subMenu.Add(new TextMenu.SubHeader("Whether you want checkpoint names to be full or abbreviated in the room name"));
                List<KeyValuePair<int, string>> PBNameTypes = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.AbbreviationAndRoomNumberInCP, "EH-3"),
                    new KeyValuePair<int, string>((int)RoomNameDisplayType.FullNameAndRoomNumberInCP, "Event-Horizon-3"),
                };
                subMenu.Add(new TextMenuExt.EnumerableSlider<int>("Room Name Format", PBNameTypes, (int)LiveDataRoomNameDisplayType) {
                    OnValueChange = (value) => {
                        LiveDataRoomNameDisplayType = (RoomNameDisplayType)value;
                    }
                });


                subMenu.Add(new TextMenu.SubHeader("Output format for lists. Plain is easily readable, JSON is for programming purposes"));
                List<KeyValuePair<int, string>> ListTypes = new List<KeyValuePair<int, string>>() {
                    new KeyValuePair<int, string>((int)ListFormat.Plain, "Plain"),
                    new KeyValuePair<int, string>((int)ListFormat.Json, "JSON"),
                };
                subMenu.Add(new TextMenuExt.EnumerableSlider<int>("List Output Format", ListTypes, (int)LiveDataListOutputFormat) {
                    OnValueChange = (value) => {
                        LiveDataListOutputFormat = (ListFormat)value;
                    }
                });


                subMenu.Add(new TextMenu.SubHeader("If a format depends on path information and no path is set, the format will be blanked out"));
                subMenu.Add(new TextMenu.OnOff("Hide Formats When No Path", LiveDataHideFormatsWithoutPath) {
                    OnValueChange = v => {
                        LiveDataHideFormatsWithoutPath = v;
                    }
                });


                subMenu.Add(new TextMenu.SubHeader("For chance calculation unplayed rooms count as 0% success rate. Toggle this on to ignore unplayed rooms"));
                subMenu.Add(new TextMenu.OnOff("Ignore Unplayed Rooms", LiveDataIgnoreUnplayedRooms) {
                    OnValueChange = v => {
                        LiveDataIgnoreUnplayedRooms = v;
                    }
                });


                subMenu.Add(new TextMenu.SubHeader($"After editing '{StatManager.BaseFolder}/{StatManager.FormatFileName}' use this to update the live data format"));
                subMenu.Add(new TextMenu.Button("Reload format file") {
                    OnPressed = () => {
                        Instance.StatsManager.LoadFormats();
                    }
                });

                menu.Add(subMenu);
            }

        }
    }
}
