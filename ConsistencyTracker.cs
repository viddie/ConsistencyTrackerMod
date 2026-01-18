using Celeste.Mod.ConsistencyTracker.Entities;
using Celeste.Mod.ConsistencyTracker.Entities.Summary;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.EverestInterop;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.PhysicsLog;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.ThirdParty;
using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.ModInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using On.Celeste.Editor;
using GameData = Celeste.Mod.ConsistencyTracker.Utility.GameData;

namespace Celeste.Mod.ConsistencyTracker {
    public class ConsistencyTrackerModule : EverestModule {

        public static ConsistencyTrackerModule Instance;
        private const int LOG_FILE_COUNT = 10;

        private static readonly object LogLock = new object();

        #region Versions
        public static class VersionsNewest {
            public static string Mod => "2.9.8";
            public static string Overlay => "2.0.0";
            public static string LiveDataEditor => "1.0.1";
            public static string PhysicsInspector => "1.4.2";
        }
        public static class VersionsCurrent {
            public static string Overlay {
                get => Instance.ModSettings.OverlayVersion;
                set => Instance.ModSettings.OverlayVersion = value;
            }
            public static string LiveDataEditor {
                get => Instance.ModSettings.LiveDataEditorVersion;
                set => Instance.ModSettings.LiveDataEditorVersion = value;
            }
            public static string PhysicsInspector {
                get => Instance.ModSettings.PhysicsInspectorVersion;
                set => Instance.ModSettings.PhysicsInspectorVersion = value;
            }
        }
        #endregion

        public override Type SettingsType => typeof(ConsistencyTrackerSettings);
        public ConsistencyTrackerSettings ModSettings => (ConsistencyTrackerSettings)_Settings;

        public static string BaseFolderPath = "./ConsistencyTracker/";
        public static readonly string ExternalToolsFolder = "external-tools";
        public static readonly string LogsFolder = "logs";
        public static readonly string PathsFolder = "paths";
        public static readonly string StatsFolder = "stats";
        public static readonly string FgrFolder = "fgr";
        public static readonly string SummariesFolder = "summaries";


        private bool DidRestart { get; set; }
        private HashSet<string> PathsThisSession { get; set; } = new HashSet<string>();

        #region Path Recording Variables

        public bool DoRecordPath {
            get => DoRecordPathInternal;
            set {
                if (value) {
                    if (DisabledInRoomName != CurrentRoomName) {
                        PathRec = new PathRecorder();
                        InsertCheckpointIntoPath(null, LastRoomWithCheckpoint);
                        PathRec.AddRoom(CurrentRoomName);
                    }
                } else {
                    SaveRecordedRoomPath();
                }

                DoRecordPathInternal = value;
                Log($"DoRecordPath is now '{DoRecordPathInternal}'");
            }
        }
        private bool DoRecordPathInternal;
        public PathRecorder PathRec;
        private string DisabledInRoomName;
        public bool AbortPathRecording;
        
        #endregion

        #region State Variables

        //Used to cache and prevent unnecessary operations via DebugRC
        public long CurrentUpdateFrame;

        // For DebugRC endpoint "recentChapters"
        public List<Tuple<ChapterStatsList, PathSegmentList>> LastVisitedChapters = new List<Tuple<ChapterStatsList, PathSegmentList>>();
        public const int MAX_LAST_VISITED_CHAPTERS = 10;

        public bool IsInFgrMode {
            get {
                if (ModSettings.SelectedFgr == 0) return false;
                return FgrPathExists(ModSettings.SelectedFgr);
            }
        }

        public bool IsInOrPastFinal => CurrentChapterPath != null 
                                       && CurrentChapterPath.CurrentRoom != null
                                       && CurrentChapterPath.CurrentRoom.RoomNumberInChapter >= CurrentChapterPath.GameplayRoomCount;

        public bool IsInFinalRoomOfMap => CurrentChapterPath != null
                                          && CurrentChapterPath.CurrentRoom != null
                                          && (CurrentChapterPath.CurrentRoom.NextRoomInChapter == null || CurrentChapterPath.CurrentRoom.UID !=
                                              CurrentChapterPath.CurrentRoom.NextRoomInChapter.UID);
        
        public bool IsInFirstRoom => CurrentChapterPath != null 
                                    && CurrentChapterPath.CurrentRoom != null
                                    && CurrentChapterPath.CurrentRoom.DebugRoomName == CurrentChapterPath.WalkPath().First().DebugRoomName;

        public PathSegmentList CurrentChapterPathSegmentList { get; set; }
        public PathInfo CurrentChapterPath {
            get => CurrentChapterPathSegmentList?.CurrentPath;
            set {
                if (CurrentChapterPathSegmentList != null) {
                    CurrentChapterPathSegmentList.CurrentPath = value;
                }
            }
        }
        public int SelectedPathSegmentIndex => CurrentChapterPathSegmentList?.SelectedIndex ?? 0;

        public ChapterStatsList CurrentChapterStatsList { get; set; }
        public ChapterStats CurrentChapterStats { 
            get => CurrentChapterStatsList?.GetStats(SelectedPathSegmentIndex);
            set => CurrentChapterStatsList?.SetStats(SelectedPathSegmentIndex, value);
        }

        public ChapterMetaInfo ActualChapterMetaInfo { get; set; }
        public string CurrentChapterUID;
        public string CurrentChapterUIDForPath => ChapterMetaInfo.GetChapterUIDForPath(CurrentChapterUID);
        public string PreviousRoomName;
        public string CurrentRoomName;
        public string SpeedrunToolSaveStateRoomName;
        public Session LastSession;

        private string LastRoomWithCheckpoint;

        private bool CurrentRoomCompleted;
        private bool CurrentRoomCompletedResetOnDeath;

        public bool IsInGoldenRun {
            get => ModSettings.IsInRun;
            set => ModSettings.IsInRun = value;
        }

        public bool HasTriggeredPbEvent { get; set; }
        public bool HasTriggeredAfterPbEvent { get; set; }
        
        #endregion

        #region Mod Options State Variables
        //For combining multiple mod options settings into one logical state
        public bool SettingsTrackGoldens => !ModSettings.PauseDeathTracking || ModSettings.TrackingAlwaysGoldenDeaths;
        public bool SettingsTrackAttempts => !ModSettings.PauseDeathTracking && (!ModSettings.TrackingOnlyWithGoldenBerry || IsInGoldenRun);
        #endregion

        public StatManager StatsManager;
        public TextOverlay IngameOverlay;
        public GraphOverlay GraphOverlay;
        public SummaryHud SummaryOverlay;
        public PhysicsLogger PhysicsLog;
        public MultiPacePingManager MultiPacePingManager;

        public DebugMapUtil DebugMapUtil;


        public ConsistencyTrackerModule() {
            Instance = this;
        }

        #region Load/Unload Stuff

        public override void Load() {
            CheckRootFolder();
            CheckFolderExists(GetPathToFile(LogsFolder));
            LogInit();
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log($"~~~==== CCT STARTED ({time}) ====~~~");
            
            GameData.Load();
            
            CheckFolderExists(GetPathToFile(PathsFolder));
            ResourceUnpacker.CheckPrepackagedPaths(reset:false);
            
            CheckFolderExists(GetPathToFile(StatsFolder));
            CheckFolderExists(GetPathToFile(FgrFolder));
            CheckFolderExists(GetPathToFile(SummariesFolder));


            CheckFolderExists(GetPathToFile(ExternalToolsFolder));
            ResourceUnpacker.UpdateExternalTools();


            Log($"Mod Settings -> \n{JsonConvert.SerializeObject(ModSettings, Formatting.Indented)}");
            Log($"~~~==============================~~~");

            DebugMapUtil = new DebugMapUtil();
            PhysicsLog = new PhysicsLogger();
            MultiPacePingManager = new MultiPacePingManager();
            HookStuff();

            
            //Interop
            DebugRcPage.Load();
            typeof(ConsistencyTrackerAPI).ModInterop();
        }

        public override void Unload() {
            Log($"Called Unload");
            UnHookStuff();
            
            if (PhysicsLogger.Settings.IsRecording) {
                PhysicsLog.StopRecording();
            }

            GameData.Unload();
            DebugRcPage.Unload();
            LogCleanup();
        }

        private void HookStuff() {
            //Create stats manager
            Everest.Events.MainMenu.OnCreateButtons += MainMenu_OnCreateButtons;
            //Track where the player is
            On.Celeste.Level.Begin += Level_Begin;
            On.Celeste.Level.End += LevelOnEnd;
            Everest.Events.Level.OnExit += Level_OnExit;
            Everest.Events.Level.OnComplete += Level_OnComplete;
            Everest.Events.Level.OnTransitionTo += Level_OnTransitionTo;
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            // On.Celeste.Level.Update += LevelOnUpdate;
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
            On.Celeste.Strawberry.OnPlayer += Strawberry_OnPlayer; //sorta works, but triggers very often for a single berry
            On.Celeste.Strawberry.OnCollect += On_Strawberry_OnCollect;

            //Changing lava/ice in Core
            On.Celeste.CoreModeToggle.OnChangeMode += CoreModeToggle_OnChangeMode; //works
            //Picking up a Cassette tape
            On.Celeste.Cassette.OnPlayer += Cassette_OnPlayer; //works

            //Open up key doors?
            //On.Celeste.Door.Open += Door_Open; //Wrong door (those are the resort doors)
            On.Celeste.LockBlock.TryOpen += LockBlock_TryOpen; //works
            
            On.Celeste.Level.UpdateTime += Level_UpdateTime;
            On.Celeste.Level.Update += LevelOnUpdate;
            MapEditor.ctor += MapEditorOnCtor;

            //Self hooks
            HookCustom();
            
            //Other objects
            PhysicsLog.Hook();
            DebugMapUtil.Hook();
            MultiPacePingManager.Hook();
        }

