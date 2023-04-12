using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.ConsistencyTracker.ThirdParty;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.EverestInterop;
using Celeste.Mod.ConsistencyTracker.Properties;
using Celeste.Mod.ConsistencyTracker.Utility;
using Newtonsoft.Json;
using System.Diagnostics;
using Celeste.Mod.ConsistencyTracker.Entities;
using Monocle;
using System.Reflection;
using Celeste.Mod.ConsistencyTracker.PhysicsLog;
using Celeste.Mod.SpeedrunTool.TeleportRoom;
using Celeste.Editor;
using Microsoft.Xna.Framework.Graphics;
using Celeste.Mod.ConsistencyTracker.Enums;
using System.Xml.Linq;
using Celeste.Mod.ConsistencyTracker.Entities.Summary;

namespace Celeste.Mod.ConsistencyTracker {
    public class ConsistencyTrackerModule : EverestModule {

        public static ConsistencyTrackerModule Instance;
        private static readonly int LOG_FILE_COUNT = 10;

        #region Versions
        public class VersionsNewest {
            public static string Mod => "2.3.0";
            public static string Overlay => "2.0.0";
            public static string LiveDataEditor => "1.0.0";
            public static string PhysicsInspector => "1.1.2";
        }
        public class VersionsCurrent {
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
        public ConsistencyTrackerSettings ModSettings => (ConsistencyTrackerSettings)this._Settings;

        public static readonly string BaseFolderPath = "./ConsistencyTracker/";
        public static readonly string ExternalToolsFolder = "external-tools";
        public static readonly string LogsFolder = "logs";
        public static readonly string PathsFolder = "paths";
        public static readonly string StatsFolder = "stats";
        public static readonly string SummariesFolder = "summaries";


        private bool DidRestart { get; set; } = false;
        private HashSet<string> ChaptersThisSession { get; set; } = new HashSet<string>();

        #region Path Recording Variables

        public bool DoRecordPath {
            get => _DoRecordPath;
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

                _DoRecordPath = value;
            }
        }
        private bool _DoRecordPath = false;
        private PathRecorder PathRec;
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
        public string SpeedrunToolSaveStateRoomName;

        private string LastRoomWithCheckpoint = null;

        private bool _CurrentRoomCompleted = false;
        private bool _CurrentRoomCompletedResetOnDeath = false;
        public bool PlayerIsHoldingGolden {
            get => _PlayerIsHoldingGolden; 
            set { 
                _PlayerIsHoldingGolden = value;
                if (IngameOverlay != null) {
                    IngameOverlay.SetGoldenState(value);
                }
            } 
        }
        private bool _PlayerIsHoldingGolden = false;

        #endregion

        public StatManager StatsManager;
        public TextOverlay IngameOverlay;
        public SummaryHud SummaryOverlay;
        public PhysicsLogger PhysicsLog;


        public ConsistencyTrackerModule() {
            Instance = this;
        }

        #region Load/Unload Stuff

        public override void Load() {
            CheckFolderExists(BaseFolderPath);
            CheckFolderExists(GetPathToFile(LogsFolder));
            LogInit();
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Log($"~~~==== CCT STARTED ({time}) ====~~~");
            
            CheckFolderExists(GetPathToFile(PathsFolder));
            CheckPrepackagedPaths();
            
            CheckFolderExists(GetPathToFile(StatsFolder));
            CheckFolderExists(GetPathToFile(SummariesFolder));


            CheckFolderExists(GetPathToFile(ExternalToolsFolder));
            UpdateExternalTools();


            Log($"Mod Settings -> \n{JsonConvert.SerializeObject(ModSettings, Formatting.Indented)}");
            Log($"~~~==============================~~~");

            PhysicsLog = new PhysicsLogger();

            HookStuff();

            StatsManager = new StatManager();

            DebugRcPage.Load();

            //https://github.com/EverestAPI/CelesteTAS-EverestInterop/blob/master/CelesteTAS-EverestInterop/Source/Communication/StudioCommunicationClient.cs
            //idk how to use this class to get GameBananaId
            //ModUpdateInfo updateInfo = new ModUpdateInfo();
        }

        public override void Unload() {
            Log($"Called Unload");
            UnHookStuff();
            
            if (PhysicsLogger.Settings.IsRecording) {
                PhysicsLog.StopRecording();
            }

            DebugRcPage.Unload();
            LogCleanup();
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

            //On.Celeste.Player.Update += LogPhysicsUpdate;
            On.Monocle.Engine.Update += PhysicsLog.Engine_Update;
            //On.Monocle.Engine.Update += Engine_Update;

            On.Celeste.Editor.MapEditor.Render += MapEditor_Render;
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

            //On.Celeste.Player.Update -= LogPhysicsUpdate;
            On.Monocle.Engine.Update -= PhysicsLog.Engine_Update;
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
            LogVerbose($"Opened a door");
            SetRoomCompleted(resetOnDeath: false);
        }

