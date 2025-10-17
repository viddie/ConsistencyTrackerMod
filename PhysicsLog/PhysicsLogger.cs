using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog
{
    public class PhysicsLogger {

        public enum FileType { 
            PhysicsLog,
            Layout
        }

        public enum RecordingType { 
            Recent,
            Saved,
        }

        public class Direction { //Flag for dash direction, UP/DOWN/LEFT/RIGHT have bits, diagonals are combinations of those
            public const int UP = 1;
            public const int DOWN = 2;
            public const int LEFT = 4;
            public const int RIGHT = 8;

            public const int UP_LEFT = UP | LEFT;
            public const int UP_RIGHT = UP | RIGHT;
            public const int DOWN_LEFT = DOWN | LEFT;
            public const int DOWN_RIGHT = DOWN | RIGHT;

            public static string ToString(int dir) {
                if (dir == UP) return "UP";
                if (dir == DOWN) return "DOWN";
                if (dir == LEFT) return "LEFT";
                if (dir == RIGHT) return "RIGHT";
                if (dir == UP_LEFT) return "UP_LEFT";
                if (dir == UP_RIGHT) return "UP_RIGHT";
                if (dir == DOWN_LEFT) return "DOWN_LEFT";
                if (dir == DOWN_RIGHT) return "DOWN_RIGHT";
                return "NONE";
            }
        }

        public enum EntityList {
            Static,
            Movable
        }

        private static List<string> EntityNamesSpinners = new List<string>() {
            "CrystalStaticSpinner",
            "DustStaticSpinner",
            "CustomSpinner",
            "DustTrackSpinner", "DustRotateSpinner",
        };
        private static List<string> EntityNamesHitboxColliders = new List<string>() {
            "Spikes", "RainbowSpikes", "BouncySpikes",
            "TriggerSpikes", "GroupedTriggerSpikes", "GroupedDustTriggerSpikes", "TriggerSpikesOriginal", "RainbowTriggerSpikes", "TimedTriggerSpikes",
            "Lightning",

            "Refill", "CustomRefill", "RefillWall",
            "Spring", "CustomSpring", "DashSpring", "SpringGreen",
            "DreamBlock", "CustomDreamBlock", "CustomDreamBlockV2", "ConnectedDreamBlock", "DashThroughSpikes",
            "SinkingPlatform",
            "SwapBlock", "ToggleSwapBlock", "ReskinnableSwapBlock",
            "ZipMover", "LinkedZipMover", "LinkedZipMoverNoReturn",
            "TouchSwitch", "SwitchGate", "FlagTouchSwitch", "FlagSwitchGate", "MovingTouchSwitch",
            "BounceBlock", //Core Block
            "CrushBlock", //Kevin
            "DashBlock",
            "DashSwitch", "TempleGate",
            "Glider", "RespawningJellyfish",
            "SeekerBarrier", "CrystalBombDetonator", "HoldableBarrier",
            "TempleCrackedBlock",
            "FlyFeather",
            "Cloud",
            "WallBooster", "IcyFloor", //Conveyers or IceWalls, depending on Core Mode
            "MoveBlock", "DreamMoveBlock", "VitMoveBlock", "MoveBlockCustomSpeed",
            "CassetteBlock", "WonkyCassetteBlock",

            "Puffer", "StaticPuffer", "SpeedPreservePuffer",

            "FallingBlock", "GroupedFallingBlock", "RisingBlock",
            "JumpThru", "SidewaysJumpThru", "AttachedJumpThru", "JumpthruPlatform", "UpsideDownJumpThru", "AttachedSidewaysJumpThru",
            "CrumblePlatform", "CrumbleBlock", "CrumbleBlockOnTouch", "VariableCrumblePlatform",
            "FloatySpaceBlock", "FancyFloatySpaceBlock", "FloatierSpaceBlock", "FloatyBreakBlock",
            "ClutterBlockBase", "ClutterDoor", "ClutterSwitch",
            "Key", "LockBlock",
            "StarJumpBlock",

            "Strawberry",
            "SilverBerry",

            "Lookout", "CustomPlaybackWatchtower", //Binos
            "LightningBreakerBox",

            "Killbox",
            "FakeWall", "InvisibleBarrier", "CustomInvisibleBarrier",

            //Modded Entities
            "Portal",
        };
        private static List<string> EntityNamesHitcircleColliders = new List<string>() {
            "Booster", "BlueBooster",
            "Bumper", "StaticBumper", "VortexBumper",
            "Shield",
        };
        private static List<string> EntityNamesOther = new List<string>() {
            "ConnectedMoveBlock", "AngryOshiro"
        };
        private static List<string> IgnoreEntityNames = new List<string>() {
            //UI Entities
            "Player",
            "InputHistoryListener", "InputHistoryListEntity",
            "DashCountIndicator",
            "Speedometer",
            "SpeedrunTimerDisplay",
            "BombTimerDisplay",
            "TotalStrawberriesDisplay",
            "GameplayStats",
            "SelectedAreaEntity",
            "GrabbyIcon",
            "SpaceJumpIndicator", "JumpIndicator",
            "TextOverlay", "LineupIndicatorEntity", "SummaryHud",
            "TalkComponentUI",
            "DeathDisplay",
            "DashSequenceDisplay",
            "AnalogDisplay",
            "WorldTextEntity",

            //Deco
            "SolidTiles", "FG",
            "Decal", "FlagDecal", "ParticleSystem", "ParticleEmitter", "FloatingDebris", "ForegroundDebris",
            "BackgroundTiles", "BGTilesRenderer", "GlassBlockBg",
            "MirrorSurfaces", "WaterSurface", "WaterFloatingObject", "ColoredWater", "ColoredWaterfall",
            "ColoredBigWaterfall", "CustomParallaxBigWaterfall",
            "DustEdges",
            "FormationBackdrop",
            "CustomHangingLamp", "ResortLantern", "WireLamps", "HangingLamp", "CustomTorch2", "LightSource", "LightingMask", "RustyLamp", "Lamp",
            "LightBeam", "FlickerLightSource", "LightSourceZone", "InvisibleLightSource",
            "Raindrop", "SpinnerGlow", "PlatformGlow", "RectangleGlow",
            "StaticDoor", "LightOccludeBlock",
            "CustomFlagline", "ConfettiTrigger",
            "Clothesline", "Chain", "Wire", "Moth", "CustomFlutterBird",
            "CustomPlayerPlayback", "CrumbleWallOnRumble",
            "PseudoPolyhedron", "PlaybackBillboard",
            "CustomNPC", "MoreCustomNPC", "StrawberryJamJar",
            "MoonCreature", "FlutterBird",
            "ColoredHangingLamp", "Torch", "LitBlueTorch", "Cobweb",
            "CustomMoonCreature",

            //Camera
            "CameraTargetTrigger", "CameraOffsetBorder", "CameraOffsetTrigger",
            "SmoothCameraOffsetTrigger", "InstantLockingCameraTrigger", "CameraHitboxEntity",
            "LookoutBlocker", "CameraAdvanceTargetTrigger", "CameraCatchupSpeedTrigger",
            "OneWayCameraTrigger", "MomentumCameraOffsetTrigger", "CameraTargetCornerTrigger",
            "CameraTargetCrossfadeTrigger",

            //Triggers
            "FlagTrigger", "FlagIfVisibleTrigger",
            "TeleportationTrigger", "TeleportationTarget",
            "ChangeRespawnTrigger", "SpawnFacingTrigger",
            "LuaCutsceneTrigger", "LuaCutsceneEntity",
            "DialogCutsceneTrigger", "MiniTextboxTrigger",
            "ExtendedVariantTrigger", "BooleanExtendedVariantTrigger", "ForceVariantTrigger", "FloatExtendedVariantFadeTrigger", "ExtendedVariantFadeTrigger",
            "FloatExtendedVariantTrigger", "ResetVariantsTrigger", "FloatFadeTrigger",
            "TriggerTrigger", "KillBoxTrigger", "LightningColorTrigger", "ColorGradeTrigger",
            "RumbleTrigger", "ScreenWipeTrigger", "ShakeTrigger",
            "MiniHeartDoorUnlockCutsceneTrigger", "TimeModulationTrigger",
            "PocketUmbrellaTrigger",

            //Styles & Lighting
            "StylegroundMask", "ColorGradeMask",
            "BloomFadeTrigger", "LightFadeTrigger", "BloomStrengthTrigger", "SetBloomStrengthTrigger", "SetBloomBaseTrigger", "SetDarknessAlphaTrigger",
            "MadelineSpotlightModifierTrigger", "FlashTrigger", "AlphaLerpLightSource", "ColorLerpLightSource", "BloomMask", "MadelineSilhouetteTrigger",
            "ColorGradeFadeTrigger", "EditDepthTrigger", "FlashlightColorTrigger", "LightningColorTrigger", "RemoveLightSourcesTrigger",
            "ColoredLightbeam", "CustomLightBeam", "GradualChangeColorGradeTrigger",
            "BloomColorFadeTrigger", "RainbowSpinnerColorFadeTrigger", "BloomColorTrigger",

            //Music
            "MusicParamTrigger", "AmbienceVolumeTrigger", "LightningMuter", "MusicFadeTrigger", "AmbienceParamTrigger",
            "MusicTrigger",

            //Controllers/Managers
            "LaserDetectorManager",
            "UnderwaterSwitchController",
            "WindController",
            "CustomSpinnerController",
            "RainbowSpinnerColorController", "RainbowSpinnerColorAreaController",
            "TimeController",
            "SeekerEffectsController", "SeekerBarrierRenderer",
            "StylegroundFadeController", "PhotosensitiveFlagController",
            "EntityRainbowifyController",
            "GlowController",
            "TrailManager",
            "ParallaxFadeOutController",
            "CustomizableGlassBlockAreaController",
            "CassetteMusicTransitionController",
            "BitsMagicLanternController",
            "LobbyMapController",
            "RainbowTilesetController",
            "StarClimbGraphicsController",
            "AssistIconController",

            //Renderers
            "PathRenderer",
            "LightningRenderer",
            "DreamSpinnerRenderer", "DreamTunnelRenderer", "DreamTunnelEntryRenderer", "DreamJellyfishRenderer", "DreamDashController",
            "MoveBlockBarrierRenderer", "PlayerSeekerBarrierRenderer", "PufferBarrierRenderer",
            "CrystalBombDetonatorRenderer", "CrystalBombFieldRenderer",
            "FlagKillBarrierRenderer",
            "DecalContainerRenderer",
            "InstantTeleporterRenderer",
            "SpinnerConnectorRenderer",
            "Renderer",

            //Misc
            "OnSpawnActivator", "EntityActivator",
            "AttachedContainer",
            "BurstEffect",
            "ClutterBlock",
            "EntityMover",
            "LobbyMapWarp",
            "AllInOneMask",

            //Idk
            "Why",
            "BlockField",
            "Border",
            "SlashFx",
            "Snapshot",
            "Entity", "HelperEntity",
        };
        private static List<string> EntityNamesToTest = new List<string>() {
            
        };
        private static List<string> EntityNamesMovables = new List<string>() {
            "DustTrackSpinner", "DustRotateSpinner",
            "SinkingPlatform", "AngryOshiro",
            "SwapBlock", "ToggleSwapBlock", "ReskinnableSwapBlock",
            "ZipMover", "LinkedZipMover", "LinkedZipMoverNoReturn", "DreamZipMover",
            "TouchSwitch", "SwitchGate", "FlagTouchSwitch", "FlagSwitchGate", "MovingTouchSwitch",
            "BounceBlock", //Core Block
            "CrushBlock", "UninterruptedNRCB",
            "Glider", "RespawningJellyfish", "CustomGlider", "TheoCrystal", "CrystalBomb", "ExtendedVariantTheoCrystal",
            "Cloud",
            "MoveBlock", "DreamMoveBlock", "VitMoveBlock", "ConnectedMoveBlock", "MoveBlockCustomSpeed",
            "FallingBlock", "GroupedFallingBlock", "RisingBlock",
            "AttachedJumpThru", "AttachedSidewaysJumpThru",
            "FloatySpaceBlock", "FancyFloatySpaceBlock", "FloatierSpaceBlock", "FloatyBreakBlock",
        };

        private static List<string> CustomEntityNamesMovables = new List<string>();
        private static List<string> CustomIgnoredEntityNames = new List<string>();

        
        public static readonly string PhysicsLogFolder = "physics-recordings";
        public static readonly string EntityListsFolder = "entity-lists";
        
        
        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        private static ConsistencyTrackerSettings ModSettings => Mod.ModSettings;
        
        // Mask for the mod settings option LogPhysicsEnabled, since we dont ever want to change that from here.
        private static bool _IsRecording = true;
        
        public static class Settings {
            public static bool IsRecording {
                get => _IsRecording && ModSettings.LogPhysicsEnabled;
                set => _IsRecording = value;
            }
            public static bool SegmentOnDeath {
                get => ModSettings.LogSegmentOnDeath;
                set => ModSettings.LogSegmentOnDeath = value;
            }
            public static bool SegmentOnLoadState {
                get => ModSettings.LogSegmentOnLoadState;
                set => ModSettings.LogSegmentOnLoadState = value;
            }
            public static bool InputsToTasFile {
                get => ModSettings.LogPhysicsInputsToTasFile;
                set => ModSettings.LogPhysicsInputsToTasFile = value;
            }
            public static bool FlipY {
                get => ModSettings.LogFlipY;
                set => ModSettings.LogFlipY = value;
            }

            public static bool FlagDashes {
                get => ModSettings.LogFlagDashes;
                set => ModSettings.LogFlagDashes = value;
            }
            public static bool FlagMaxDashes {
                get => ModSettings.LogFlagMaxDashes;
                set => ModSettings.LogFlagMaxDashes = value;
            }
            public static bool FlagDashDir {
                get => ModSettings.LogFlagDashDir;
                set => ModSettings.LogFlagDashDir = value;
            }
            public static bool FlagFacing {
                get => ModSettings.LogFlagFacing;
                set => ModSettings.LogFlagFacing = value;
            }
        }

        private DateTime RecordingStarted;
        private string RecordingStartedInSID;
        private string RecordingStartedInMapBin;
        private string RecordingStartedInChapterName;
        private string RecordingStartedInSideName;
        private Vector2 LastExactPos = Vector2.Zero;
        private bool LastFrameEnabled = false;
        private int FrameNumber = -1;
        private long RTAFrameOffset = -1;
        private Player LastPlayer = null;

        private int TasFrameCount = 0;
        private string TasInputs = null;
        private string TasFileContent = null;
        
        
        public bool IsInMap { get; set; }
        private bool playerMarkedDead = false;
        private bool doSegmentRecording = false;
        private bool skipFrameOnSegment = false;

        
        public PhysicsRecordingsManager RecordingsManager { get; set; }
        private Dictionary<int, LoggedEntity> RoomEntities = new Dictionary<int, LoggedEntity>();

        public PhysicsLogger() {
            RecordingsManager = new PhysicsRecordingsManager();

            LoadCustomEntityNames();
        }

        public void LoadCustomEntityNames() {
            ConsistencyTrackerModule.CheckFolderExists(
                ConsistencyTrackerModule.GetPathToFile(PhysicsLogFolder, EntityListsFolder));
            
            LoadEntityNamesList(ref CustomEntityNamesMovables, "movable-entities");
            LoadEntityNamesList(ref CustomIgnoredEntityNames, "ignored-entities");
        }

        private static readonly string _FileExplanation = "# Put 1 entity name (case sensitive!) per line\n" +
                                                          "# The lists do the following:\n" +
                                                          "# - movable-entities: Entities on this list will be tracked every frame for position changes\n" +
                                                          "# - ignored-entities: Entities on this list will be ignored entirely\n";
        private void LoadEntityNamesList(ref List<string> list, string listName) {
            string filePath =
                ConsistencyTrackerModule.GetPathToFile(PhysicsLogFolder, EntityListsFolder, listName + ".txt");
            if (!File.Exists(filePath)) {
                File.WriteAllText(filePath, _FileExplanation);
                return;
            }

            list.Clear();
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines) {
                if (line.Length > 0 && !line.StartsWith("#")) {
                    list.Add(line);
                }
            }
            
            Mod.Log($"Loaded {list.Count} custom entity names from '{listName}.txt'");
        }

        #region Events
        public void Hook() {
            On.Monocle.Engine.Update += Engine_Update;
            Everest.Events.Level.OnExit += Level_OnExit;
            Events.Events.OnResetSession += Events_OnResetSession;
        }

        public void UnHook() {
            On.Monocle.Engine.Update -= Engine_Update;
            Everest.Events.Level.OnExit -= Level_OnExit;
            Events.Events.OnResetSession -= Events_OnResetSession;
        }

        public void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
            orig(self, gameTime);

            if (Engine.Scene is Level level) {
                Player player = level.Tracker.GetEntity<Player>();
                LogPhysicsUpdate(player, level);
            }
        }
        private void Events_OnResetSession(bool sameSession) {
            if (Settings.IsRecording && IsInMap) {
                SegmentLog(true);
            }
            IsInMap = true;
        }
        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            if (!Settings.IsRecording) return;
            StopRecording();
            IsInMap = false;
        }
        #endregion

        #region Recording
        public void SegmentLog(bool skipFrame) {
            Mod.Log($"Segmenting log... (FrameNumber: {FrameNumber})");
            doSegmentRecording = true;
            skipFrameOnSegment = skipFrame;
        }

        public void LogPhysicsUpdate(Player player, Level level) {
            if (player == null) {
                if (LastPlayer == null) return;
                player = LastPlayer;
            }
            LastPlayer = player;

            if (player.Dead && Settings.SegmentOnDeath && !playerMarkedDead) {
                Mod.Log($"Player died and recording should segment!");
                playerMarkedDead = true;
                doSegmentRecording = true;
            } else if (!doSegmentRecording && !player.Dead) {
                playerMarkedDead = false;
            } else if (playerMarkedDead && player.Dead) {
                return;
            }


            bool logPhysics = Settings.IsRecording;
            if (logPhysics && !LastFrameEnabled) {
                //should log now, but didnt previously
                StartRecording();

            } else if (!logPhysics && LastFrameEnabled) {
                //previously logged, but shouldnt now
                StopRecording();
                return;

            } else if (logPhysics && LastFrameEnabled) {
                //should log now, and did previously
                //do nothing

                //Disables recording
                if (doSegmentRecording) {
                    Mod.Log($"Logged last frame and should continue, but should segment!");
                    Settings.IsRecording = false;
                    if (skipFrameOnSegment) {
                        return;
                    }
                }
            } else {
                //shouldnt log now, and didnt previously
                return;
            }

            SaveRoomLayout();

            Vector2 pos = player.ExactPosition;
            Vector2 speed = player.Speed;

            Vector2 velocity = Vector2.Zero;
            if (LastExactPos != Vector2.Zero) {
                velocity = pos - LastExactPos;
            }

            LastExactPos = pos;
            Vector2 liftboost = GetAdjustedLiftboost(player);
            float speedRetention = GetRetainedSpeed(player);
            FrameNumber++;

            if (RTAFrameOffset == -1) {
                RTAFrameOffset = Util.TicksToFrames(level.Session.Time);
            }
            long currentRTAFrames = Util.TicksToFrames(level.Session.Time); //Convert from ticks to frames

            int flipYFactor = Settings.FlipY ? -1 : 1;

            string toWrite = $"{FrameNumber},{currentRTAFrames - RTAFrameOffset + 1}";
            toWrite += $",{pos.X},{pos.Y * flipYFactor}";
            toWrite += $",{speed.X},{speed.Y * flipYFactor}";
            toWrite += $",{velocity.X},{velocity.Y * flipYFactor}";
            toWrite += $",{liftboost.X},{liftboost.Y * flipYFactor}";
            toWrite += $",{speedRetention}";
            toWrite += $",{player.Stamina}";
            toWrite += $",{string.Join(" ", GetPlayerFlags(player, level, IsFirstFrameInRoom))}";

            UpdateJumpState();
            toWrite += $",{GetInputsFormatted()}";

            if (MInput.GamePads.Length > 0 && Input.Aim.GamepadIndex >= 0) {
                Vector2 aim = MInput.GamePads[Input.Aim.GamepadIndex].CurrentState.ThumbSticks.Left;
                toWrite += $",{aim.X},{aim.Y}";
            } else {
                toWrite += $",-1,-1";
            }

            if (Mod.ModSettings.LogMovableEntities) {
                if (!IsFirstFrameInRoom) {
                    HashSet<int> entities = new HashSet<int>(); //Have this be empty, as we dont mind duplicates here
                    Dictionary<int, LoggedEntity> newRoomEntities  = GetEntitiesFromLevel(level, EntityList.Movable, ref entities);
                    
                    Dictionary<int, LoggedEntity> changes = new Dictionary<int, LoggedEntity>();
                    //Compare these entities to the ones in the room
                    foreach (KeyValuePair<int, LoggedEntity> pair in newRoomEntities) {
                        LoggedEntity entity = pair.Value;
                        if (RoomEntities.TryGetValue(entity.ID, out LoggedEntity roomEntity)) {
                            if (!entity.Position.Equals(roomEntity.Position) && entity.AttachedTo == -1) {
                                //Only note down the position difference as change
                                changes.Add(entity.ID, new LoggedEntity() {
                                    ID = entity.ID,
                                    Position = new JsonVector2() {
                                        X = entity.Position.X - roomEntity.Position.X,
                                        Y = entity.Position.Y - roomEntity.Position.Y
                                    },
                                });
                            }
                        } else {
                            //Entity wasnt found in the room
                            LoggedEntity changedEntity = new LoggedEntity(entity);
                            changedEntity.Properties.Add("added", true);
                            changes.Add(entity.ID, changedEntity);
                        }
                    }
                    
                    //Check for removed entities
                    foreach (KeyValuePair<int, LoggedEntity> roomEntity in RoomEntities) {
                        if (!newRoomEntities.ContainsKey(roomEntity.Key)) {
                            //Entity was removed
                            changes.Add(roomEntity.Key, new LoggedEntity() {
                                ID = roomEntity.Key,
                                Properties = new Dictionary<string, object>() {
                                    ["removed"] = true
                                }
                            });
                        }
                    }
                    
                    if (changes.Count > 0) {
                        toWrite += $",{JsonConvert.SerializeObject(changes)}";
                    } else {
                        toWrite += ",{}";
                    }
                    RoomEntities = newRoomEntities;
                } else {
                    toWrite += $",{JsonConvert.SerializeObject(RoomEntities)}";
                }
            } else {
                toWrite += ",{}";
            }
            
            IsFirstFrameInRoom = false;

            if (Settings.InputsToTasFile) {
                string tasInputs = GetInputsTASFormatted();
                if (tasInputs != TasInputs) {
                    //new input combination, write old one to file
                    TasFileContent += $"{TasFrameCount},{TasInputs}\n";
                    TasInputs = tasInputs;
                    TasFrameCount = 0;
                }
                TasFrameCount++;
            }

            RecordingsManager.LogWriter.WriteLine(toWrite);
        }

        public void StartRecording() {
            RecordingsManager.StartRecording();
            RecordingsManager.LogWriter.WriteLine(GetPhysicsLogHeader());
            LastFrameEnabled = true;

            TasFileContent = "";
            FrameNumber = 0;
            RTAFrameOffset = -1;
            TasInputs = GetInputsTASFormatted();

            VisitedRooms = new HashSet<string>();
            VisitedRoomsLayouts = new List<PhysicsLogRoomLayout>();
            LoggedEntitiesRaw = new HashSet<int>();
            IsFirstFrameInRoom = false;
            LastRoomName = null;

            RecordingStarted = DateTime.Now;
            RecordingStartedInSID = Mod.CurrentChapterStats.ChapterSID;
            RecordingStartedInMapBin = Mod.CurrentChapterStats.MapBin;
            RecordingStartedInChapterName = Mod.CurrentChapterStats.ChapterName;
            RecordingStartedInSideName = Mod.CurrentChapterStats.SideName;
        }

        public void StopRecording() {
            RecordingsManager.StopRecording();

            LastFrameEnabled = false;

            if (Settings.InputsToTasFile) {
                TextInput.SetClipboardText(TasFileContent);
                TasFileContent = "";
            }

            RecordingsManager.SaveRoomLayoutsToFile(VisitedRoomsLayouts, RecordingStartedInSID, RecordingStartedInMapBin, RecordingStartedInChapterName, RecordingStartedInSideName, RecordingStarted, FrameNumber);

            //Turns recording back on after storing segment
            if (doSegmentRecording) {
                doSegmentRecording = false;
                skipFrameOnSegment = false;
                Settings.IsRecording = true;
            }
        }
        #endregion
        
        #region Physics Log
        public string GetPhysicsLogHeader() {
            return "Frame,Frame (RTA),Position X,Position Y,Speed X,Speed Y,Velocity X,Velocity Y,LiftBoost X,LiftBoost Y,Retained,Stamina,Flags,Inputs,Analog X,Analog Y,Entities";
        }

        private Dictionary<int, string> PhysicsLogStatesToCheck = new Dictionary<int, string>() {
            [Player.StAttract] = nameof(Player.StAttract),
            [Player.StBoost] = nameof(Player.StBoost),
            [Player.StCassetteFly] = nameof(Player.StCassetteFly),
            [Player.StClimb] = nameof(Player.StClimb),
            [Player.StDash] = nameof(Player.StDash),
            [Player.StDreamDash] = nameof(Player.StDreamDash),
            [Player.StDummy] = nameof(Player.StDummy),
            [Player.StFlingBird] = nameof(Player.StFlingBird),
            [Player.StFrozen] = nameof(Player.StFrozen),
            [Player.StHitSquash] = nameof(Player.StHitSquash),
            [Player.StLaunch] = nameof(Player.StLaunch),
            [Player.StNormal] = nameof(Player.StNormal),
            [Player.StPickup] = nameof(Player.StPickup),
            [Player.StRedDash] = nameof(Player.StRedDash),
            [Player.StReflectionFall] = nameof(Player.StReflectionFall),
            [Player.StStarFly] = nameof(Player.StStarFly),
            [Player.StSummitLaunch] = nameof(Player.StSummitLaunch),
            [Player.StSwim] = nameof(Player.StSwim),
            [Player.StTempleFall] = nameof(Player.StTempleFall),
        };
        public List<string> GetPlayerFlags(Player player, Level level, bool firstFrameInRoom = false) {
            List<string> flags = new List<string>();
            if (player.Dead) {
                flags.Add("Dead");
            }
            if (PhysicsLogStatesToCheck.ContainsKey(player.StateMachine.State)) {
                flags.Add($"{PhysicsLogStatesToCheck[player.StateMachine.State]}");
            } else {
                flags.Add($"StOther");
            }

            if (Engine.FreezeTimer > 0) {
                flags.Add($"Frozen({Engine.FreezeTimer.ToCeilingFrames()})");
            }

            if (player.DashAttacking) {
                flags.Add($"DashAttack");
            }


            bool onGround = Util.GetPrivateProperty<bool>(player, "onGround");
            float jumpGraceTimer = Util.GetPrivateProperty<float>(player, "jumpGraceTimer");
            float varJumpTimer = Util.GetPrivateProperty<float>(player, "varJumpTimer");
            int forceMoveX = Util.GetPrivateProperty<int>(player, "forceMoveX");
            float forceMoveXTimer = Util.GetPrivateProperty<float>(player, "forceMoveXTimer");
            float dashCooldownTimer = Util.GetPrivateProperty<float>(player, "dashCooldownTimer");
            float wallSpeedRetained = Util.GetPrivateProperty<float>(player, "wallSpeedRetained");
            float wallSpeedRetentionTimer = Util.GetPrivateProperty<float>(player, "wallSpeedRetentionTimer");
            float maxFall = Util.GetPrivateProperty<float>(player, "maxFall");

            if (player.Ducking) {
                flags.Add("Ducking");
            }
            if (onGround) {
                flags.Add("OnGround");
            }

            if (player.InControl && !level.Transitioning) {
                if (dashCooldownTimer <= 0 && player.Dashes > 0) {
                    flags.Add("CanDash");
                } else if (dashCooldownTimer > 0) {
                    int frames = dashCooldownTimer.ToCeilingFrames() - 1; // We are 1f ahead of CelesteTAS
                    if (frames > 0) {
                        flags.Add($"DashCD({frames})");
                    }
                }

                int coyote = jumpGraceTimer.ToFloorFrames();
                if (coyote > 0) {
                    flags.Add($"Coyote({coyote})");
                }

                int jumpTimer = varJumpTimer.ToFloorFrames();
                if (jumpTimer > 0) {
                    flags.Add($"Jump({jumpTimer})");
                }

                if (player.StateMachine.State == Player.StNormal && (player.Speed.Y > 0f || (player.Holding != null && player.Holding.SlowFall == true))) {
                    flags.Add($"MaxFall({Math.Round(maxFall, 2)})");
                }

                int forceMoveXFrames = forceMoveXTimer.ToCeilingFrames();
                if (forceMoveXFrames > 0) {
                    string direction = forceMoveX > 0 ? "R" : forceMoveX < 0 ? "L" : "N";
                    flags.Add($"ForceMove{direction}({forceMoveXFrames})");
                }
            } else {
                flags.Add($"NoControl");
            }

            if (wallSpeedRetentionTimer > 0) {
                flags.Add($"Retained({wallSpeedRetentionTimer.ToCeilingFrames()})");
            }
            
            if (level.InCutscene) {
                flags.Add("Cutscene");
            }

            if (player.Holding == null && level.Tracker.GetComponents<Holdable>().Any(comp => {
                Holdable holdable = comp as Holdable;
                return holdable.Check(player);
            })) {
                flags.Add("Grab");
            }
            
            // Get private method from player: WallJumpCheck(int) -> bool
            try {
                MethodInfo method = typeof(Player).GetMethod("WallJumpCheck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null) {
                    // Check wall R first, then wall L
                    if ((bool)method.Invoke(player, new object[] { 1 })) {
                        flags.Add("Wall-R");
                    }
                    if ((bool)method.Invoke(player, new object[] { -1 })) {
                        flags.Add("Wall-L");
                    }
                }
            } catch (Exception) {
                // Don't log, it happens naturally in certain situations, like when the player is dead.
            }

            if (Settings.FlagDashes) {
                string maxDashesAddition = Settings.FlagMaxDashes ? $"/{player.MaxDashes}" : "";
                flags.Add($"Dashes({player.Dashes}{maxDashesAddition})");
            }
            if (Settings.FlagDashDir) {
                flags.Add($"DashDir({Math.Round(player.DashDir.X, 2)}|{Math.Round(player.DashDir.Y, 2)})");
            }
            if (Settings.FlagFacing) {
                string facingDir = player.Facing == Facings.Left ? "L" : "R";
                flags.Add($"Facing({facingDir})");
            }

            if (firstFrameInRoom) {
                flags.Add("FirstFrameInRoom");
            }

            return flags;
        }


        private bool PhysicsLogHeldJumpLastFrame = false;
        private bool PhysicsLogHoldingSecondJump = false;
        private void UpdateJumpState() {
            //Log($"Pre frame: Jump.Check -> {Input.Jump.Check}, Jump.Pressed -> {Input.Jump.Pressed}, Held Jump Last Frame -> {PhysicsLogHeldJumpLastFrame}, Holding Second Jump -> {PhysicsLogHoldingSecondJump}");
            if (Input.Jump.Check) {
                if (PhysicsLogHeldJumpLastFrame && Input.Jump.Pressed) {
                    PhysicsLogHoldingSecondJump = !PhysicsLogHoldingSecondJump;
                }
                PhysicsLogHeldJumpLastFrame = true;
            } else {
                PhysicsLogHeldJumpLastFrame = false;
                PhysicsLogHoldingSecondJump = false;
            }
        }

        public string GetInputsFormatted(char separator = ' ') {
            string inputs = "";

            if (Input.MoveX.Value != 0) {
                string rightleft = Input.MoveX.Value > 0 ? "R" : "L";
                inputs += $"{rightleft}{separator}";
            }
            if (Input.MoveY.Value != 0) {
                string updown = Input.MoveY.Value > 0 ? "D" : "U";
                inputs += $"{updown}{separator}";
            }

            if (Input.Jump.Check) {
                if (PhysicsLogHoldingSecondJump) {
                    inputs += $"K{separator}";
                } else {
                    inputs += $"J{separator}";
                }
            }

            if (Input.Dash.Check) {
                inputs += $"X{separator}";
            }
            if (Input.CrouchDash.Check) {
                inputs += $"Z{separator}";
            }
            if (Input.Grab.Check) {
                inputs += $"G{separator}";
            }

            return inputs.TrimEnd(separator);
        }
        public string GetInputsTASFormatted() {
            return GetInputsFormatted(',');
        }

        public Vector2 GetAdjustedLiftboost(Player player) {
            Vector2 liftBoost = new Vector2(player.LiftSpeed.X, player.LiftSpeed.Y);

            //liftboost can not be greater than 250 in x or 130 in y direction
            //also cannot be less than -250 in x or -130 in y direction

            if (liftBoost.X > 250) {
                liftBoost.X = 250;
            } else if (liftBoost.X < -250) {
                liftBoost.X = -250;
            }

            if (liftBoost.Y > 0) {
                liftBoost.Y = 0;
            } else if (liftBoost.Y < -130) {
                liftBoost.Y = -130;
            }

            return liftBoost;
        }

        public float GetRetainedSpeed(Player player) {
            float wallSpeedRetained = Util.GetPrivateProperty<float>(player, "wallSpeedRetained");
            float wallSpeedRetentionTimer = Util.GetPrivateProperty<float>(player, "wallSpeedRetentionTimer");

            if (wallSpeedRetentionTimer > 0) {
                return wallSpeedRetained;
            } else {
                return 0;
            }
        }
        #endregion
        
        #region Room Layout
        private HashSet<string> VisitedRooms;
        private List<PhysicsLogRoomLayout> VisitedRoomsLayouts;
        private HashSet<int> LoggedEntitiesRaw;

        private string LastRoomName = null;
        private bool IsFirstFrameInRoom = false;
        public void SaveRoomLayout() {
            if (!(Engine.Scene is Level)) return;
            Level level = (Level)Engine.Scene;
            
            string debugRoomName = level.Session.Level;
            //If we are in the same room as last frame
            if (LastRoomName != null && LastRoomName == debugRoomName) return;
            LastRoomName = debugRoomName;

            IsFirstFrameInRoom = true; //To notify physics logger that we are in a new room
            
            //Note down the current state of the entities in the room
            RoomEntities = GetEntitiesFromLevel(level, EntityList.Movable, ref LoggedEntitiesRaw);

            //Save the static entities in the room
            if (VisitedRooms.Contains(debugRoomName)) {
                Mod.Log($"Room '{debugRoomName}' has already been visited! (LastRoomName: {LastRoomName})");
                return;
            }
            VisitedRooms.Add(debugRoomName);

            Mod.Log($"Saving room layout for room '{debugRoomName}'");
            
            Dictionary<int, LoggedEntity> entities = GetEntitiesFromLevel(level, EntityList.Static, ref LoggedEntitiesRaw);
            
            int offsetX = level.LevelSolidOffset.X;
            int offsetY = level.LevelSolidOffset.Y;
            int width = level.Bounds.Width / 8;
            int height = level.Bounds.Height / 8;
            List<int[]> solidTileData = new List<int[]>();
            for (int y = 0; y < height; y++) {
                int[] row = new int[width];
                string line = "";
                for (int x = 0; x < width; x++) {
                    row[x] = level.SolidTiles.Grid.Data[x + offsetX, y + offsetY] ? 1 : 0;
                    line += row[x] == 1 ? "1" : " ";
                }
                solidTileData.Add(row);
            }
            
            PhysicsLogRoomLayout roomLayout = new PhysicsLogRoomLayout() {
                DebugRoomName = debugRoomName,
                LevelBounds = level.Bounds.ToJsonRectangle(),
                SolidTiles = solidTileData,
                Entities = entities,
            };
            VisitedRoomsLayouts.Add(roomLayout);

            Mod.Log($"Room layout saving done!");
        }
        #endregion

        public static Dictionary<int, LoggedEntity> GetEntitiesFromLevel(Level level, EntityList list, ref HashSet<int> loggedEntitiesRaw) {
            Dictionary<int, LoggedEntity> entities = new Dictionary<int, LoggedEntity>();
            foreach (Entity outerEntity in level.Entities) {
                List<Entity> subEntities = new List<Entity>() {
                    outerEntity
                };
                LoggedEntity loggedOuterEntity = null;

                for (int subIndex = 0; subIndex < subEntities.Count; subIndex++) {
                    Entity entity = subEntities[subIndex];
                    string entityName = entity.GetType().Name;
                    if (list == EntityList.Movable) {
                        if (subIndex == 0 && !IsMovableEntity(entityName)) continue;

                        if (entity is Platform platform) {
                            //Mod.Log($"'{entityName}' is a Platform! Checking static movers...");
                            List<StaticMover> staticMovers = Util.GetPrivateProperty<List<StaticMover>>(platform, "staticMovers");
                            if (staticMovers != null) {
                                foreach(StaticMover mover in staticMovers) {
                                    //Mod.Log($"Static mover: {mover.Entity.GetType().Name}");
                                    subEntities.Add(mover.Entity);
                                }
                            }
                        }
                    } else {
                        if (IsMovableEntity(entityName)) continue;
                        StaticMover entityMover = entity.Components.Get<StaticMover>(); //Entity is riding another entity
                        //When the entitiy has a mover, is attached to a solid AND the solid allows moving, consider the entity as movable
                        if (entityMover != null && entityMover.Platform != null && entityMover.Platform is Solid solid && solid.AllowStaticMovers
                            && entityMover.Platform.GetType().IsSubclassOf(typeof(Solid))) {
                            continue;
                        }
                        
                        //Entities can also ride non-moving platforms
                        // if (entity is Platform platform) {
                        //     //Mod.Log($"'{entityName}' is a Platform! Checking static movers...");
                        //     List<StaticMover> staticMovers = Util.GetPrivateProperty<List<StaticMover>>(platform, "staticMovers");
                        //     if (staticMovers != null) {
                        //         foreach(StaticMover mover in staticMovers) {
                        //             //Mod.Log($"Static mover: {mover.Entity.GetType().Name}");
                        //             subEntities.Add(mover.Entity);
                        //         }
                        //     }
                        // }
                    }

                    int entityHash = entity.GetHashCode();
                    if (loggedEntitiesRaw.Contains(entityHash)) continue;
                    loggedEntitiesRaw.Add(entityHash);

                    LoggedEntity loggedEntity = new LoggedEntity() {
                        Type = entityName,
                        Position = entity.Position.ToJsonVector2(),
                    };
                    if (loggedOuterEntity == null) loggedOuterEntity = loggedEntity;
                    Collider collider = null;
                    bool logged = false;

                    if (EntityNamesToTest.Contains(entityName)) {
                        Util.GetPrivateProperty<object>(entity, "a");
                    }

                    if (IsSpinnerEntity(entityName)) {
                        if (entity.Collider is ColliderList colliderList && colliderList.colliders.Length == 2) {
                            bool boxCorrect = false, circleCorrect = false;
                            foreach (Collider spinnerCollider in colliderList.colliders) {
                                if (spinnerCollider is Hitbox hitbox) {
                                    if (hitbox.Position.X == -8 && hitbox.Position.Y == -3 && hitbox.Width == 16 &&
                                        hitbox.Height == 4) {
                                        boxCorrect = true;
                                    }
                                } else if (spinnerCollider is Circle circle) {
                                    if (circle.Position.X == 0 && circle.Position.Y == 0 && circle.Radius == 6) {
                                        circleCorrect = true;
                                    }
                                }
                            }
                            
                            //If the spinner is not exactly default properties, log the collider list
                            if (!boxCorrect || !circleCorrect) {
                                collider = colliderList;
                            }
                        }
                        
                        logged = true;
                    }

                    //Hitbox entities
                    if (EntityNamesHitboxColliders.Contains(entityName) || entityName == "Solid") {
                        if (entity.Collider == null) {
                            Mod.Log($"Entity '{entityName}' has no collider!");
                            continue;
                        }
                        if (entity.Collider is Hitbox == false) {
                            Mod.Log($"Entity '{entityName}' has a collider that is not a Hitbox!");
                            continue;
                        }

                        collider = entity.Collider;

                        //Optional properties
                        if (entityName == "Strawberry") {
                            Strawberry strawberry = entity as Strawberry;
                            loggedEntity.Properties.Add("golden", strawberry.Golden);
                        }

                        if (entityName == "Refill" || entityName == "RefillWall") {
                            bool twoDashes = Util.GetPrivateProperty<bool>(entity, "twoDashes");
                            bool oneUse = Util.GetPrivateProperty<bool>(entity, "oneUse");

                            loggedEntity.Properties.Add("twoDashes", twoDashes);
                            loggedEntity.Properties.Add("oneUse", oneUse);
                        }

                        if (entityName == "MoveBlock" || entityName == "VitMoveBlock") {
                            MoveBlock.Directions direction = Util.GetPrivateProperty<MoveBlock.Directions>(entity, "direction");
                            loggedEntity.Properties.Add("direction", direction.ToString());
                        }
                        if (entityName == "DreamMoveBlock") {
                            MoveBlock.Directions direction = Util.GetPrivateProperty<MoveBlock.Directions>(entity, "Direction", isPublic: true);
                            loggedEntity.Properties.Add("direction", direction.ToString());
                        }

                        if (entityName == "Cloud") {
                            bool fragile = Util.GetPrivateProperty<bool>(entity, "fragile");
                            loggedEntity.Properties.Add("fragile", fragile);
                        }

                        if (entityName == "ClutterBlockBase") {
                            ClutterBlockBase clutterBlockBase = entity as ClutterBlockBase;
                            ClutterBlock.Colors color = clutterBlockBase.BlockColor;
                            loggedEntity.Properties.Add("color", color.ToString());
                        }

                        if (entityName == "ClutterSwitch") {
                            ClutterSwitch clutterSwitch = entity as ClutterSwitch;
                            ClutterBlock.Colors color = Util.GetPrivateProperty<ClutterBlock.Colors>(clutterSwitch, "color");
                            loggedEntity.Properties.Add("color", color.ToString());
                        }

                        if (entityName == "CassetteBlock") {
                            Color color = Util.GetPrivateProperty<Color>(entity, "color");
                            //bool isActive = (entity as CassetteBlock).Activated;
                            //collider = new Hitbox(collider.Width, collider.Height, collider.Position.X, collider.Position.Y - (isActive ? 2 : 0));
                            loggedEntity.Properties.Add("color", color.ToHex());
                        }
                        if (entityName == "WonkyCassetteBlock") {
                            string textureDir = Util.GetPrivateProperty<object>(entity, "textureDir").ToString();
                            char cassetteType = textureDir.Last();
                        
                            Mod.Log($"WonkyCassetteBlock type: {cassetteType}");

                            string color = "#a2babc";
                            if (cassetteType == 'A') {
                                color = "#3d73be";
                            } else if (cassetteType == 'B') {
                                color = "#73324f";
                            } else if (cassetteType == 'C') {
                                color = "#c08362";
                            } else if (cassetteType == 'D') {
                                color = "#346157";
                            }

                            loggedEntity.Properties.Add("color", color);
                        }

                        if (entityName == "MovingTouchSwitch") {
                            Vector2[] nodes = Util.GetPrivateProperty<Vector2[]>(entity, "touchSwitchNodes");
                            List<JsonVector2> jsonNodes = new List<JsonVector2>();
                            foreach (Vector2 node in nodes) {
                                jsonNodes.Add(node.ToJsonVector2());
                            }
                            loggedEntity.Properties.Add("nodes", jsonNodes);
                        }

                        logged = true;
                    }


                    //Hitcircle entities
                    if (EntityNamesHitcircleColliders.Contains(entityName)) {
                        if (entity.Collider == null) {
                            Mod.Log($"Entity '{entityName}' has no collider!");
                            continue;
                        }
                        if (entity.Collider is Circle == false) {
                            Mod.Log($"Entity '{entityName}' has a collider that is not a Circle!");
                            continue;
                        }

                        collider = entity.Collider;

                        //Optional properties
                        if (entityName == "Booster") {
                            Booster booster = entity as Booster;
                            bool red = Util.GetPrivateProperty<bool>(booster, "red");
                            loggedEntity.Properties.Add("red", red);
                        }
                        if (entityName == "StaticBumper") {
                            //public field: NotCoreMode
                            bool notCoreMode = Util.GetPrivateProperty<bool>(entity, "NotCoreMode", isPublic: true);
                            loggedEntity.Properties.Add("notCoreMode", notCoreMode);
                        }
                        if (entityName == "VortexBumper") {
                            //Interesting properties: fireMode, twoDashes, oneUse, deadly, notCoreMode
                            bool twoDashes = Util.GetPrivateProperty<bool>(entity, "twoDashes");
                            bool oneUse = Util.GetPrivateProperty<bool>(entity, "oneUse");
                            bool deadly = Util.GetPrivateProperty<bool>(entity, "deadly");
                            bool notCoreMode = Util.GetPrivateProperty<bool>(entity, "notCoreMode");

                            loggedEntity.Properties.Add("twoDashes", twoDashes);
                            loggedEntity.Properties.Add("oneUse", oneUse);
                            loggedEntity.Properties.Add("deadly", deadly);
                            loggedEntity.Properties.Add("notCoreMode", notCoreMode);
                        }

                        entities.Add(entityHash, loggedEntity);
                        logged = true;
                    }

                    //Other entities
                    if (EntityNamesOther.Contains(entityName)) {
                        if (entityName == "ConnectedMoveBlock") {
                            MoveBlock.Directions direction = Util.GetPrivateProperty<MoveBlock.Directions>(entity, "Direction", isPublic: true);
                            loggedEntity.Properties.Add("direction", direction.ToString());

                            Hitbox[] colliders = Util.GetPrivateProperty<Hitbox[]>(entity, "Colliders", isPublic: true);
                            if (colliders != null && colliders.Length > 0) {
                                collider = colliders[0];
                            }
                        } else if (entityName == "AngryOshiro") {
                            AngryOshiro oshiro = entity as AngryOshiro;
                            PlayerCollider playerCollider = Util.GetPrivateProperty<PlayerCollider>(oshiro, "bounceCollider");
                            if (playerCollider != null) {
                                ColliderList cl = new ColliderList(oshiro.Collider, playerCollider.Collider);
                                collider = cl;
                            } else {
                                collider = oshiro.Collider;
                            }

                            loggedEntity.Properties.Add("bs", false);
                            loggedEntity.Properties.Add("bc", true);
                        }

                        logged = true;
                    }
                    

                    //Glider, TheoCrystal
                    if (entityName == "Glider" || entityName == "CustomGlider" || entityName == "RespawningJellyfish" || entityName == "TheoCrystal" || entityName == "CrystalBomb") {
                        Holdable hold = null;

                        if (entityName == "Glider")
                            hold = (entity as Glider).Hold;
                        else if (entityName == "TheoCrystal")
                            hold = (entity as TheoCrystal).Hold;

                        if (hold == null) {
                            hold = Util.GetPrivateProperty<Holdable>(entity, "Hold", isPublic: true);
                        }

                        if (hold == null) {
                            collider = Util.GetPrivateProperty<Collider>(entity, "hitBox");
                            if (collider == null && entity.Collider != null) {
                                collider = entity.Collider;
                            }
                        } else {
                            collider = hold.PickupCollider;
                        }

                        if (collider == null) {
                            Mod.Log($"Holdable entity '{entityName}' has no collider!");
                            continue;
                        }
                        logged = true;
                    }

                    //FinalBoss, BadelineBoost, FlingBird
                    if (entityName == "FinalBoss" || entityName == "BadelineBoost" || entityName == "FlingBird") {
                        List<JsonVector2> nodes = new List<JsonVector2>();
                        Vector2[] nodesEntity = null;
                        int startIndex = 1;

                        if (entityName == "FinalBoss") {
                            FinalBoss boss = entity as FinalBoss;
                            nodesEntity = Util.GetPrivateProperty<Vector2[]>(boss, "nodes");

                        } else if (entityName == "BadelineBoost") {
                            BadelineBoost boost = entity as BadelineBoost;
                            nodesEntity = Util.GetPrivateProperty<Vector2[]>(boost, "nodes");

                        } else if (entityName == "FlingBird") {
                            FlingBird bird = entity as FlingBird;
                            List<Vector2> allNodes = new List<Vector2>();
                            for (int i = 0; i < bird.NodeSegments.Count; i++) {
                                Vector2[] segment = bird.NodeSegments[i];
                                allNodes.Add(segment[0]);
                            }
                            nodesEntity = allNodes.ToArray();
                        }

                        for (int i = startIndex; i < nodesEntity.Length; i++) {
                            Vector2 node = nodesEntity[i];
                            nodes.Add(node.ToJsonVector2());
                        }

                        collider = entity.Collider;

                        loggedEntity.Properties.Add("nodes", nodes);
                        logged = true;
                    }

                    if (IsIgnoredEntity(entityName)) {
                        continue;
                    }
                    
                    if (!logged) {
                        AddOtherInfoToLoggedEntity(loggedEntity, entity);
                    } else if(collider != null) {
                        AddColliderInfoToLoggedEntity(loggedEntity, collider);
                    }

                    loggedEntity.ID = entityHash;
                    if (subIndex > 0) {
                        loggedEntity.AttachedTo = loggedOuterEntity.ID;
                    }

                    if (!entities.ContainsKey(entityHash)) {
                        entities.Add(entityHash, loggedEntity);
                    }
                }
            }

            return entities;
        }

        #region Util
        public static void AddColliderInfoToLoggedEntity(LoggedEntity loggedEntity, Collider collider) {
            if (collider == null) {
                Mod.Log($"Entity '{loggedEntity.Type}' has no collider set!");
                return;
            }

            if (collider is Hitbox) {
                loggedEntity.Properties.Add("b", GetBasicColliderInfo(collider));
            } else if (collider is Circle) {
                loggedEntity.Properties.Add("c", GetBasicColliderInfo(collider));
            } else if (collider is ColliderList) {
                List<object> colliderList = new List<object>();
                
                ColliderList entityColliders = collider as ColliderList;
                foreach (Collider actualCollider in entityColliders.colliders) {
                    if (actualCollider is Hitbox) {
                        colliderList.Add(new JsonColliderHitbox() {
                            Hitbox = (JsonRectangle) GetBasicColliderInfo(actualCollider),
                        });
                    } else if (actualCollider is Circle) {
                        colliderList.Add(new JsonColliderCircle() {
                            HitCircle = (JsonCircle) GetBasicColliderInfo(actualCollider),
                        });
                    }
                }

                loggedEntity.Properties.Add("cl", colliderList);
            }
        }

        private static object GetBasicColliderInfo(Collider collider) {
            if (collider == null) return null;
            
            if (collider is Hitbox) {
                Hitbox hitbox = collider as Hitbox;
                return new JsonRectangle() {
                    X = hitbox.Position.X,
                    Y = hitbox.Position.Y,
                    Width = hitbox.Width,
                    Height = hitbox.Height,
                };
            } else if (collider is Circle) {
                Circle hitcircle = collider as Circle;
                return new JsonCircle() {
                    X = hitcircle.Position.X,
                    Y = hitcircle.Position.Y,
                    Radius = hitcircle.Radius,
                };
            }

            return null;
        }

        public static void AddOtherInfoToLoggedEntity(LoggedEntity loggedEntity, Entity entity) {
            loggedEntity.Properties.Add("bs", entity is Solid);
            loggedEntity.Properties.Add("bc", entity.Collider != null);
            if (entity.Collider != null) {
                AddColliderInfoToLoggedEntity(loggedEntity, entity.Collider);
            }
        }

        public static bool IsMovableEntity(string name) {
            return EntityNamesMovables.Contains(name) || CustomEntityNamesMovables.Contains(name);
        }
        public static bool IsSpinnerEntity(string name) {
            return EntityNamesSpinners.Contains(name);
        }
        public static bool IsIgnoredEntity(string name) {
            return IgnoreEntityNames.Contains(name) || CustomIgnoredEntityNames.Contains(name);
        }
        #endregion
    }
}