        private void UnHookStuff() {
            Everest.Events.MainMenu.OnCreateButtons -= MainMenu_OnCreateButtons;
            On.Celeste.Level.Begin -= Level_Begin;
            Everest.Events.Level.OnExit -= Level_OnExit;
            Everest.Events.Level.OnComplete -= Level_OnComplete;
            Everest.Events.Level.OnTransitionTo -= Level_OnTransitionTo;
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
            // On.Celeste.Level.Update -= LevelOnUpdate;
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
            On.Celeste.Strawberry.OnCollect -= On_Strawberry_OnCollect;

            //Changing lava/ice in Core
            On.Celeste.CoreModeToggle.OnChangeMode -= CoreModeToggle_OnChangeMode;

            //Picking up a Cassette tape
            On.Celeste.Cassette.OnPlayer -= Cassette_OnPlayer;

            //Open up key doors
            On.Celeste.LockBlock.TryOpen -= LockBlock_TryOpen;

            On.Celeste.Level.UpdateTime -= Level_UpdateTime;
            MapEditor.ctor -= MapEditorOnCtor;

            //Self hooks
            UnHookCustom();

            //Other objects
            PhysicsLog.UnHook();
            DebugMapUtil.UnHook();
            MultiPacePingManager.UnHook();
        }

        public override void Initialize()
        {
            base.Initialize();
            SpeedrunToolSupport.Load();
        }

        #endregion

        #region Hooks

        /// <summary>
        /// Hook to check button bindings which should only be active when the level is unpaused.
        /// </summary>
        /// <param name="orig"></param>
        /// <param name="self"></param>
        private void LevelOnUpdate(On.Celeste.Level.orig_Update orig, Level self) {
            orig(self);

            if (self.Paused) return;

            if (ModSettings.ButtonFgrToggleInRun.Pressed) {
                IsInGoldenRun = !IsInGoldenRun;
                SaveSettings();
            }

            if (ModSettings.ButtonFgrTeleportToNextMap.Pressed && CurrentChapterPath != null && CurrentChapterPath.CurrentRoom != null && IsInFgrMode) {
                RoomInfo nextMap = CurrentChapterPath.CurrentRoom;
                bool found = false;
                while (!found) {
                    nextMap = nextMap.NextRoomInChapter;
                    if (nextMap == null || nextMap.UID != CurrentChapterUID) {
                        found = true;
                    }
                }
                if (nextMap == null) {
                    Log($"There is no next map to load into.");
                    return;
                }
                ConsoleCommands.LoadRoom(nextMap, IsInGoldenRun && !IsInFinalRoomOfMap);
            }

            if (ModSettings.ButtonFgrTeleportToPreviousMap.Pressed && CurrentChapterPath != null && CurrentChapterPath.CurrentRoom != null && IsInFgrMode) {
                RoomInfo previousMap = CurrentChapterPath.CurrentRoom;
                Log($"Check from current room backwards: {previousMap.DebugRoomName}");
                bool found = false;
                while (!found) {
                    previousMap = previousMap.PreviousRoomInChapter;
                    Log($"Checking previous room: {previousMap?.DebugRoomName}");
                    if (previousMap == null || previousMap.UID != CurrentChapterUID) {
                        found = true;
                    }
                }
                if (previousMap == null) {
                    // No previous map exists on the path. Instead of doing nothing, fall back to
                    // teleporting to the first room of the current map (expected behavior).
                    Log($"There is no previous map to load into. Falling back to first room of current map.");
                    previousMap = CurrentChapterPath.CurrentRoom;
                }

                // As opposed to next map, we want to load into not the first room found, but the first room of the map
                // So we need to keep searching until we find a room with a different UID again, then use the next one.
                RoomInfo firstRoomOfMap = previousMap;
                found = false;
                while (!found) {
                    RoomInfo temp = firstRoomOfMap.PreviousRoomInChapter;
                    if (temp == null || temp.UID != previousMap.UID) {
                        found = true;
                    } else {
                        firstRoomOfMap = temp;
                    }
                }

                ConsoleCommands.LoadRoom(firstRoomOfMap);
            }

            if (ModSettings.ButtonFgrReset.Pressed && CurrentChapterPath != null && IsInFgrMode) {
                RoomInfo rInfo = CurrentChapterPath.WalkPath().First();
                if (rInfo == null) {
                    Log($"There is no initial room to load into.");
                    return;
                }
                ConsoleCommands.LoadRoom(rInfo);
            }

            if (ModSettings.ButtonTogglePauseDeathTracking.Pressed) {
                ModSettings.PauseDeathTracking = !ModSettings.PauseDeathTracking;
                SaveSettings();
                Log($"ButtonTogglePauseDeathTracking: Toggled pause death tracking to {ModSettings.PauseDeathTracking}");
            }

            if (ModSettings.ButtonToggleDifficultyGraph.Pressed) {
                bool currentVisible = ModSettings.IngameOverlayGraphEnabled;
                ModSettings.IngameOverlayGraphEnabled = !currentVisible;
                SaveSettings();
                Log($"Toggled ingame graph to {ModSettings.IngameOverlayGraphEnabled}");
            }

            if (ModSettings.ButtonAddRoomSuccess.Pressed) {
                if (CurrentChapterStats != null) {
                    Log($"ButtonAddRoomSuccess: Adding room attempt success");
                    AddRoomAttempt(true);
                }
            }

            if (ModSettings.ButtonRemoveRoomLastAttempt.Pressed) {
                if (CurrentChapterStats != null) {
                    Log($"ButtonRemoveRoomLastAttempt: Removing last room attempt");
                    RemoveLastAttempt();
                }
            }

            if (ModSettings.ButtonRemoveRoomDeathStreak.Pressed) {
                if (CurrentChapterStats != null) {
                    Log($"ButtonRemoveRoomDeathStreak: Removing room death streak");
                    RemoveLastDeathStreak();
                }
            }

            if (ModSettings.ButtonToggleRecordPhysics.Pressed) {
                PhysicsLogger.Settings.IsRecording = !PhysicsLogger.Settings.IsRecording;
                Log($"ButtonToggleLogPhysics: Toggled logging of physics to {PhysicsLogger.Settings.IsRecording}");
            }

            if (ModSettings.ButtonImportCustomRoomNameFromClipboard.Pressed) {
                string text = TextInput.GetClipboardText().Trim();
                Log($"Importing custom room name from clipboard...");
                try {
                    SetCustomRoomName(text);
                } catch (Exception ex) {
                    Log($"Couldn't import custom room name from clipboard: {ex}");
                }
            }
        }

        private void MapEditorOnCtor(MapEditor.orig_ctor orig, Editor.MapEditor self, AreaKey area, bool reloadmapdata) {
            orig(self, area, reloadmapdata);
            // Opening the debug map should reset the run. Only relevant in FGR mode, as the normal ChangeChapter
            // would end the run anyway and double calling is fine. The IsInFgrMode check is expensive, so skip it.
            EndRun();
        }
        
        private void MainMenu_OnCreateButtons(OuiMainMenu menu, List<MenuButton> buttons) {
            if (StatsManager == null) {
                StatsManager = new StatManager();
            }
        }

        private void LockBlock_TryOpen(On.Celeste.LockBlock.orig_TryOpen orig, LockBlock self, Player player, Follower fol) {
            orig(self, player, fol);
            LogVerbose($"Opened a door");
            SetRoomCompleted(resetOnDeath: false);
        }

        private DashCollisionResults ClutterSwitch_OnDashed(On.Celeste.ClutterSwitch.orig_OnDashed orig, ClutterSwitch self, Player player, Vector2 direction) {
            LogVerbose($"Activated a clutter switch");
            SetRoomCompleted(resetOnDeath: false);
            return orig(self, player, direction);
        }

        private void Key_OnPlayer(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
            orig(self, player);
            LogVerbose($"Picked up a key");
            SetRoomCompleted(resetOnDeath: false);
        }

        private void Cassette_OnPlayer(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
            orig(self, player);
            LogVerbose($"Collected a cassette tape");
            SetRoomCompleted(resetOnDeath: false);
        }

        private readonly List<EntityID> TouchedBerries = new List<EntityID>();
        private readonly List<EntityID> IgnoreBerryCollects = new List<EntityID>();
        // All touched berries need to be reset on death, since they either:
        // - already collected
        // - disappeared on death
        private void Strawberry_OnPlayer(On.Celeste.Strawberry.orig_OnPlayer orig, Strawberry self, Player player) {
            //to not spam the log
            if (TouchedBerries.Contains(self.ID)) {
                orig(self, player);
                return;
            }
            TouchedBerries.Add(self.ID);

            LogVerbose($"Strawberry on player | self.Type: {self.GetType().Name}, self.Golden: {self.Golden}");
            SetRoomCompleted(resetOnDeath: true);
            
            if (self.Winged) {
                if(!IgnoreBerryCollects.Contains(self.ID))
                    IgnoreBerryCollects.Add(self.ID);
                LogVerbose($"Ignoring winged berry pickup.");
                orig(self, player);
                return;
            }
            
            GoldenType goldenType = GoldenType.Golden;
            bool isSpeedBerry = false;
            switch (self.GetType().Name) {
                case "Strawberry":
                    goldenType = GoldenType.Golden;
                    break;
                case "SilverBerry":
                    goldenType = GoldenType.Silver;
                    break;
                case "PlatinumBerry":
                    goldenType = GoldenType.Platinum;
                    break;
                case "SpeedBerry":
                    isSpeedBerry = true;
                    break;
            }

            if (self.Golden && !isSpeedBerry && !IsInFgrMode) {
                CurrentChapterStats.GoldenBerryType = goldenType;
                StartNewRun();
                SaveChapterStats();
                Events.Events.InvokeRunStarted();
            }
            
            orig(self, player);
        }