        private DashCollisionResults ClutterSwitch_OnDashed(On.Celeste.ClutterSwitch.orig_OnDashed orig, ClutterSwitch self, Player player, Vector2 direction) {
            LogVerbose($"Activated a clutter switch");
            SetRoomCompleted(resetOnDeath: false);
            return orig(self, player, direction);
        }

        private void Key_OnPlayer(On.Celeste.Key.orig_OnPlayer orig, Key self, Player player) {
            LogVerbose($"Picked up a key");
            orig(self, player);
            SetRoomCompleted(resetOnDeath: false);
        }

        private void Cassette_OnPlayer(On.Celeste.Cassette.orig_OnPlayer orig, Cassette self, Player player) {
            LogVerbose($"Collected a cassette tape");
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

            LogVerbose($"Strawberry on player");
            SetRoomCompleted(resetOnDeath: true);
        }

        private void Strawberry_OnCollect(On.Celeste.Strawberry.orig_OnCollect orig, Strawberry self) {
            LogVerbose($"Collected a strawberry");
            orig(self);
            SetRoomCompleted(resetOnDeath: false);
        }

        private void CoreModeToggle_OnChangeMode(On.Celeste.CoreModeToggle.orig_OnChangeMode orig, CoreModeToggle self, Session.CoreModes mode) {
            LogVerbose($"Changed core mode to '{mode}'");
            orig(self, mode);
            SetRoomCompleted(resetOnDeath:true);
        }

        private void Checkpoint_TurnOn(On.Celeste.Checkpoint.orig_TurnOn orig, Checkpoint cp, bool animate) {
            orig(cp, animate);

            if (Engine.Scene is Level level) {
                LogVerbose($"Checkpoint in room '{level.Session.LevelData.Name}'");
            } else {
                LogVerbose($"Engine.Scene is not Level...");
                return;
            }

            if (level.Session == null) {
                LogVerbose($"level.Session is null");
                return;
            }
            if (level.Session.LevelData == null) {
                LogVerbose($"level.Session.LevelData is null");
                return;
            }

            string roomName = SanitizeRoomName(level.Session.LevelData.Name);

            Log($"cp.Position={cp.Position}, Room Name='{roomName}'");
            if (ModSettings.Enabled && DoRecordPath) {
                InsertCheckpointIntoPath(cp, roomName);
            }

            LastRoomWithCheckpoint = roomName;
        }

        //Not triggered when teleporting via debug map
        private void Level_TeleportTo(On.Celeste.Level.orig_TeleportTo orig, Level level, Player player, string nextLevel, Player.IntroTypes introType, Vector2? nearestSpawn) {
            orig(level, player, nextLevel, introType, nearestSpawn);
            Log($"level.Session.LevelData.Name={SanitizeRoomName(level.Session.LevelData.Name)}");

            
        }

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            string newCurrentRoom = SanitizeRoomName(level.Session.LevelData.Name);
            bool holdingGolden = PlayerIsHoldingGoldenBerry(level.Tracker.GetEntity<Player>());

            Log($"level.Session.LevelData.Name={newCurrentRoom}, playerIntro={playerIntro} | CurrentRoomName: '{CurrentRoomName}', PreviousRoomName: '{PreviousRoomName}'");

            //Changing room via golden berry death or debug map teleport
            if (playerIntro == Player.IntroTypes.Respawn && CurrentRoomName != null && newCurrentRoom != CurrentRoomName) {
                if (level.Session.LevelData.HasCheckpoint) {
                    LastRoomWithCheckpoint = newCurrentRoom;
                }
                SetNewRoom(newCurrentRoom, false, holdingGolden);
            }
            //Teleporters?
            if (playerIntro == Player.IntroTypes.Transition && CurrentRoomName != null && newCurrentRoom != CurrentRoomName && ModSettings.CountTeleportsForRoomTransitions) {
                if (level.Session.LevelData.HasCheckpoint) {
                    LastRoomWithCheckpoint = newCurrentRoom;
                }
                SetNewRoom(newCurrentRoom, true, holdingGolden);
            }