        private void On_Strawberry_OnCollect(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
            orig(self);

            if (IgnoreBerryCollects.Contains(self.ID)) {
                LogVerbose($"Ignoring winged berry collect.");
                return;
            }

            Log($"Berry collected! Type Name: '{self.GetType().Name}', Golden: '{self.Golden}', Winged: '{self.Winged}'");
            
            GoldenType goldenType = GoldenType.Golden;
            switch (self.GetType().Name) {
                case "Strawberry":
                    goldenType = GoldenType.Golden;
                    break;
                case "SilverBerry":
                    goldenType = GoldenType.Silver;
                    break;
                case "PlatinumBerry":
                    goldenType = GoldenType.Platinum;
                    break;
            }

            if (self.Golden && !self.Winged && SettingsTrackGoldens && !IsInFgrMode) { // In fgr mode, goldens dont exist, so they cant mark the end of the challenge.
                Log($"Golden collected! GG :catpog:");
                CurrentChapterStats.CollectedGolden(goldenType);
                SaveChapterStats();
                EndRun(won:true);
            }
        }

        private void CoreModeToggle_OnChangeMode(On.Celeste.CoreModeToggle.orig_OnChangeMode orig, CoreModeToggle self, Session.CoreModes mode) {
            LogVerbose($"Changed core mode to '{mode}'");
            orig(self, mode);
            SetRoomCompleted(resetOnDeath:true);
        }

        private void Checkpoint_TurnOn(On.Celeste.Checkpoint.orig_TurnOn orig, Checkpoint cp, bool animate) {
            orig(cp, animate);

            if (!(Engine.Scene is Level)) {
                LogVerbose($"Engine.Scene is not Level...");
                return;
            }

            Level level = Engine.Scene as Level;
            if (level == null) {
                LogVerbose($"level is null");
                return;
            } else if (level.Session == null) {
                LogVerbose($"level.Session is null");
                return;
            } else if (level.Session.LevelData == null) {
                LogVerbose($"level.Session.LevelData is null");
                return;
            } else {
                LogVerbose($"Checkpoint in room '{level.Session.LevelData.Name}'");
            }
            
            string roomName = GetRoomName(level.Session.LevelData.Name, IsInFgrMode, ActualChapterMetaInfo.ChapterUID);

            Log($"cp.Position={cp.Position}, Room Name='{roomName}'");
            if (DoRecordPath) {
                InsertCheckpointIntoPath(cp, roomName);
            }

            LastRoomWithCheckpoint = roomName;
        }

        //Not triggered when teleporting via debug map
        private void Level_TeleportTo(On.Celeste.Level.orig_TeleportTo orig, Level level, Player player, string nextLevel, Player.IntroTypes introType, Vector2? nearestSpawn) {
            orig(level, player, nextLevel, introType, nearestSpawn);

            string roomName = GetRoomName(level.Session.LevelData?.Name, IsInFgrMode, ActualChapterMetaInfo.ChapterUID);
            Log($"level.Session.LevelData.Name={roomName}");

            //if (ModSettings.CountTeleportsForRoomTransitions && CurrentRoomName != null && roomName != CurrentRoomName) {
            //    bool holdingGolden = PlayerIsHoldingGoldenBerry(level.Tracker.GetEntity<Player>());
            //    SetNewRoom(roomName, true, holdingGolden);
            //}
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            if (level.Session.LevelData == null) {
                Log($"level.Session.LevelData is null...");
                return;
            }

            ChapterMetaInfo chapterInfo = new ChapterMetaInfo(level.Session);
            string newCurrentRoom = GetRoomName(level.Session.LevelData.Name, IsInFgrMode, chapterInfo.ChapterUID);

            Log($"level.Session.LevelData.Name={newCurrentRoom}, playerIntro={playerIntro} | CurrentRoomName: '{CurrentRoomName}', PreviousRoomName: '{PreviousRoomName}', holdingGolden: '{IsInGoldenRun}'");
            bool isGoldenDeathOrDebugTeleport = playerIntro == Player.IntroTypes.Respawn;

            //Change room if we're not in the same room as before
            if (CurrentRoomName != null && newCurrentRoom != CurrentRoomName) {
                if (level.Session.LevelData.HasCheckpoint) {
                    LastRoomWithCheckpoint = newCurrentRoom;
                }
                SetNewRoom(newCurrentRoom, !isGoldenDeathOrDebugTeleport);
            }

            if (DidRestart) {
                if (level.Session.LevelData.HasCheckpoint) {
                    LastRoomWithCheckpoint = newCurrentRoom;
                }
                Log($"\tRequested reset of PreviousRoomName to null", true);
                DidRestart = false;
                SetNewRoom(newCurrentRoom, false);
                PreviousRoomName = null;
            }

            // After handling room change
            if (!isFromLoader && isGoldenDeathOrDebugTeleport && IsInFgrMode) {
                EndRun();
                if (IsInFirstRoom) {
                    StartNewRun();
                }
                SaveChapterStats(); //Necessary to update stats after changing run state
            }

            if (isFromLoader) {
                Log("Adding overlay!");
                IngameOverlay = new TextOverlay();
                level.Add(IngameOverlay);

                SummaryOverlay = new SummaryHud();
                level.Add(SummaryOverlay);
                
                GraphOverlay = new GraphOverlay();
                level.Add(GraphOverlay);
            }
        }

        private void LevelOnEnd(On.Celeste.Level.orig_End orig, Level self) {
            orig(self);
            Log($"Level ended.");
        }
        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            Log($"mode={mode}, snow={snow}");
            if (mode == LevelExit.Mode.Restart) {
                DidRestart = true;
                if (IsInGoldenRun 
                    && ModSettings.TrackingRestartChapterCountsForGoldenDeath
                    && SettingsTrackGoldens) {
                    CurrentChapterStats?.AddGoldenBerryDeath();
                    EndRun(died:true);
                }
            } else if (mode == LevelExit.Mode.GoldenBerryRestart) {
                DidRestart = true;
                Log($"GoldenBerryRestart -> PlayerIsHoldingGolden: {IsInGoldenRun}");

                if (SettingsTrackGoldens && IsInGoldenRun) { //Only count golden berry deaths when enabled
                    CurrentChapterStats?.AddGoldenBerryDeath();
                    EndRun(died:true);
                }
            }

            // Calling it again is safe, as it cannot double-end a run.
            if (!IsInFgrMode) {
                EndRun();
            }

            if (DoRecordPath) { //Abort path recording when exiting level
                AbortPathRecording = true;
                DoRecordPath = false;
                ModSettings.RecordPath = false;
            }
            
            SaveChapterStats();
        }

        private void Level_OnComplete(Level level) {
            if (CurrentChapterStats == null) return;
            Log($"Incrementing {CurrentChapterStats.CurrentRoom.DebugRoomName}");
            if(SettingsTrackAttempts)
                CurrentChapterStats.AddAttempt(true);
            CurrentChapterStats.ModState.ChapterCompleted = true;
            
            // Auto disable path recording when completing level
            if (DoRecordPath) { 
                DoRecordPath = false;
                ModSettings.RecordPath = false;
            }
            
            // End the run. If in fgr and in last room, count as win.
            if (IsInFgrMode) {
                if (IsInOrPastFinal) {
                    EndRun(won:true);
                }
            } else {
                EndRun();
            }
            
            
            SaveChapterStats();
        }

        private void Level_Begin(On.Celeste.Level.orig_Begin orig, Level level) {
            Log($"Calling ChangeChapter with 'level.Session'");
            ChangeChapter(level.Session);
            orig(level);
        }
        
        private void Level_OnTransitionTo(Level level, LevelData levelDataNext, Vector2 direction) {
            if (levelDataNext == null) return;
            
            if (levelDataNext.HasCheckpoint) {
                LastRoomWithCheckpoint = levelDataNext.Name;
            }

            string roomName = GetRoomName(levelDataNext.Name, IsInFgrMode, ActualChapterMetaInfo.ChapterUID);
            Log($"levelData.Name->{roomName}");

            if (CurrentRoomName != null && roomName != CurrentRoomName) {
                SetNewRoom(roomName);
            }
        }

        private void Player_OnDie(Player player) {
            TouchedBerries.Clear();
            
            Log($"Player died. (holdingGolden: {IsInGoldenRun})");
            if (CurrentRoomCompletedResetOnDeath) {
                CurrentRoomCompleted = false;
            }

            if (SettingsTrackAttempts)
                CurrentChapterStats?.AddAttempt(false);

            if(CurrentChapterStats != null)
                CurrentChapterStats.CurrentRoom.DeathsInCurrentRun++;

            if (IsInGoldenRun && IsInFgrMode) {
                CurrentChapterStats?.AddGoldenBerryDeath();
                EndRun(died:true);

                if (IsInFirstRoom) {
                    StartNewRun();
                }
            }

            SaveChapterStats();
        }

        private void Level_UpdateTime(On.Celeste.Level.orig_UpdateTime orig, Level self) {
            orig(self);
            
            if (CurrentChapterStats == null) return;
            if (CurrentChapterStats.CurrentRoom == null) return;

            //Stolen from Level.UpdateTime()
            if (self.InCredits || self.TimerStopped || self.Completed || !self.TimerStarted) {
                return;
            }

            long ticks = TimeSpan.FromSeconds(Engine.RawDeltaTime).Ticks;
            CurrentChapterStats.CurrentRoom.TimeSpentInRoom += ticks;
            if (IsInGoldenRun) {
                CurrentChapterStats.CurrentRoom.TimeSpentInRoomInRuns += ticks;
            } else {
                //Track first playthrough (FILE DEPENDENT)
                AreaModeStats stats = GetCurrentAreaModeStats();
                if (stats != null && !stats.Completed) {
                    CurrentChapterStats.CurrentRoom.TimeSpentInRoomFirstPlaythrough += ticks;
                }
            }
        }

        #endregion

        #region Custom Hooks
        private void HookCustom() {
            Events.Events.OnResetRun += Events_OnResetRun;
            Events.Events.OnRunStarted += EventsOnRunStarted;
            Events.Events.OnRunEnded += EventsOnRunEnded;
            Events.Events.OnChangedRoom += Events_OnChangedRoom;
        }

        private void UnHookCustom() {
            Events.Events.OnResetRun -= Events_OnResetRun;
            Events.Events.OnRunStarted -= EventsOnRunStarted;
            Events.Events.OnRunEnded -= EventsOnRunEnded;
            Events.Events.OnChangedRoom -= Events_OnChangedRoom;
        }

        private void Events_OnResetRun() {
            //Track previously entered chapters
            if (LastVisitedChapters.Any(t => t.Item1.SegmentStats[0].ChapterUID == CurrentChapterUID)) {
                LastVisitedChapters.RemoveAll(t => t.Item1.SegmentStats[0].ChapterUID == CurrentChapterUID);
            }
            LastVisitedChapters.Add(Tuple.Create(CurrentChapterStatsList, CurrentChapterPathSegmentList));
            if (LastVisitedChapters.Count > MAX_LAST_VISITED_CHAPTERS) {
                LastVisitedChapters.RemoveAt(0);
            }

            //Reset pb event flags
            HasTriggeredPbEvent = false;
            HasTriggeredAfterPbEvent = false;
        }

        private void EventsOnRunStarted() {

        }
        private void EventsOnRunEnded(bool died, bool won) {
            ChokeRateStat.ChokeRateData = null; //Reset caching
        }
        private void Events_OnChangedRoom(string roomName, bool isPreviousRoom) {
            if (DoRecordPath) {
                PathRec.AddRoom(roomName);
            }
        }
        #endregion

        #region State Management

        /// <summary>
        /// Starts a new run.
        /// </summary>
        public void StartNewRun() {
            if (IsInGoldenRun) return;
            IsInGoldenRun = true;
            Log($"Starting new run");
            Events.Events.InvokeRunStarted();
        }

        /// <summary>
        /// Ends the current run. Invokes necessary events and resets states.
        /// <param name="died">Whether the run ended due to death.</param>
        /// <param name="won">Whether the run ended due to winning.</param>
        /// </summary>
        public void EndRun(bool died = false, bool won = false) {
            if (!IsInGoldenRun) return; // Only invoke events if we were actually in a run
            IsInGoldenRun = false;
            Log($"Ending run | died: {died}, won: {won}");
            Events.Events.InvokeRunEnded(died, won);
        }

        private void ChangeChapter(Session session) {
            // Firstly, if we arent in FGR mode, end the run
            bool isInFgrMode = IsInFgrMode;
            if (!isInFgrMode) {
                EndRun();
            }
            
            // Its the same session if you teleport within a chapter through external means (debug map, LevelLoader when passing the current session)
            bool isLastSession = LastSession == session;
            Log($"Called chapter change | isLastSession: {isLastSession}");
            
            ChapterMetaInfo chapterInfo = new ChapterMetaInfo(session);

            Log($"Level->{session.Level}, chapterInfo->'{chapterInfo}'");

            ActualChapterMetaInfo = chapterInfo;
            CurrentChapterUID = chapterInfo.ChapterUID;

            PreviousRoomName = null;
            CurrentRoomName = GetRoomName(session.Level, IsInFgrMode, chapterInfo.ChapterUID);

            // Resolve active path and stats. Respects FGR mode.
            var statsPathInfo = ResolveActiveChapterStatsListPath();
            CurrentChapterStatsList = GetChapterStatsList(statsPathInfo.Item1, statsPathInfo.Item2);
            
            var pathPathInfo = ResolveActivePathSegmentListPath();
            PathSegmentList psl = pathPathInfo == null ? null : GetPathSegmentList(pathPathInfo.Item1, pathPathInfo.Item2);
            CurrentChapterPathSegmentList = psl;
            
            // Add meta info to path and stats
            if (psl != null) {
                FixChapterPathInfo(psl.CurrentPath, chapterInfo);
            }
            CurrentChapterStats.SetChapterMetaInfo(chapterInfo);

            //fix for SpeedrunTool savestate inconsistency
            TouchedBerries.Clear();
            
            //Reset caching of choke rate data
            ChokeRateStat.ChokeRateData = null;
            
            //Cause initial stats calculation
            SetNewRoom(CurrentRoomName, false);

            if (!isLastSession) {
                if (LastSession != null && isInFgrMode && IsInGoldenRun && !IsInFirstRoom && ModSettings.FgrContinuousSessionTimer) {
                    session.Time = LastSession.Time;
                }
                CheckResetSessionStats();
                CurrentChapterStats.ResetCurrentRun();
                Events.Events.InvokeResetRun();
            }
            
            // If we are in fgr mode, check if we are in the first room of the path. If yes, start a new run
            if (isInFgrMode && IsInFirstRoom) {
                StartNewRun();
            }
            
            //Another stats calculation, accounting for reset session
            SaveChapterStats();

            if (session.LevelData != null && session.LevelData.HasCheckpoint) {
                LastRoomWithCheckpoint = CurrentRoomName;
            } else {
                LastRoomWithCheckpoint = null;
            }
            LastSession = session;
        }

        public void CheckResetSessionStats() {
            PathSegmentList psl = CurrentChapterPathSegmentList;
            if (psl?.CurrentPath == null) return;
            
            string pathKey = GetPathKey(psl);
            bool firstEntry = PathsThisSession.Add(pathKey);
            if (!firstEntry) return;

            IsInGoldenRun = IsInFgrMode && IsInFirstRoom; //Reset flag between sessions.
            CurrentChapterStats.ResetCurrentSession(CurrentChapterPath);
            Events.Events.InvokeResetSession();
        }

        public string GetPathKey(PathSegmentList psl) {
            if (psl == null) return null;
            string uid = psl.CurrentPath?.ChapterUID ?? CurrentChapterUID;
            int index = psl.SelectedIndex;
            if (IsInFgrMode) {
                uid = "FGR";
                index = ModSettings.SelectedFgr;
            }
            return $"{uid}|{index}";
        }

        public void FixChapterPathInfo(PathInfo pathInfo, ChapterMetaInfo chapterInfo = null) {
            if (pathInfo == null) return;
            
            pathInfo.SegmentList = CurrentChapterPathSegmentList;
            pathInfo.SetCheckpointRefs();
            if (chapterInfo != null && (pathInfo.ChapterName == null
                                        || pathInfo.ChapterUID == null
                                        || (!IsInFgrMode && ChapterMetaInfo.GetChapterUIDForPath(pathInfo.ChapterUID) == pathInfo.ChapterUID))) {
                pathInfo.SetChapterMetaInfo(chapterInfo);
                SaveActivePath();
            }
        }
        public void SetCurrentChapterPath(PathInfo path, ChapterMetaInfo chapterInfo = null) {
            if (CurrentChapterPathSegmentList == null) {
                CurrentChapterPathSegmentList = PathSegmentList.Create();
            }

            CurrentChapterPath = path;
            FixChapterPathInfo(path, chapterInfo);
        }

        public void SetCurrentChapterPathSegment(int segmentIndex) {
            if (CurrentChapterPathSegmentList == null) return;
            CurrentChapterPathSegmentList.SelectedIndex = segmentIndex;

            ChapterMetaInfo chapterInfo = null;
            if (Engine.Scene is Level level) {
                chapterInfo = new ChapterMetaInfo(level.Session);
            }
            FixChapterPathInfo(CurrentChapterPath, chapterInfo);
            
            SaveActivePath();
            if (CurrentChapterPath != null) {
                StatsManager.AggregateStatsPassOnce(CurrentChapterPath);
                CheckResetSessionStats();
            }
            SetNewRoom(CurrentRoomName, false);
        }

        public string ResolveRoomNameInActiveChapter(string roomName) {
            if (!IsInFgrMode) return roomName;
            return GetRoomName(roomName, true, ActualChapterMetaInfo.ChapterUID);
        }
        public static string GetRoomName(string roomName, bool isFgrMode, string uid) {
            if (!isFgrMode) return roomName;
            return $"{uid}:{roomName}";
        }
        public static string InverseRoomName(string roomName) {
            if (!roomName.Contains(":")) return roomName;
            return roomName.Split(':')[1];
        }