            if (DidRestart) {
                if (level.Session.LevelData.HasCheckpoint) {
                    LastRoomWithCheckpoint = newCurrentRoom;
                }
                Log($"\tRequested reset of PreviousRoomName to null", true);
                DidRestart = false;
                SetNewRoom(newCurrentRoom, false, holdingGolden);
                PreviousRoomName = null;
            }

            if (isFromLoader) {
                Log("Adding overlay!");
                IngameOverlay = new TextOverlay();
                level.Add(IngameOverlay);

                SummaryOverlay = new SummaryHud();
                level.Add(SummaryOverlay);
            }
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            Log($"mode={mode}, snow={snow}");
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

            if (PhysicsLogger.Settings.IsRecording) {
                PhysicsLog.StopRecording();
                PhysicsLog.IsInMap = false;
            }
        }

        private void Level_OnComplete(Level level) {
            Log($"Incrementing {CurrentChapterStats?.CurrentRoom.DebugRoomName}");
            if(ModSettings.Enabled && !ModSettings.PauseDeathTracking && (!ModSettings.OnlyTrackWithGoldenBerry || PlayerIsHoldingGolden))
                CurrentChapterStats?.AddAttempt(true);
            CurrentChapterStats.ModState.ChapterCompleted = true;
            SaveChapterStats();
        }

        private void Level_Begin(On.Celeste.Level.orig_Begin orig, Level level) {
            Log($"Calling ChangeChapter with 'level.Session'");
            ChangeChapter(level.Session);
            orig(level);
        }

        //**** USE THIS WHEN MORE NON-FUNCTIONING ROOM TRANSITIONS ARE FOUND ****//
        //private bool engineDelayHadSetTimer = false;
        //private int engineDelayTimer = 0;
        //private int engineDelayTime = 3;
        //public void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
        //    orig(self, gameTime);


        //    if (!(Engine.Scene is Level)) return;
        //    Level level = Engine.Scene as Level;
        //    Player player = level.Tracker.GetEntity<Player>();
        //    string roomNameNoSani = level.Session.LevelData.Name;

        //    if (level.Session.LevelData.HasCheckpoint) {
        //        LastRoomWithCheckpoint = roomNameNoSani;
        //    }

        //    string roomName = SanitizeRoomName(roomNameNoSani);

        //    if (CurrentRoomName != null && roomName != CurrentRoomName && ModSettings.CountTeleportsForRoomTransitions) {
        //        if (!engineDelayHadSetTimer) {
        //            engineDelayHadSetTimer = true;
        //            engineDelayTimer = engineDelayTime;
        //        }

        //        if (engineDelayTimer > 0) {
        //            engineDelayTimer--;
        //            return;
        //        }
        //        Log($"Engine.Update found a special room transition!");

        //        bool holdingGolden = PlayerIsHoldingGoldenBerry(player);
        //        SetNewRoom(roomName, true, holdingGolden);
        //    }
        //}
        
        private void Level_OnTransitionTo(Level level, LevelData levelDataNext, Vector2 direction) {
            if (levelDataNext.HasCheckpoint) {
                LastRoomWithCheckpoint = levelDataNext.Name;
            }

            string roomName = SanitizeRoomName(levelDataNext.Name);
            Log($"levelData.Name->{roomName}");

            if (CurrentRoomName != null && roomName != CurrentRoomName) {
                bool holdingGolden = PlayerIsHoldingGoldenBerry(level.Tracker.GetEntity<Player>());
                SetNewRoom(roomName, true, holdingGolden);
            }
        }

        private void Player_OnDie(Player player) {
            TouchedBerries.Clear();
            bool holdingGolden = PlayerIsHoldingGoldenBerry(player);

            Log($"Player died. (holdingGolden: {holdingGolden})");
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
            Log($"Called chapter change");
            AreaData area = AreaData.Areas[session.Area.ID];
            string chapName = area.Name;
            string chapNameClean = chapName.DialogCleanOrNull() ?? chapName.SpacedPascalCase();
            string campaignName = DialogExt.CleanLevelSet(area.GetLevelSet());

            Log($"Level->{session.Level}, session.Area.GetSID()->{session.Area.GetSID()}, session.Area.Mode->{session.Area.Mode}, chapterNameClean->{chapNameClean}, campaignName->{campaignName}");

            CurrentChapterDebugName = ($"{session.MapData.Data.SID}_{session.Area.Mode}").Replace("/", "_");

            //string test2 = Dialog.Get($"luma_farewellbb_FarewellBB_b_intro");
            //Log($"Dialog Test 2: {test2}");

            PreviousRoomName = null;
            CurrentRoomName = session.Level;

            CurrentChapterStats = GetCurrentChapterStats();
            CurrentChapterStats.ChapterDebugName = CurrentChapterDebugName;
            CurrentChapterStats.CampaignName = campaignName;
            CurrentChapterStats.ChapterName = chapNameClean;
            CurrentChapterStats.ChapterSID = session.MapData.Data.SID;
            CurrentChapterStats.ChapterSIDDialogSanitized = SanitizeSIDForDialog(session.MapData.Data.SID);
            CurrentChapterStats.SideName = session.Area.Mode.ToReadableString();

            SetCurrentChapterPath(GetPathInputInfo());

            //fix for SpeedrunTool savestate inconsistency
            TouchedBerries.Clear();
            
            SetNewRoom(CurrentRoomName, false, false);
            if (session.LevelData.HasCheckpoint) {
                LastRoomWithCheckpoint = CurrentRoomName;
            } else {
                LastRoomWithCheckpoint = null;
            }

            if (!DoRecordPath && ModSettings.RecordPath) { // TODO figure out why i did this
                DoRecordPath = true;
            }

            if (PhysicsLogger.Settings.IsRecording && PhysicsLog.IsInMap) {
                PhysicsLog.SegmentLog(true);
            }

            PhysicsLog.IsInMap = true;
        }