        public string ResolveGroupedRoomName(string roomName) {
            if (CurrentChapterPath == null) {
                return roomName;
            }

            //Loop through path and see if any room on the path has the roomName as grouped room
            //If yes, return that room
            //If no, return roomName
            foreach (CheckpointInfo cpInfo in CurrentChapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.GroupedRooms != null && rInfo.GroupedRooms.Contains(roomName)) {
                        return rInfo.DebugRoomName;
                    }
                }
            }

            return roomName;
        }

        public void SetNewRoom(string newRoomName, bool countSuccess=true) {
            if (CurrentChapterStats == null) return;
            
            CurrentChapterStats.ModState.ChapterCompleted = false;

            //Resolve grouped room name
            string nameBefore = newRoomName;
            newRoomName = ResolveGroupedRoomName(newRoomName);
            if (nameBefore != newRoomName) {
                Log($"Resolved grouped room name '{nameBefore}' to '{newRoomName}'");
            } else {
                Log($"Room name '{nameBefore}' was not grouped, returned it raw");
            }

            //If the room is to be ignored, don't update anything
            if (CurrentChapterPath != null && CurrentChapterPath.IgnoredRooms.Contains(newRoomName)) {
                Log($"Entered ignored room '{newRoomName}', not updating state!");
                return;
            }

            //Don't complete if entering previous room and current room was not completed
            if (PreviousRoomName == newRoomName && !CurrentRoomCompleted) {
                Log($"Entered previous room '{PreviousRoomName}'");
                PreviousRoomName = CurrentRoomName;
                CurrentRoomName = newRoomName;
                CurrentChapterStats?.SetCurrentRoom(newRoomName);
                SaveChapterStats();
                Events.Events.InvokeChangedRoom(newRoomName, true);
                return;
            }

            //Transitioned to own room, probably due to teleport or entering a grouped room
            if (CurrentRoomName == newRoomName) {
                Log($"Entered own room '{newRoomName}', not updating state!");
                CurrentChapterStats.SetCurrentRoom(newRoomName);
                SaveChapterStats();
                return;
            }

            Log($"Entered new room '{newRoomName}' | Is in run: '{IsInGoldenRun}'");

            PreviousRoomName = CurrentRoomName;
            CurrentRoomName = newRoomName;
            CurrentRoomCompleted = false;

            if (countSuccess && !ModSettings.PauseDeathTracking && (!ModSettings.TrackingOnlyWithGoldenBerry || IsInGoldenRun)) {
                CurrentChapterStats.AddAttempt(true);
            }
            
            // Set current room references
            CurrentChapterStats.SetCurrentRoom(newRoomName);
            if (CurrentChapterPath != null) {
                CurrentChapterPath.SetCurrentRoom(newRoomName);
            }

            // When in FGR mode, consider the run to start when you touched the first room.
            if (IsInFirstRoom && IsInFgrMode) {
                StartNewRun();
            }
            
            SaveChapterStats();
            CheckPaceEvents(newRoomName);
        }

        private void CheckPaceEvents(string newRoomName) {
            //PB state tracking
            if (CurrentChapterPath != null && CurrentChapterPath.CurrentRoom != null && IsInGoldenRun) {
                RoomInfo currentRoom = CurrentChapterPath.CurrentRoom;
                RoomInfo pbRoom = PersonalBestStat.GetFurthestDeathRoom(CurrentChapterPath, CurrentChapterStats);
                if (pbRoom != null && CurrentChapterStats.GoldenCollectedCount == 0) { //Don't call events if no PB or if golden has been collected
                    if (!HasTriggeredPbEvent && currentRoom.DebugRoomName == pbRoom.DebugRoomName) {
                        HasTriggeredPbEvent = true;
                        Events.Events.InvokeEnteredPbRoomWithGolden();
                    } else if (HasTriggeredPbEvent && !HasTriggeredAfterPbEvent && currentRoom.RoomNumberInChapter > pbRoom.RoomNumberInChapter) {
                        HasTriggeredAfterPbEvent = true;
                        Events.Events.InvokeExitedPbRoomWithGolden();
                    }
                }
            }
            
            Events.Events.InvokeChangedRoom(newRoomName, false);
        }

        private void SetRoomCompleted(bool resetOnDeath=false) {
            CurrentRoomCompleted = true;
            CurrentRoomCompletedResetOnDeath = resetOnDeath;
        }

        private bool PlayerIsHoldingGoldenBerry(Player player) {
            if (player == null || player.Leader == null || player.Leader.Followers == null || player.Leader.Followers.Count == 0) {
                return false;
            }
            
            return player.Leader.Followers.Any((f) => {
                if (f.Entity.GetType().Name == "PlatinumBerry") {
                    return true;
                } else if (f.Entity.GetType().Name == "SpeedBerry") {
                    return false;
                }
                
                if (!(f.Entity is Strawberry berry)) {
                    return false;
                }

                return berry.Golden && !berry.Winged;
            });
        }
        
        #region Speedrun Tool Save States
        public void SpeedrunToolSaveState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Type type = GetType();
            if (!savedvalues.ContainsKey(type)) {
                savedvalues.Add(type, new Dictionary<string, object>());
                savedvalues[type].Add(nameof(PreviousRoomName), PreviousRoomName);
                savedvalues[type].Add(nameof(CurrentRoomName), CurrentRoomName);
                savedvalues[type].Add(nameof(CurrentRoomCompleted), CurrentRoomCompleted);
                savedvalues[type].Add(nameof(CurrentRoomCompletedResetOnDeath), CurrentRoomCompletedResetOnDeath);
            } else {
                savedvalues[type][nameof(PreviousRoomName)] = PreviousRoomName;
                savedvalues[type][nameof(CurrentRoomName)] = CurrentRoomName;
                savedvalues[type][nameof(CurrentRoomCompleted)] = CurrentRoomCompleted;
                savedvalues[type][nameof(CurrentRoomCompletedResetOnDeath)] = CurrentRoomCompletedResetOnDeath;
            }

            SpeedrunToolSaveStateRoomName = CurrentRoomName;
            SaveChapterStats();
        }

        public void SpeedrunToolLoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Type type = GetType();
            if (!savedvalues.ContainsKey(type)) {
                Log("Trying to load state without prior saving a state...");
                return;
            }

            //Register the death if setting is enabled
            if (ModSettings.TrackingSaveStateCountsForGoldenDeath && CurrentChapterStats != null) {
                if (IsInGoldenRun && SettingsTrackGoldens) {
                    CurrentChapterStats?.AddGoldenBerryDeath();
                    EndRun(died:true);
                } else {
                    EndRun();
                }
            } else {
                EndRun();
            }

            IngameOverlay = level.Tracker.GetEntity<TextOverlay>();
            SummaryOverlay = level.Tracker.GetEntity<SummaryHud>();
            GraphOverlay = level.Tracker.GetEntity<GraphOverlay>();

            PreviousRoomName = (string) savedvalues[type][nameof(PreviousRoomName)];
            CurrentRoomName = (string) savedvalues[type][nameof(CurrentRoomName)];
            CurrentRoomCompleted = (bool) savedvalues[type][nameof(CurrentRoomCompleted)];
            CurrentRoomCompletedResetOnDeath = (bool) savedvalues[type][nameof(CurrentRoomCompletedResetOnDeath)];

            if (CurrentChapterStats != null) CurrentChapterStats.SetCurrentRoom(CurrentRoomName);
            SaveChapterStats();

            if (PhysicsLogger.Settings.SegmentOnLoadState) {
                PhysicsLog.SegmentLog(true);
            }
        }

        public void SpeedrunToolClearState() {
            SpeedrunToolSaveStateRoomName = null;
            if (CurrentChapterPath != null) {
                CurrentChapterPath.SpeedrunToolSaveStateRoom = null;
            }
            SaveChapterStats();
        }

        #endregion
        
        #endregion

        #region Data Import/Export
        
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
        
        /// <summary>
        /// Checks the root folder for the mod data, creates it if it doesn't exist.
        /// </summary>
        public void CheckRootFolder() {
            string dataRootFolder = ModSettings.DataRootFolderLocation;
            dataRootFolder = dataRootFolder == null ? BaseFolderPath : Path.Combine(dataRootFolder, "ConsistencyTracker");

            if (!Directory.Exists(dataRootFolder)) {
                try { 
                    Directory.CreateDirectory(dataRootFolder);
                } catch (Exception) {
                    CheckFolderExists(BaseFolderPath);
                    dataRootFolder = BaseFolderPath;
                }
            }

            BaseFolderPath = dataRootFolder;
        }

        /// <summary>
        /// Resolves the active path segment list, depending on whether FGR mode is active or not.
        /// </summary>
        /// <returns>The folder and fileName of the active path segment list.</returns>
        public Tuple<string, string> ResolveActivePathSegmentListPath() {
            if (!IsInFgrMode) {
                return Tuple.Create(PathsFolder, CurrentChapterUIDForPath);
            }
            string pathFileName = $"fgr_{ModSettings.SelectedFgr}_path";
            return Tuple.Create(FgrFolder, pathFileName);
        }
        
        /// <summary>
        /// Core function to load a path segment list from file. Will create a new one if neither JSON nor TXT file
        /// is found. Also handles conversion from old formats to new format.
        /// </summary>
        /// <param name="folder">The subfolder to look in.</param>
        /// <param name="pathName">The base name of the path file (without extension).</param>
        /// <returns>>The loaded path segment list, or null if not found or failed to parse.</returns>
        public PathSegmentList GetPathSegmentList(string folder = null, string pathName = null) {
            Log($"Fetching path info for chapter '{pathName}'");

            string pathTxt = GetPathToFile(folder, $"{pathName}.txt");
            string pathJson = GetPathToFile(folder, $"{pathName}.json");
            Log($"\tSearching for path '{pathJson}' or .txt equiv", true);

            if (!File.Exists(pathJson) && !File.Exists(pathTxt)) {
                Log($"\tDidn't find file at '{pathJson}', returned null.", true);
                return null;
            }
            
            
            string content = null;
            bool readAsTxt = false;
            if (File.Exists(pathJson)) {
                content = File.ReadAllText(pathJson);
            } else if (File.Exists(pathTxt)) {
                content = File.ReadAllText(pathTxt);
                readAsTxt = true;
            }

            string logExt = readAsTxt ? "txt" : "json";
            Log($"\tFound '.{logExt}' version of file, parsing...", true);

            try {
                Log($"Trying to parse PathSegmentList...");
                PathSegmentList psl = JsonConvert.DeserializeObject<PathSegmentList>(content);
                Log($"Successfully parsed PathSegmentList: Segments.Count -> {psl.Segments.Count}", isFollowup: true);
                return psl;
            } catch (Exception ex) {
                Log($"Couldn't parse PathSegmentList. Exception: {ex.Message}", isFollowup: true);
            }

            
            PathInfo oldPathFormat = null;
            //[Try 1] New file format: JSON
            try {
                PathInfo pathInfo = JsonConvert.DeserializeObject<PathInfo>(content);
                if (readAsTxt) { //Move file to json format
                    File.Move(pathTxt, pathJson);
                }
                oldPathFormat = pathInfo;
            } catch (Exception) {
                Log($"\tCouldn't read path info as JSON, trying old path format...", true);
            }

            if (oldPathFormat == null) {
                //[Try 2] Old file format: selfmade text format
                try {
                    PathInfo parsedOldFormat = PathInfo.ParseString(content);
                    Log($"\tSaving path for map '{pathName}' in new format!", true);
                    oldPathFormat = parsedOldFormat;
                } catch (Exception) {
                    Log($"\tCouldn't read old path info. Old path info content:\n{content}", true);
                }
            }

            if (oldPathFormat == null) return null;
            PathSegmentList pathList = PathSegmentList.Create();
            pathList.Segments[0].Path = oldPathFormat;

            SavePathToFile(pathList, pathName, folder); //Save in new format (json)
            return pathList;
        }

        /// <summary>
        /// Moves a file to a safe backup location by appending .bak.# to the filename. Triggered when a stats file
        /// corruption is detected, but a backup file exists.
        /// </summary>
        /// <param name="path">The path to move.</param>
        public void MoveFileToSaveLocation(string path) {
            string destPath;
            int backupNum = 1;
            
            while (true) {
                destPath = path + ".bak."+backupNum;
                if (!File.Exists(destPath)) {
                    break;
                }
                backupNum++;
            }
            
            Log($"\tMoving backup stats file to safe location: "+destPath, true);
            File.Move(path, destPath);
        }

        /// <summary>
        /// Resolves the active chapter stats list, depending on whether FGR mode is active or not.
        /// </summary>
        /// <returns></returns>
        public Tuple<string, string> ResolveActiveChapterStatsListPath() {
            if (!IsInFgrMode) {
                return Tuple.Create(StatsFolder, CurrentChapterUIDForPath);
            }
            //In FGR mode, the folder is FgrFolder and the name is based on the selected fgr index
            string statsFileName = $"fgr_{ModSettings.SelectedFgr}_stats";
            return Tuple.Create(FgrFolder, statsFileName);
        }
        
        /// <summary>
        /// Core function to load a chapter stats list from file. Will create a new one if neither JSON nor TXT file
        /// is found. Also handles conversion from old formats to new format.
        /// </summary>
        /// <param name="folder">The subfolder to look in.</param>
        /// <param name="fileBaseName">The base name of the stats file (without extension).</param>
        /// <returns>>The loaded chapter stats list.</returns>
        /// <exception cref="Exception">Thrown when the stats file couldn't be parsed.</exception>
        public ChapterStatsList GetChapterStatsList(string folder, string fileBaseName) {
            string pathTxt = GetPathToFile(folder, $"{fileBaseName}.txt");
            string pathJson = GetPathToFile(folder, $"{fileBaseName}.json");
            string backupJson = GetPathToFile(folder, $"{fileBaseName}_backup.json");

            Log($"fileBaseName: '{fileBaseName}', ChaptersThisSession: '{string.Join(", ", PathsThisSession)}'");


            ChapterStatsList toRet;
            if (!File.Exists(pathTxt) && !File.Exists(pathJson)) { //Create new
                if (File.Exists(backupJson)) {
                    MoveFileToSaveLocation(backupJson);
                }
                toRet = new ChapterStatsList();
                toRet.GetStats(0).SetCurrentRoom(CurrentRoomName);
                return toRet;
            }

            string content = null;
            bool readAsTxt = false;
            if (File.Exists(pathJson)) {
                content = File.ReadAllText(pathJson);
            } else if (File.Exists(pathTxt)) {
                content = File.ReadAllText(pathTxt);
                readAsTxt = true;
            }

            try {
                toRet = JsonConvert.DeserializeObject<ChapterStatsList>(content);
                if (toRet == null) {
                    throw new Exception();
                }
                return toRet;
            } catch (Exception) {
                Log($"\tCouldn't read chapter stats list, trying older stats formats...", true);
                if (File.Exists(backupJson)) {
                    MoveFileToSaveLocation(backupJson);
                }
            }

            ChapterStats chapterStats = null;
            //[Try 1] New file format: JSON
            try {
                chapterStats = JsonConvert.DeserializeObject<ChapterStats>(content);
                if (chapterStats == null) {
                    throw new Exception();
                }
            } catch (Exception) {
                Log($"\tCouldn't read chapter stats as JSON, trying old stats format...", true);
                if (File.Exists(backupJson)) {
                    MoveFileToSaveLocation(backupJson);
                }
            }

            if (chapterStats == null) {
                //[Try 2] Old file format: selfmade text format
                try {
                    chapterStats = ChapterStats.ParseString(content);
                    if (chapterStats == null) {
                        throw new Exception();
                    }
                    Log($"\tSaving chapter stats for map '{CurrentChapterUID}' in new format!", true);
                } catch (Exception) {
                    Log($"\tCouldn't read old chapter stats, created new ChapterStats. Old chapter stats content:\n{content}", true);
                    chapterStats = new ChapterStats();
                    chapterStats.SetCurrentRoom(CurrentRoomName);
                }
            }

            if (readAsTxt) { //Try to move file from TXT to JSON path 
                File.Move(pathTxt, pathJson);
            }

            //When a stats file was parsed from an old format, the path segment is always 0 (default)
            toRet = new ChapterStatsList();
            toRet.SetStats(0, chapterStats);

            return toRet;
        }

        /// <summary>
        /// Core function to save the currently loaded chapter stats to file.
        /// </summary>
        public void SaveChapterStats() {
            Events.Events.InvokeBeforeSavingStats();
            
            if (CurrentChapterStatsList == null) {
                Log($"Aborting saving chapter stats as '{nameof(CurrentChapterStatsList)}' is null");
                return;
            }
            if (CurrentChapterStats == null) {
                Log($"Aborting saving chapter stats as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            if (CurrentChapterStats.ChapterName == null) {
                if (Engine.Scene is Level level && level.Session != null) {
                    ChapterMetaInfo chapterInfo = new ChapterMetaInfo(level.Session);
                    CurrentChapterStats.SetChapterMetaInfo(chapterInfo);
                    Log($"Fixed missing meta info on '{nameof(CurrentChapterStats)}'");
                } else {
                    Log($"Aborting saving chapter stats as '{nameof(CurrentChapterStats)}' doesn't have meta info set.");
                    return;
                }
            }

            CurrentUpdateFrame++;

            CurrentChapterStats.ModState.PlayerIsHoldingGolden = IsInGoldenRun;
            CurrentChapterStats.ModState.GoldenDone = IsInGoldenRun && CurrentChapterStats.ModState.ChapterCompleted;

            CurrentChapterStats.ModState.DeathTrackingPaused = ModSettings.PauseDeathTracking;
            CurrentChapterStats.ModState.RecordingPath = ModSettings.RecordPath || DebugMapUtil.IsRecording;
            CurrentChapterStats.ModState.OverlayVersion = VersionsCurrent.Overlay;
            CurrentChapterStats.ModState.ModVersion = VersionsNewest.Mod;
            CurrentChapterStats.ModState.ChapterHasPath = CurrentChapterPath != null;

            AreaModeStats modeStats = GetCurrentAreaModeStats();
            if (modeStats != null) {
                CurrentChapterStats.GameData.TotalTime = modeStats.TimePlayed;
                CurrentChapterStats.GameData.TotalDeaths = modeStats.Deaths;
                CurrentChapterStats.GameData.Completed = modeStats.Completed;
                CurrentChapterStats.GameData.FullClear = modeStats.FullClear;
            }

            var statsPathInfo = ResolveActiveChapterStatsListPath();
            string path = GetPathToFile(statsPathInfo.Item1, $"{statsPathInfo.Item2}.json");
            string backupPath = GetPathToFile(statsPathInfo.Item1, $"{statsPathInfo.Item2}_backup.json");
            
            // File.WriteAllText(backupPath, JsonConvert.SerializeObject(CurrentChapterStatsList, Formatting.Indented));
            using (FileStream fs = new FileStream(backupPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (StreamWriter writer = new StreamWriter(fs))
            {
                string json = JsonConvert.SerializeObject(CurrentChapterStatsList, Formatting.Indented);
                writer.Write(json);
                writer.Flush();
            }

            //Delete actual file
            if (File.Exists(path)) {
                File.Delete(path);
            }
            //Move backup to actual file
            File.Copy(backupPath, path);

            StatsManager.OutputFormats(CurrentChapterPath, CurrentChapterStats);
            Events.Events.InvokeAfterSavingStats();
        }

        public void CreateChapterSummary(int attemptCount) {
            Log($"Attempting to create tracker summary, attemptCount = '{attemptCount}'");
            string outPath = GetPathToFile(SummariesFolder, $"{CurrentChapterUIDForPath}.txt");
            CurrentChapterStats?.OutputSummary(outPath, CurrentChapterPath, attemptCount);
        }

        #endregion

        #region Stats Data Control

        public void WipeChapterData() {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping chapter data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Wiping death data for chapter '{CurrentChapterUID}'");

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

        public void RemoveRoomGoldenBerryDeaths(bool removeOne = false) {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping room golden berry deaths as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            if (removeOne) {
                Log($"Removing 1 golden death for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");
                CurrentChapterStats.CurrentRoom.GoldenBerryDeaths = Math.Max(0, CurrentChapterStats.CurrentRoom.GoldenBerryDeaths - 1);
                CurrentChapterStats.CurrentRoom.GoldenBerryDeathsSession = Math.Max(0, CurrentChapterStats.CurrentRoom.GoldenBerryDeathsSession - 1);
            } else {
                Log($"Wiping golden berry death data for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");
                CurrentChapterStats.CurrentRoom.GoldenBerryDeaths = 0;
                CurrentChapterStats.CurrentRoom.GoldenBerryDeathsSession = 0;
            }

            //Reset cached choke rate data for graph
            ChokeRateStat.ChokeRateData = null;
            SaveChapterStats();
        }
        public void WipeChapterGoldenBerryDeaths() {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping chapter golden berry deaths as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Wiping golden berry death data for chapter '{CurrentChapterUID}'");

            foreach (string debugName in CurrentChapterStats.Rooms.Keys) {
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeaths = 0;
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeathsSession = 0;
            }

            SaveChapterStats();
        }
        public void WipeChapterGoldenBerryCollects() {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping chapter golden berry collections as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Wiping golden berry collection data for chapter '{CurrentChapterUID}'");
            CurrentChapterStats.GoldenCollectedCount = 0;
            CurrentChapterStats.GoldenCollectedCountSession = 0;

            SaveChapterStats();
        }

        public void WipeRoomData() {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping room data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            Log($"Wiping room data for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.CurrentRoom.PreviousAttempts.Clear();
            SaveChapterStats();
        }

        public void RemoveLastDeathStreak() {
            if (CurrentChapterStats == null) {
                Log($"Aborting removing death streak as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            Log($"Removing death streak for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            while (CurrentChapterStats.CurrentRoom.PreviousAttempts.Count > 0 && CurrentChapterStats.CurrentRoom.LastAttempt == false) {
                CurrentChapterStats.CurrentRoom.RemoveLastAttempt();
            }

            SaveChapterStats();
        }

        public void RemoveLastAttempt() {
            if (CurrentChapterStats == null) {
                Log($"Aborting removing death streak as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            Log($"Removing last attempt for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.CurrentRoom.RemoveLastAttempt();
            SaveChapterStats();
        }
        public void AddRoomAttempt(bool success) {
            if (CurrentChapterStats == null) {
                Log($"Aborting adding room attempt ({success}) as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Adding room attempt ({success}) to '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.AddAttempt(success);

            SaveChapterStats();
        }

        #endregion

        #region Path Management

        public void ChangedSelectedFgr() {
            if (SaveData.Instance.CurrentSession != null) {
                ChangeChapter(SaveData.Instance.CurrentSession);
            }
        }
        
        public void SaveRecordedRoomPath() {
            Log($"Saving recorded path...");
            if (PathRec.TotalRecordedRooms <= 1) {
                Log($"Path is too short to save. ({PathRec.TotalRecordedRooms} rooms)", isFollowup: true);
                return;
            }
            if (AbortPathRecording) {
                AbortPathRecording = false;
                Log($"AbortPathRecording is set, not saving path.", isFollowup:true);
                return;
            }

            PathInfo path = PathRec.ToPathInfo();

            ChapterMetaInfo chapterInfo = null;
            if (Engine.Scene is Level level && level.Session != null) {
                chapterInfo = new ChapterMetaInfo(level.Session);
            }

            DisabledInRoomName = CurrentRoomName;
            SetCurrentChapterPath(path, chapterInfo);
            Log($"Recorded path:\n{JsonConvert.SerializeObject(CurrentChapterPath)}", isFollowup: true);
            SaveActivePath();
            
            SaveChapterStats();//Output stats with updated path
        }

        public void SaveActivePath() {
            var pathPathInfo = ResolveActivePathSegmentListPath();
            SavePathToFile(CurrentChapterPathSegmentList, pathPathInfo.Item2, pathPathInfo.Item1);
        }
        public void SavePathToFile(PathSegmentList pathList = null, string pathName = null, string folder = null) {
            if (pathList == null) {
                pathList = CurrentChapterPathSegmentList;
            }
            if (pathName == null) {
                pathName = CurrentChapterUIDForPath;
            }
            if (folder == null) {
                folder = PathsFolder;
            }

            string outPath = GetPathToFile(folder, $"{pathName}.json");
            File.WriteAllText(outPath, JsonConvert.SerializeObject(pathList, Formatting.Indented));
            Log($"Wrote path data to '{outPath}'");
        }
        
        public bool SetCurrentChapterPathSegmentName(string name) {
            if (string.IsNullOrEmpty(name) || CurrentChapterPathSegmentList == null) return false;

            CurrentChapterPathSegmentList.CurrentSegment.Name = name;
            SaveActivePath();
            SaveChapterStats();
            return true;
        }
        public PathSegment AddCurrentChapterPathSegment() {
            if (CurrentChapterPathSegmentList == null) return null;

            PathSegment segment = new PathSegment() {
                Name = $"Segment {CurrentChapterPathSegmentList.Segments.Count + 1}",
                Path = null,
            };
            CurrentChapterPathSegmentList.Segments.Add(segment);
            SaveActivePath();
            return segment;
        }
        public bool DeleteCurrentChapterPathSegment() {
            if (CurrentChapterPathSegmentList == null) return false;
            int segmentIndex = CurrentChapterPathSegmentList.SelectedIndex;
            if (segmentIndex >= CurrentChapterPathSegmentList.Segments.Count || CurrentChapterPathSegmentList.Segments.Count <= 1) return false;
            
            CurrentChapterPathSegmentList.RemoveSegment(segmentIndex);
            CurrentChapterStatsList.RemoveSegment(segmentIndex);
            
            SaveActivePath();
            if (CurrentChapterPath != null) {
                StatsManager.AggregateStatsPassOnce(CurrentChapterPath);
            }
            SetNewRoom(CurrentRoomName, false);
            return true;
        }

        public void RemoveRoomFromChapterPath() {
            Log($"Removing room '{CurrentRoomName}' from path");
            
            if (CurrentChapterPath == null) {
                Log($"CurrentChapterPath was null");
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
                SaveActivePath();
                SaveChapterStats();
            } else {
                Log($"Could not find room '{CurrentRoomName}' in path");
            }
        }

        public void GroupRoomsOnChapterPath() {
            Log($"Grouping room '{CurrentRoomName}' with previous room on path");
            
            if (CurrentChapterPath == null) {
                Log($"CurrentChapterPath was null");
                return;
            }

            RoomInfo previousRoomR = null;

            CheckpointInfo currentRoomCp = null;
            RoomInfo currentRoomR = null;

            foreach (CheckpointInfo cpInfo in CurrentChapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.DebugRoomName == CurrentRoomName) {
                        currentRoomCp = cpInfo;
                        currentRoomR = rInfo;
                        break;
                    } else { 
                        previousRoomR = rInfo;
                    }
                }

                if (currentRoomR != null) break;
            }

            if (currentRoomR == null) {
                Log($"Could not find room '{CurrentRoomName}' in path");
                return;
            }
            if (previousRoomR == null) {
                Log($"Room '{CurrentRoomName}' doesn't have a previous room on the path");
                return;
            }

            currentRoomCp.Rooms.Remove(currentRoomR);
            if (previousRoomR.GroupedRooms == null) previousRoomR.GroupedRooms = new List<string>();
            previousRoomR.GroupedRooms.Add(currentRoomR.DebugRoomName);

            CurrentChapterPath.Stats = null; //Call a new aggregate stats pass to fix room numbering
            SetNewRoom(previousRoomR.DebugRoomName, false);
            
            SaveActivePath();
            SaveChapterStats();
        }

        public void UngroupRoomsOnChapterPath() {
            if (CurrentChapterPath == null) {
                Log($"CurrentChapterPath was null");
                return;
            }

            if (!(Engine.Scene is Level level)) return;
            string actualRoomName = ResolveRoomNameInActiveChapter(level.Session.Level);
            Log($"Ungrouping room '{actualRoomName}' from room '{CurrentRoomName}' on path");
            
            if (actualRoomName == CurrentRoomName) {
                Log($"Room '{CurrentRoomName}' isn't marked as a grouped room!");
                return;
            }
            if (CurrentChapterPath.IgnoredRooms.Contains(actualRoomName)) {
                Log($"Room '{actualRoomName}' is marked as ignored room and can't be grouped/ungrouped");
                return;
            }

            CheckpointInfo currentRoomCp = null;
            RoomInfo currentRoomR = null;

            foreach (CheckpointInfo cpInfo in CurrentChapterPath.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    if (rInfo.DebugRoomName == CurrentRoomName) {
                        currentRoomCp = cpInfo;
                        currentRoomR = rInfo;
                        break;
                    }
                }

                if (currentRoomR != null) break;
            }

            if (currentRoomR == null) {
                Log($"Could not find room '{CurrentRoomName}' in path");
                return;
            }

            currentRoomR.GroupedRooms.Remove(actualRoomName);

            RoomInfo newRoomInfo = new RoomInfo() { 
                DebugRoomName = actualRoomName,
                Checkpoint = currentRoomCp,
            };
            int indexOfCurrent = currentRoomCp.Rooms.IndexOf(currentRoomR);
            currentRoomCp.Rooms.Insert(indexOfCurrent+1, newRoomInfo);

            CurrentChapterPath.Stats = null; //Call a new aggregate stats pass to fix room numbering
            SetNewRoom(actualRoomName, false);

            SaveActivePath();
            SaveChapterStats();
        }

        public void ChangeRoomDifficultyWeight(int newDifficulty, RoomInfo targetRoom = null) {
            if (CurrentChapterPath == null) {
                Log($"CurrentChapterPath was null");
                return;
            }

            if (targetRoom == null) {
                targetRoom = CurrentChapterPath.CurrentRoom;
            }

            if (targetRoom == null) {
                Log($"Current room is not on path!");
                return;
            }
            
            Log($"Changing rooms difficulty for '{targetRoom.GetFormattedRoomName(ModSettings.LiveDataRoomNameDisplayType)}' to '{newDifficulty}'");

            bool applyToAllSegments = ModSettings.RoomDifficultyAllSegments;

            if (applyToAllSegments) {
                foreach (PathSegment segment in CurrentChapterPathSegmentList.Segments) {
                    if (segment.Path == null) continue;
                    foreach (CheckpointInfo cpInfo in segment.Path.Checkpoints) {
                        foreach (RoomInfo rInfo in cpInfo.Rooms) {
                            if (rInfo.DebugRoomName == targetRoom.DebugRoomName) {
                                rInfo.DifficultyWeight = newDifficulty;
                            }
                        }
                    }
                }
                Log($"Applied difficulty change to all segments");
            } else {
                CurrentChapterPath.CurrentRoom.DifficultyWeight = newDifficulty;
            }

            CurrentChapterPath.Stats = null; //Call a new aggregate stats pass to weights

            SaveActivePath();
            SaveChapterStats();
        }

        public void ChangeAllRoomDifficultyWeights(int newDifficulty) {
            if (CurrentChapterPath == null) {
                Log($"CurrentChapterPath was null");
                return;
            }
            
            Log($"Changing all rooms difficulties for the current chapter");

            bool applyToAllSegments = ModSettings.RoomDifficultyAllSegments;
            List<PathSegment> segments = new List<PathSegment>() { CurrentChapterPathSegmentList.CurrentSegment };

            if (applyToAllSegments) {
                foreach (PathSegment segment in CurrentChapterPathSegmentList.Segments) {
                    if (segment.Path == null) continue;
                    if (segments.Contains(segment)) continue;
                    segments.Add(segment);
                }
            }

            foreach (PathSegment segment in segments) {
                foreach (RoomInfo rInfo in segment.Path.WalkPath()) {
                    rInfo.DifficultyWeight = newDifficulty;
                }
            }

            CurrentChapterPath.Stats = null; //Call a new aggregate stats pass to weights
            //SetNewRoom(CurrentRoomName, false);

            SaveActivePath();
            SaveChapterStats();
        }

        public void SetCustomRoomName(string name, RoomInfo room = null) {
            if (CurrentChapterPath == null || name == null) return;
            
            if (room == null) {
                room = CurrentChapterPath.CurrentRoom;
            }
            if (room == null) return;
            
            name = name.Trim();
            if (string.IsNullOrEmpty(name)) name = null;
            
            string currentRoomName = room.DebugRoomName;
            int count = 0;
            if (ModSettings.CustomRoomNameAllSegments) { //Find all other rooms with same name in other segments 
                foreach (PathSegment segment in CurrentChapterPathSegmentList.Segments) {
                    if (segment.Path == null) continue;
                    foreach (CheckpointInfo cpInfo in segment.Path.Checkpoints) {
                        foreach (RoomInfo rInfo in cpInfo.Rooms) {
                            if (rInfo.DebugRoomName == currentRoomName) {
                                rInfo.CustomRoomName = name;
                                count++;
                            }
                        }
                    }
                }
            } else {
                CurrentChapterPath.CurrentRoom.CustomRoomName = name;
                count++;
            }

            Log($"Set custom room name of room '{CurrentChapterPath.CurrentRoom.DebugRoomName}' to '{name}' (Count: {count})");
            SaveActivePath();
            SaveChapterStats();//Recalc stats
        }

        #endregion

        #region Logging
        private bool LogInitialized;
        private StreamWriter LogFileWriter;
        public void LogInit() {
            string logFileMax = GetPathToFile(LogsFolder, $"log_old{LOG_FILE_COUNT}.txt");
            if (File.Exists(logFileMax)) {
                File.Delete(logFileMax);
            }

            for (int i = LOG_FILE_COUNT - 1; i >= 1; i--) {
                string logFilePath = GetPathToFile(LogsFolder, $"log_old{i}.txt");
                if (File.Exists(logFilePath)) {
                    string logFileNewPath = GetPathToFile(LogsFolder, $"log_old{i+1}.txt");
                    File.Move(logFilePath, logFileNewPath);
                }
            }

            string lastFile = GetPathToFile(LogsFolder, $"log.txt");
            if (File.Exists(lastFile)) {
                string logFileNewPath = GetPathToFile(LogsFolder, $"log_old1.txt");
                File.Move(lastFile, logFileNewPath);
            }

            string path = GetPathToFile(LogsFolder, $"log.txt");
            LogFileWriter = new StreamWriter(path) {
                AutoFlush = true
            };
            LogInitialized = true;
        }
        public void Log(string log, bool isFollowup = false, int frameBack = 1) {
            if (!LogInitialized) {
                return;
            }
            lock(LogLock) {
                if (!isFollowup) {
                    StackFrame frame = new StackTrace().GetFrame(frameBack);
                    string methodName = frame.GetMethod().Name;
                    string typeName = frame.GetMethod().DeclaringType?.Name ?? "<NoType>";

                    string time = DateTime.Now.ToString("HH:mm:ss.ffff");

                    LogFileWriter.WriteLine($"[{time}]\t[{typeName}.{methodName}]\t{log}");
                } else {
                    LogFileWriter.WriteLine($"\t\t{log}");
                }
            }
        }

        private int LogEveryN;
        public void LogEvery(int n, string message, bool isFollowup = false, int frameBack = 2) {
            LogEveryN++;
            if (LogEveryN >= n) {
                LogEveryN = 0;
                Log(message, isFollowup, frameBack);
            }
        }

        public void LogVerbose(string message, bool isFollowup = false, int frameBack = 2) {
            if (ModSettings.VerboseLogging) { 
                Log(message, isFollowup, frameBack);
            }
        }

        public void LogCleanup() {
            LogFileWriter?.Close();
            LogFileWriter?.Dispose();
        }
        #endregion

        #region Util

        public static string GetPathToFile(string file) {
            return Path.Combine(BaseFolderPath, file);
        }
        public static string GetPathToFile(string folder, string file) {
            return Path.Combine(BaseFolderPath, Path.Combine(folder, file));
        }
        public static string GetPathToFile(string folder, string subfolder, string file) {
            return Path.Combine(BaseFolderPath, Path.Combine(folder, Path.Combine(subfolder, file)));
        }
        
        public static string SanitizeSidForDialog(string sid) {
            return sid.DialogKeyify();
        }

        public void InsertCheckpointIntoPath(Checkpoint cp, string roomName) {
            Vector2 pos = cp?.Position ?? Vector2.Zero;
            if (roomName == null) {
                PathRec.AddCheckpoint(pos, PathRecorder.DefaultCheckpointName);
                return;
            }

            string cpDialogName = $"{CurrentChapterStats.ChapterSIDDialogSanitized}_{roomName}";
            Log($"cpDialogName: {cpDialogName}");
            string cpName = Dialog.Get(cpDialogName);
            Log($"Dialog.Get says: {cpName}");

            if (cpName.StartsWith("[") && cpName.EndsWith("]")) cpName = null;

            PathRec.AddCheckpoint(pos, cpName);
        }

        public AreaModeStats GetCurrentAreaModeStats() {
            try {
                SaveData saveData = SaveData.Instance;
                Session session = saveData.CurrentSession;
                AreaKey area = session.Area;
                AreaStats areaStats = saveData.Areas_Safe[area.ID];
                AreaModeStats modeStats = areaStats.Modes[(int)area.Mode];
                return modeStats;
            } catch (Exception) {
                // ignored
            }

            return null;
        }

        public Player GetPlayer() {
            Scene scene = Engine.Scene;
            if (!(scene is Level level)) {
                return null;
            }

            return level.Tracker.GetEntity<Player>();
        }
        
        private static bool FgrPathExists(int fgrNumber) {
            string fgrPathName = $"fgr_{fgrNumber}_path";
            string pathJson = GetPathToFile(FgrFolder, $"{fgrPathName}.json");
            return File.Exists(pathJson);
        }
        
        public static int GetHighestFgrWithPath() {
            int highestFgr = -1;
            for (int i = 1; i < 100; i++) {
                if (FgrPathExists(i)) {
                    highestFgr = i;
                } else {
                    break;
                }
            }
            return highestFgr;
        }
        #endregion
    }
}