        public void SetCurrentChapterPath(PathInfo path) {
            CurrentChapterPath = path;
            if (CurrentChapterPath != null) {
                CurrentChapterPath.SetCheckpointRefs();

                if (CurrentChapterPath.ChapterName == null && CurrentChapterStats != null) {
                    CurrentChapterPath.CampaignName = CurrentChapterStats.CampaignName;
                    CurrentChapterPath.ChapterName = CurrentChapterStats.ChapterName;
                    CurrentChapterPath.ChapterSID = CurrentChapterStats.ChapterSID;
                    CurrentChapterPath.SideName = CurrentChapterStats.SideName;
                    SavePathToFile();
                }
            }
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

        public void SetNewRoom(string newRoomName, bool countDeath=true, bool holdingGolden=false) {
            PlayerIsHoldingGolden = holdingGolden;
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
            if (PreviousRoomName == newRoomName && !_CurrentRoomCompleted) {
                Log($"Entered previous room '{PreviousRoomName}'");
                PreviousRoomName = CurrentRoomName;
                CurrentRoomName = newRoomName;
                CurrentChapterStats?.SetCurrentRoom(newRoomName);
                SaveChapterStats();
                return;
            }

            //Transitioned to own room, probably due to teleport or entering a grouped room
            if (CurrentRoomName == newRoomName) {
                Log($"Entered own room '{newRoomName}', not updating state!");
                CurrentChapterStats.SetCurrentRoom(newRoomName);
                SaveChapterStats();
                return;
            }


            Log($"Entered new room '{newRoomName}' | Holding golden: '{holdingGolden}'");

            PreviousRoomName = CurrentRoomName;
            CurrentRoomName = newRoomName;
            _CurrentRoomCompleted = false;

            if (DoRecordPath) {
                PathRec.AddRoom(newRoomName);
            }

            if (ModSettings.Enabled && CurrentChapterStats != null) {
                if (countDeath && !ModSettings.PauseDeathTracking && (!ModSettings.OnlyTrackWithGoldenBerry || holdingGolden)) {
                    CurrentChapterStats.AddAttempt(true);
                }
                CurrentChapterStats.SetCurrentRoom(newRoomName);
                SaveChapterStats();
            }

            //engineDelayTimer = 0;
            //engineDelayHadSetTimer = false;
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

            SpeedrunToolSaveStateRoomName = CurrentRoomName;
            SaveChapterStats();
        }

        public void SpeedrunToolLoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            Type type = GetType();
            if (!savedvalues.ContainsKey(type)) {
                Log("Trying to load state without prior saving a state...");
                return;
            }

            IngameOverlay = level.Tracker.GetEntity<TextOverlay>();
            SummaryOverlay = level.Tracker.GetEntity<SummaryHud>();

            PreviousRoomName = (string) savedvalues[type][nameof(PreviousRoomName)];
            CurrentRoomName = (string) savedvalues[type][nameof(CurrentRoomName)];
            _CurrentRoomCompleted = (bool) savedvalues[type][nameof(_CurrentRoomCompleted)];
            _CurrentRoomCompletedResetOnDeath = (bool) savedvalues[type][nameof(_CurrentRoomCompletedResetOnDeath)];

            CurrentChapterStats.SetCurrentRoom(CurrentRoomName);
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

        public static string GetPathToFile(string file) {
            return Path.Combine(BaseFolderPath, file);
        }
        public static string GetPathToFile(string folder, string file) {
            return Path.Combine(BaseFolderPath, Path.Combine(folder, file));
        }
        public static string GetPathToFile(string folder, string subfolder, string file) {
            return Path.Combine(BaseFolderPath, Path.Combine(folder, Path.Combine(subfolder, file)));
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
            string path = GetPathToFile(PathsFolder, $"{CurrentChapterDebugName}.txt");
            return File.Exists(path);
        }
        public PathInfo GetPathInputInfo(string pathName = null) {
            if(pathName == null) {
                pathName = CurrentChapterDebugName;
            }
            Log($"Fetching path info for chapter '{pathName}'");

            string path = GetPathToFile(PathsFolder, $"{pathName}.txt");
            Log($"\tSearching for path '{path}'", true);

            if (File.Exists(path)) { //Parse File
                Log($"\tFound file, parsing...", true);
                string content = File.ReadAllText(path);

                //[Try 1] New file format: JSON
                try {
                    return JsonConvert.DeserializeObject<PathInfo>(content);
                } catch (Exception) {
                    Log($"\tCouldn't read path info as JSON, trying old path format...", true);
                }

                //[Try 2] Old file format: selfmade text format
                try {
                    PathInfo parsedOldFormat = PathInfo.ParseString(content);
                    Log($"\tSaving path for map '{pathName}' in new format!", true);
                    SavePathToFile(parsedOldFormat, pathName); //Save in new format
                    return parsedOldFormat;
                } catch (Exception) {
                    Log($"\tCouldn't read old path info. Old path info content:\n{content}", true);
                    return null;
                }

            } else { //Create new
                Log($"\tDidn't find file at '{path}', returned null.", true);
                return null;
            }
        }

        public ChapterStats GetCurrentChapterStats() {
            string path = GetPathToFile(StatsFolder, $"{CurrentChapterDebugName}.txt");

            bool hasEnteredThisSession = ChaptersThisSession.Contains(CurrentChapterDebugName);
            ChaptersThisSession.Add(CurrentChapterDebugName);
            Log($"CurrentChapterName: '{CurrentChapterDebugName}', hasEnteredThisSession: '{hasEnteredThisSession}', ChaptersThisSession: '{string.Join(", ", ChaptersThisSession)}'");

            ChapterStats toRet = null;

            if (File.Exists(path)) { //Parse File
                string content = File.ReadAllText(path);

                //[Try 1] New file format: JSON
                try {
                    toRet = JsonConvert.DeserializeObject<ChapterStats>(content);
                } catch (Exception) {
                    Log($"\tCouldn't read chapter stats as JSON, trying old stats format...", true);
                }

                if (toRet == null) {
                    //[Try 2] Old file format: selfmade text format
                    try {
                        toRet = ChapterStats.ParseString(content);
                        Log($"\tSaving chapter stats for map '{CurrentChapterDebugName}' in new format!", true);
                    } catch (Exception) {
                        Log($"\tCouldn't read old chapter stats, created new ChapterStats. Old chapter stats content:\n{content}", true);
                        toRet = new ChapterStats();
                        toRet.SetCurrentRoom(CurrentRoomName);
                    }
                }
                
            } else { //Create new
                toRet = new ChapterStats();
                toRet.SetCurrentRoom(CurrentRoomName);
            }

            if (!hasEnteredThisSession) {
                toRet.ResetCurrentSession();
            }
            toRet.ResetCurrentRun();

            return toRet;
        }

        public void SaveChapterStats() {
            if (CurrentChapterStats == null) {
                Log($"Aborting saving chapter stats as '{nameof(CurrentChapterStats)}' is null");
                return;
            }
            if (!ModSettings.Enabled) {
                return;
            }

            CurrentUpdateFrame++;

            CurrentChapterStats.ModState.PlayerIsHoldingGolden = PlayerIsHoldingGolden;
            CurrentChapterStats.ModState.GoldenDone = PlayerIsHoldingGolden && CurrentChapterStats.ModState.ChapterCompleted;

            CurrentChapterStats.ModState.DeathTrackingPaused = ModSettings.PauseDeathTracking;
            CurrentChapterStats.ModState.RecordingPath = ModSettings.RecordPath;
            CurrentChapterStats.ModState.OverlayVersion = VersionsCurrent.Overlay;
            CurrentChapterStats.ModState.ModVersion = VersionsNewest.Mod;
            CurrentChapterStats.ModState.ChapterHasPath = CurrentChapterPath != null;


            string path = GetPathToFile(StatsFolder, $"{CurrentChapterDebugName}.txt");
            File.WriteAllText(path, JsonConvert.SerializeObject(CurrentChapterStats, Formatting.Indented));

            string modStatePath = GetPathToFile(StatsFolder, $"modState.txt");

            string content = $"{CurrentChapterStats.CurrentRoom}\n{CurrentChapterStats.ChapterDebugName};{CurrentChapterStats.ModState}\n";
            File.WriteAllText(modStatePath, content);

            StatsManager.OutputFormats(CurrentChapterPath, CurrentChapterStats);
        }

        public void CreateChapterSummary(int attemptCount) {
            Log($"Attempting to create tracker summary, attemptCount = '{attemptCount}'");

            bool hasPathInfo = PathInfoExists();

            string outPath = GetPathToFile(SummariesFolder, $"{CurrentChapterDebugName}.txt");

            if (!hasPathInfo) {
                Log($"Called CreateChapterSummary without chapter path info. Please create a path before using this feature");
                File.WriteAllText(outPath, "No path info was found for the current chapter.\nPlease create a path before using the summary feature");
                return;
            }

            CurrentChapterStats?.OutputSummary(outPath, CurrentChapterPath, attemptCount);
        }

        #endregion

        #region Default Path Creation

        public void CheckPrepackagedPaths() {
            string assetPath = "Assets/DefaultPaths";
            List<string> sideNames = new List<string>() { "Normal", "BSide", "CSide" };
            List<string> levelNames = new List<string>() {
                "1-ForsakenCity",
                "2-OldSite",
                "3-CelestialResort",
                "4-GoldenRidge",
                "5-MirrorTemple",
                "6-Reflection",
                "7-Summit",
                "9-Core",
            };
            string farewellName = "Celeste_LostLevels_Normal.txt";
            string farewellAssetName = "Celeste_LostLevels_Normal";

            foreach (string level in levelNames) {
                foreach (string side in sideNames) {
                    string name = $"Celeste_{level}_{side}.txt";
                    string assetName = $"Celeste_{level}_{side}";
                    LogVerbose($"Checking path file '{name}'...");
                    CheckDefaultPathFile(name, $"{assetPath}/{assetName}");
                }
            }

            CheckDefaultPathFile(farewellName, $"{assetPath}/{farewellAssetName}");
        }
        private void CheckDefaultPathFile(string name, string assetPath) {
            string path = GetPathToFile(PathsFolder, $"{name}.txt");

            if (!File.Exists(path)) {
                CreatePathFileFromStream(name, assetPath);
            } else {
                LogVerbose($"Path file '{name}' already exists, skipping");
            }
        }

        public void UpdateExternalTools() {
            Log($"Checking for external tool updates...");

            string basePath = "Assets";
            
            List<string> externalOverlayFiles = new List<string>() {
                    "common.js",
                    "CCTOverlay.html",
                    "CCTOverlay.js",
                    "CCTOverlay.css",
                    "img/goldberry.gif"
            };
            string externalOverlayPath = $"{basePath}/ExternalOverlay";


            List<string> livedataEditorFiles = new List<string>() {
                    "LiveDataEditTool.html",
                    "LiveDataEditTool.js",
                    "LiveDataEditTool.css",
            };
            string livedataEditorPath = $"{basePath}/LiveDataEditor";


            List<string> physicsInspectorFiles = new List<string>() {
                    "PhysicsInspector.html",
                    "PhysicsInspector.js",
                    "PhysicsInspector.css",
            };
            string physicsInspectorPath = $"{basePath}/PhysicsInspector";


            //Overlay files
            string alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, "common.js");
            if (Util.IsUpdateAvailable(VersionsCurrent.Overlay, VersionsNewest.Overlay) || !File.Exists(alreadyGeneratedPath)) {
                Log($"Updating External Overlay from version {VersionsCurrent.Overlay ?? "null"} to version {VersionsNewest.Overlay}");
                VersionsCurrent.Overlay = VersionsNewest.Overlay;

                CheckFolderExists(GetPathToFile(ExternalToolsFolder, "img"));

                foreach (string file in externalOverlayFiles) {
                    CreateExternalToolFileFromStream(file, $"{externalOverlayPath}/{file}");
                }
            } else {
                Log($"External Overlay is up to date at version {VersionsCurrent.Overlay}");
            }

            //Path Edit Tool files

            //Format Edit Tool files
            alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, "LiveDataEditTool.html");
            if (Util.IsUpdateAvailable(VersionsCurrent.LiveDataEditor, VersionsNewest.LiveDataEditor) || !File.Exists(alreadyGeneratedPath)) {
                Log($"Updating LiveData Editor from version {VersionsCurrent.LiveDataEditor ?? "null"} to version {VersionsNewest.LiveDataEditor}");
                VersionsCurrent.LiveDataEditor = VersionsNewest.LiveDataEditor;

                foreach (string file in livedataEditorFiles) {
                    CreateExternalToolFileFromStream(file, $"{livedataEditorPath}/{file}");
                }
            } else {
                Log($"LiveData Editor is up to date at version {VersionsCurrent.LiveDataEditor}");
            }

            //Physics Inspector Tool files
            alreadyGeneratedPath = GetPathToFile(ExternalToolsFolder, "PhysicsInspector.html");
            if (Util.IsUpdateAvailable(VersionsCurrent.PhysicsInspector, VersionsNewest.PhysicsInspector) || !File.Exists(alreadyGeneratedPath)) {
                Log($"Updating Physics Inspector from version {VersionsCurrent.PhysicsInspector ?? "null"} to version {VersionsNewest.PhysicsInspector}");
                VersionsCurrent.PhysicsInspector = VersionsNewest.PhysicsInspector;

                foreach (string file in physicsInspectorFiles) {
                    CreateExternalToolFileFromStream(file, $"{physicsInspectorPath}/{file}");
                }
            } else {
                Log($"Physics Inspector is up to date at version {VersionsCurrent.PhysicsInspector}");
            }

        }

        private void CreateExternalToolFileFromStream(string name, string assetPath) {
            CreateFileFromStream(ExternalToolsFolder, name, assetPath);
        }
        private void CreatePathFileFromStream(string name, string assetPath) {
            CreateFileFromStream(PathsFolder, name, assetPath);
        }
        private void CreateFileFromStream(string folder, string name, string assetPath) {
            string path = GetPathToFile(folder, name);

            LogVerbose($"Trying to access asset at '{assetPath}'");
            if (Everest.Content.TryGet(assetPath, out ModAsset value, true)) {
                using (var fileStream = File.Create(path)) {
                    value.Stream.Seek(0, SeekOrigin.Begin);
                    value.Stream.CopyTo(fileStream);
                    LogVerbose($"Wrote file '{name}' to path '{path}'");
                }
            } else {
                Log($"No asset found with content path '{assetPath}'");
            }
        }

        #endregion

        #region Stats Data Control

        public void WipeChapterData() {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping chapter data as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Wiping death data for chapter '{CurrentChapterDebugName}'");

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
                Log($"Aborting wiping room golden berry deaths as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Wiping golden berry death data for room '{CurrentChapterStats.CurrentRoom.DebugRoomName}'");

            CurrentChapterStats.CurrentRoom.GoldenBerryDeaths = 0;
            CurrentChapterStats.CurrentRoom.GoldenBerryDeathsSession = 0;

            SaveChapterStats();
        }
        public void WipeChapterGoldenBerryDeaths() {
            if (CurrentChapterStats == null) {
                Log($"Aborting wiping chapter golden berry deaths as '{nameof(CurrentChapterStats)}' is null");
                return;
            }

            Log($"Wiping golden berry death data for chapter '{CurrentChapterDebugName}'");

            foreach (string debugName in CurrentChapterStats.Rooms.Keys) {
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeaths = 0;
                CurrentChapterStats.Rooms[debugName].GoldenBerryDeathsSession = 0;
            }

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

        private void MapEditor_Render(On.Celeste.Editor.MapEditor.orig_Render orig, MapEditor self) {
            orig(self);

            if (!ModSettings.Enabled || CurrentChapterPath == null || !ModSettings.ShowCCTRoomNamesOnDebugMap) return;
            
            List<LevelTemplate> levels = Util.GetPrivateProperty<List<LevelTemplate>>(self, "levels");
            Camera camera = Util.GetPrivateStaticProperty<Camera>(self, "Camera");

            Draw.SpriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    null,
                    Engine.ScreenMatrix);

            foreach (LevelTemplate template in levels) {
                string name = template.Name;
                RoomInfo rInfo = CurrentChapterPath.FindRoom(name);
                if (rInfo == null) {
                    string resolvedName = ResolveGroupedRoomName(name);
                    rInfo = CurrentChapterPath.FindRoom(resolvedName);

                    if (rInfo == null) {
                        continue;
                    }
                }
                string formattedName = rInfo.GetFormattedRoomName(ModSettings.LiveDataRoomNameDisplayType);

                int x = template.X;
                int y = template.Y;

                Vector2 pos = new Vector2(x + template.Rect.Width / 2, y);
                pos -= camera.Position;
                pos = new Vector2((float)Math.Round(pos.X), (float)Math.Round(pos.Y));
                pos *= camera.Zoom;
                pos += new Vector2(960f, 540f);

                ActiveFont.DrawOutline(
                    formattedName,
                    pos,
                    new Vector2(0.5f, 0),
                    Vector2.One * camera.Zoom / 6,
                    Color.White * 0.9f,
                    2f * camera.Zoom / 6,
                    Color.Black * 0.7f);
            }

            Draw.SpriteBatch.End();
        }

        public void SaveRecordedRoomPath() {
            Log($"Saving recorded path...");
            if (PathRec.TotalRecordedRooms <= 1) {
                Log($"Path is too short to save. ({PathRec.TotalRecordedRooms} rooms)");
                return;
            }
            
            DisabledInRoomName = CurrentRoomName;
            SetCurrentChapterPath(PathRec.ToPathInfo());
            Log($"Recorded path:\n{JsonConvert.SerializeObject(CurrentChapterPath)}", true);
            SavePathToFile();
        }
        public void SavePathToFile(PathInfo path = null, string pathName = null) {
            if (path == null) {
                path = CurrentChapterPath;
            }
            if (pathName == null) {
                pathName = CurrentChapterDebugName;
            }

            string relativeOutPath = $"{PathsFolder}/{pathName}.txt";
            string outPath = GetPathToFile(relativeOutPath);
            File.WriteAllText(outPath, JsonConvert.SerializeObject(path, Formatting.Indented));
            Log($"Wrote path data to '{relativeOutPath}'");
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
                SavePathToFile();
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

            CheckpointInfo previousRoomCp = null;
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
                        previousRoomCp = cpInfo;
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
            
            SavePathToFile();
            SaveChapterStats();
        }

        public void UngroupRoomsOnChapterPath() {
            if (CurrentChapterPath == null) {
                Log($"CurrentChapterPath was null");
                return;
            }
            
            Level level = Engine.Scene as Level;
            string actualRoomName = level.Session.Level;
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

            SavePathToFile();
            SaveChapterStats();
        }


        #endregion

        #region Logging
        private bool LogInitialized = false;
        private StreamWriter LogFileWriter = null;
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
                string logFileNewPath = GetPathToFile(LogsFolder, $"log_old{1}.txt");
                File.Move(lastFile, logFileNewPath);
            }

            string path = GetPathToFile(LogsFolder, $"log.txt");
            LogFileWriter = new StreamWriter(path) {
                AutoFlush = true
            };
            LogInitialized = true;
        }
        public void Log(string log, bool isFollowup = false, bool isComingFromVerbose = false) {
            if (!LogInitialized) {
                return;
            }

            if (!isFollowup) {
                int frameBack = 1;
                if (isComingFromVerbose) {
                    frameBack = 2;
                }

                StackFrame frame = new StackTrace().GetFrame(frameBack);
                string methodName = frame.GetMethod().Name;
                string typeName = frame.GetMethod().DeclaringType.Name;

                string time = DateTime.Now.ToString("HH:mm:ss.ffff");

                LogFileWriter.WriteLine($"[{time}]\t[{typeName}.{methodName}]\t{log}");
            } else {
                LogFileWriter.WriteLine($"\t\t{log}");
            }
        }

        public void LogVerbose(string message, bool isFollowup = false) {
            if (ModSettings.VerboseLogging) { 
                Log(message, isFollowup, true);
            }
        }

        public void LogCleanup() {
            LogFileWriter?.Close();
            LogFileWriter?.Dispose();
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
                PathRec.AddCheckpoint(cp, PathRecorder.DefaultCheckpointName);
                return;
            }

            string cpDialogName = $"{CurrentChapterStats.ChapterSIDDialogSanitized}_{roomName}";
            Log($"cpDialogName: {cpDialogName}");
            string cpName = Dialog.Get(cpDialogName);
            Log($"Dialog.Get says: {cpName}");

            //if (cpName.Length+1 >= cpDialogName.Length && cpName.Substring(1, cpDialogName.Length) == cpDialogName) cpName = null;
            if (cpName.StartsWith("[") && cpName.EndsWith("]")) cpName = null;

            PathRec.AddCheckpoint(cp, cpName);
        }
        #endregion
    }
}
