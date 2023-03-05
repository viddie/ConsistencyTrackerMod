﻿using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.PhysicsLog
{
    public class PhysicsLogger {

        public enum FileType { 
            PhysicsLog,
            Layout
        }

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        private static ConsistencyTrackerSettings ModSettings => Mod.ModSettings;
        public static bool IsInRecording => ModSettings.LogPhysicsEnabled;
        
        private int MaxLogFiles => 10;
        private static string FolderName => ConsistencyTrackerModule.LogsFolder;
        private static readonly string LogFileName = "position-log.txt";
        private static readonly string LayoutFileName = "room-layout.json";

        private DateTime RecordingStarted;
        private string RecordingStartedInChapterName;
        private string RecordingStartedInSideName;
        private Vector2 LastExactPos = Vector2.Zero;
        private bool LastFrameEnabled = false;
        private StreamWriter LogWriter = null;
        private int FrameNumber = -1;
        private bool LogPosition, LogSpeed, LogVelocity, LogLiftBoost, LogSpeedRetention, LogStamina, LogFlags, LogInputs;
        private Player LastPlayer = null;

        private int TasFrameCount = 0;
        private string TasInputs = null;
        private string TasFileContent = null;

        private bool IsFirstWriteLayout;

        public void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime) {
            orig(self, gameTime);

            if (Engine.Scene is Level level) {
                Player player = level.Tracker.GetEntity<Player>();
                LogPhysicsUpdate(player, level);
            }
        }

        private bool playerMarkedDead = false;
        private bool doSegmentRecording = false;
        private bool skipFrameOnSegment = false;
        public void SegmentLog(bool skipFrame) {
            doSegmentRecording = true;
            skipFrameOnSegment = skipFrame;
        }
        
        public void LogPhysicsUpdate(Player player, Level level) {
            if (player == null) {
                if (LastPlayer == null) return;
                player = LastPlayer;
            }
            LastPlayer = player;

            if (player.Dead && ModSettings.LogSegmentOnDeath && !playerMarkedDead) {
                playerMarkedDead = true;
                doSegmentRecording = true;
            } else if (!doSegmentRecording && !player.Dead) {
                playerMarkedDead = false;
            } else if (playerMarkedDead && player.Dead) {
                return;
            }


            bool logPhysics = ModSettings.LogPhysicsEnabled;
            if (logPhysics && !LastFrameEnabled) {
                //should log now, but didnt previously
                LogPosition = ModSettings.LogPosition;
                LogSpeed = ModSettings.LogSpeed;
                LogVelocity = ModSettings.LogVelocity;
                LogLiftBoost = ModSettings.LogLiftBoost;
                LogSpeedRetention = ModSettings.LogSpeedRetention;
                LogStamina = ModSettings.LogStamina;
                LogFlags = ModSettings.LogFlags;
                LogInputs = ModSettings.LogInputs;

                IsFirstWriteLayout = true;
                ShiftFiles(FileType.PhysicsLog);

                LogWriter = new StreamWriter(GetFilePath(FileType.PhysicsLog, 0));
                LogWriter.WriteLine(GetPhysicsLogHeader(LogPosition, LogSpeed, LogVelocity, LogLiftBoost, LogSpeedRetention, LogStamina, LogFlags, LogInputs));
                LastFrameEnabled = logPhysics;

                TasFileContent = "";
                FrameNumber = 0;
                TasInputs = GetInputsTASFormatted();

                VisitedRooms = new HashSet<string>();
                VisitedRoomsLayouts = new List<PhysicsLogRoomLayout>();
                LoggedEntitiesRaw = new HashSet<Entity>();

                RecordingStarted = DateTime.Now;
                RecordingStartedInChapterName = Mod.CurrentChapterStats.ChapterName;
                RecordingStartedInSideName = Mod.CurrentChapterStats.SideName;

                //Type playerType = player.GetType();
                //Mod.Log($"Fields of player: [{string.Join(", ", playerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}]");
                //Mod.Log($"Properties of player: [{string.Join(", ", playerType.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}]");

            } else if (!logPhysics && LastFrameEnabled) {
                //previously logged, but shouldnt now
                //close log file writer
                LogWriter.Close();
                LogWriter.Dispose();
                LogWriter = null;
                LastFrameEnabled = logPhysics;

                if (ModSettings.LogPhysicsInputsToTasFile) {
                    TextInput.SetClipboardText(TasFileContent);
                    TasFileContent = "";
                }

                SaveRoomLayoutsToFile(VisitedRoomsLayouts);

                //Turns recording back on after storing segment
                if (doSegmentRecording) {
                    doSegmentRecording = false;
                    skipFrameOnSegment = false;
                    ModSettings.LogPhysicsEnabled = true;
                }

                return;

            } else if (logPhysics && LastFrameEnabled) {
                //should log now, and did previously
                //do nothing

                //Disables recording
                if (doSegmentRecording) {
                    ModSettings.LogPhysicsEnabled = false;
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

            int flipYFactor = ModSettings.LogPhysicsFlipY ? -1 : 1;

            string toWrite = $"{FrameNumber}";
            if (LogPosition) {
                toWrite += $",{pos.X},{pos.Y * flipYFactor}";
            }
            if (LogSpeed) {
                toWrite += $",{speed.X},{speed.Y * flipYFactor}";
            }
            if (LogVelocity) {
                toWrite += $",{velocity.X},{velocity.Y * flipYFactor}";
            }
            if (LogLiftBoost) {
                toWrite += $",{liftboost.X},{liftboost.Y * flipYFactor}";
            }
            if (LogSpeedRetention) {
                toWrite += $",{speedRetention}";
            }
            if (LogStamina) {
                toWrite += $",{player.Stamina}";
            }
            if (LogFlags) {
                toWrite += $",{string.Join(" ", GetPlayerFlags(player, level))}";
            }


            UpdateJumpState();

            if (LogInputs) {
                toWrite += $",{GetInputsFormatted()}";
            }

            if (ModSettings.LogPhysicsInputsToTasFile) {
                string tasInputs = GetInputsTASFormatted();
                if (tasInputs != TasInputs) {
                    //new input combination, write old one to file
                    TasFileContent += $"{TasFrameCount},{TasInputs}\n";
                    TasInputs = tasInputs;
                    TasFrameCount = 0;
                }
                TasFrameCount++;
            }


            LogWriter.WriteLine(toWrite);
        }

        public string GetPhysicsLogHeader(bool position, bool speed, bool velocity, bool liftBoost, bool speedRetention, bool stamina, bool flags, bool inputs) {
            string header = "Frame";
            if (position) {
                header += ",Position X,Position Y";
            }
            if (speed) {
                header += ",Speed X,Speed Y";
            }
            if (velocity) {
                header += ",Velocity X,Velocity Y";
            }
            if (liftBoost) {
                header += ",LiftBoost X,LiftBoost Y";
            }
            if (speedRetention) {
                header += ",Retained";
            }
            if (stamina) {
                header += ",Stamina";
            }
            if (flags) {
                header += ",Flags";
            }
            if (inputs) {
                header += ",Inputs";
            }
            return header;
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
        public List<string> GetPlayerFlags(Player player, Level level) {
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

            

            bool onGround = Util.GetPrivateProperty<bool>(player, "onGround");
            float jumpGraceTimer = Util.GetPrivateProperty<float>(player, "jumpGraceTimer");
            float varJumpTimer = Util.GetPrivateProperty<float>(player, "varJumpTimer");
            int forceMoveX = Util.GetPrivateProperty<int>(player, "forceMoveX");
            float forceMoveXTimer = Util.GetPrivateProperty<float>(player, "forceMoveXTimer");
            float dashCooldownTimer = Util.GetPrivateProperty<float>(player, "dashCooldownTimer");
            float wallSpeedRetained = Util.GetPrivateProperty<float>(player, "wallSpeedRetained");
            float wallSpeedRetentionTimer = Util.GetPrivateProperty<float>(player, "wallSpeedRetentionTimer");
            float maxFall = Util.GetPrivateProperty<float>(player, "maxFall");

            //Log($"Selected player fields: onGround '{onGround}', jumpGraceTimer '{jumpGraceTimer}', forceMoveX '{forceMoveX}', forceMoveXTimer '{forceMoveXTimer}', dashCooldownTimer '{dashCooldownTimer}', " +
            //        $"wallSpeedRetained '{wallSpeedRetained}', wallSpeedRetentionTimer '{wallSpeedRetentionTimer}', maxFall '{maxFall}', Stamina '{player.Stamina}'");

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
                    flags.Add($"DashCD({dashCooldownTimer.ToCeilingFrames()})");
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

            //if (player.LiftSpeedGraceTime > 0) {
            //    flags.Add($"LiftBoost({player.LiftSpeedGraceTime.ToCeilingFrames()})");
            //}

            if (level.InCutscene) {
                flags.Add("Cutscene");
            }

            if (player.Holding == null && level.Tracker.GetComponents<Holdable>().Any(comp => {
                Holdable holdable = comp as Holdable;
                return holdable.Check(player);
            })) {
                flags.Add("Grab");
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

        private HashSet<string> VisitedRooms;
        private List<PhysicsLogRoomLayout> VisitedRoomsLayouts;
        private HashSet<Entity> LoggedEntitiesRaw;

        private readonly List<string> EntityNamesOnlyPosition = new List<string>() {
            "CrystalStaticSpinner",
            "DustStaticSpinner",
            "CustomSpinner",
        };
        private readonly List<string> EntityNamesHitboxColliders = new List<string>() {
            "Spikes",
            "TriggerSpikes", "GroupedTriggerSpikes", "GroupedDustTriggerSpikes", "TriggerSpikesOriginal",
            "Lightning",

            "Refill", "CustomRefill",
            "Spring", "CustomSpring", "DashSpring",
            "DreamBlock",
            "SwapBlock", "ToggleSwapBlock",
            "ZipMover", "LinkedZipMover", "LinkedZipMoverNoReturn",
            "TouchSwitch", "SwitchGate", "FlagTouchSwitch", "FlagSwitchGate", "MovingTouchSwitch",
            "BounceBlock", //Core Block
            "CrushBlock", //Kevin
            "DashBlock",
            "DashSwitch", "TempleGate",
            "Glider", "RespawningJellyfish",
            "SeekerBarrier", "CrystalBombDetonator",
            "TempleCrackedBlock",
            "FlyFeather",
            "Cloud",
            "WallBooster", "IcyFloor", //Conveyers or IceWalls, depending on Core Mode
            "MoveBlock", "DreamMoveBlock",
            "CassetteBlock", "WonkyCassetteBlock",

            "Puffer", "StaticPuffer",

            "FallingBlock", "GroupedFallingBlock", "RisingBlock",
            "JumpThru", "SidewaysJumpThru", "AttachedJumpThru", "JumpthruPlatform",
            "CrumblePlatform", "CrumbleBlock", "CrumbleBlockOnTouch", "VariableCrumblePlatform",
            "FloatySpaceBlock", "FloatierSpaceBlock",
            "ClutterBlockBase", "ClutterDoor", "ClutterSwitch",
            "Key", "LockBlock",
            "StarJumpBlock",

            "Strawberry",
            "SilverBerry",

            "Lookout", //Binos
            "LightningBreakerBox",

            "Killbox",

            //Modded Entities
            "Portal",
        };
        private readonly List<string> EntityNamesHitcircleColliders = new List<string>() {
            "Booster",
            "Bumper",
            "Shield",
        };
        private readonly List<string> EntityNamesOther = new List<string>() {
            "ConnectedMoveBlock",
        };
        private readonly List<string> IgnoreEntityNames = new List<string>() {
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
            "TextOverlay",
            "TalkComponentUI",
            "DeathDisplay",
            "DashSequenceDisplay",

            "Decal",
            "SolidTiles",
            "ParticleSystem",
            "FloatingDebris",
            "BackgroundTiles", "BGTilesRenderer",
            "GlassBlockBg",
            "MirrorSurfaces",
            "WaterSurface", "WaterFloatingObject",
            "ColoredWater", "ColoredWaterfall",
            "DustEdges",
            "ParticleEmitter",
            "FormationBackdrop",
            "CustomHangingLamp",
            "ResortLantern",
            "StrawberryJamJar",
            "StaticDoor",
            "CustomFlagline",
            "ConfettiTrigger",
            "LightOccludeBlock",
            "Moth",

            "CameraTargetTrigger", "CameraOffsetBorder", "CameraOffsetTrigger",
            "SmoothCameraOffsetTrigger", "InstantLockingCameraTrigger", "CameraHitboxEntity",
            "LookoutBlocker", "CameraAdvanceTargetTrigger",

            "FlagTrigger",
            "TeleportationTrigger", "TeleportationTarget",
            "ChangeRespawnTrigger", "SpawnFacingTrigger",

            "StylegroundMask",
            "BloomFadeTrigger", "LightFadeTrigger", "BloomStrengthTrigger", "SetBloomStrengthTrigger", "SetBloomBaseTrigger", "SetDarknessAlphaTrigger",
            "MadelineSpotlightModifierTrigger", "FlashTrigger", "AlphaLerpLightSource", "ColorLerpLightSource", "BloomMask", "MadelineSilhouetteTrigger",
            "ColorGradeFadeTrigger", "EditDepthTrigger", "FlashlightColorTrigger",

            "MusicParamTrigger",

            "LaserDetectorManager",
            "UnderwaterSwitchController",
            "WindController",
            "RainbowSpinnerColorController",
            "TimeController",
            "SeekerEffectsController", "SeekerBarrierRenderer",
            "StylegroundFadeController",

            "PathRenderer",
            "LightningRenderer",
            "DreamSpinnerRenderer", "DreamTunnelRenderer", "DreamTunnelEntryRenderer", "DreamJellyfishRenderer",
            "MoveBlockBarrierRenderer", "PlayerSeekerBarrierRenderer", "PufferBarrierRenderer",
            "CrystalBombDetonatorRenderer", "CrystalBombFieldRenderer",
            "FlagKillBarrierRenderer",
            "DecalContainerRenderer",

            "TriggerTrigger",
            "Border",
            "SlashFx",
            "Snapshot",
            "Entity", "HelperEntity",
        };

        public void SaveRoomLayout() {
            if (!(Engine.Scene is Level)) return;
            Level level = (Level)Engine.Scene;

            string debugRoomName = level.Session.Level;
            if (VisitedRooms.Contains(debugRoomName)) return;
            VisitedRooms.Add(debugRoomName);

            Mod.Log($"Saving room layout for room '{debugRoomName}'");

            string path = ConsistencyTrackerModule.GetPathToFile($"{ConsistencyTrackerModule.LogsFolder}/RoomLayout.txt");

            File.WriteAllText(path, $"Room Name '{level.Session.Level}'\n");
            File.AppendAllText(path, $"level.Bounds: {level.Bounds}\n");
            File.AppendAllText(path, $"level.TileBounds: {level.TileBounds}\n");
            File.AppendAllText(path, $"level.LevelOffset: {level.LevelOffset}\n");
            File.AppendAllText(path, $"level.LevelSolidOffset: {level.LevelSolidOffset}\n");
            File.AppendAllText(path, $"level.Entities: \n{string.Join("\n", level.Entities.Select((e) => $"{e.GetType().Name} ({e.Position})"))}\n");

            File.AppendAllText(path, $"\n");

            File.AppendAllText(path, $"level.SolidTiles.Grid.Size: {level.SolidTiles.Grid.Size}\n");
            File.AppendAllText(path, $"level.SolidTiles.Grid.CellsX: {level.SolidTiles.Grid.CellsX}\n");
            File.AppendAllText(path, $"level.SolidTiles.Grid.CellsY: {level.SolidTiles.Grid.CellsY}\n");
            File.AppendAllText(path, $"\nlevel.SolidTiles.Grid.Data:\n");

            
            //Draw only our level
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
                File.AppendAllText(path, $"{y}: {line}\n");
            }

            List<LoggedEntity> entities = new List<LoggedEntity>();
            List<LoggedEntity> otherEntities = new List<LoggedEntity>();
            foreach (Entity entity in level.Entities) {
                if (LoggedEntitiesRaw.Contains(entity)) continue;
                LoggedEntitiesRaw.Add(entity);

                string entityName = entity.GetType().Name;
                LoggedEntity loggedEntity = new LoggedEntity() {
                    Type = entityName,
                    Position = entity.Position.ToJsonVector2(),
                };
                Collider collider = null;
                bool logged = false;

                if (entityName == "ChapterPanelTrigger") {
                    Util.GetPrivateProperty<object>(entity, "asd");
                }

                if (EntityNamesOnlyPosition.Contains(entityName)) {
                    collider = entity.Collider;
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

                    if (entityName == "Refill") {
                        Refill refill = entity as Refill;

                        bool twoDashes = Util.GetPrivateProperty<bool>(refill, "twoDashes");
                        bool oneUse = Util.GetPrivateProperty<bool>(refill, "oneUse");

                        loggedEntity.Properties.Add("twoDashes", twoDashes);
                        loggedEntity.Properties.Add("oneUse", oneUse);

                    }
                    if (entityName == "CustomRefill") {
                        Util.GetPrivateProperty<object>(entity, "asd");
                    }

                    if (entityName == "MoveBlock") {
                        MoveBlock moveBlock = entity as MoveBlock;
                        MoveBlock.Directions direction = Util.GetPrivateProperty<MoveBlock.Directions>(moveBlock, "direction");
                        loggedEntity.Properties.Add("direction", direction.ToString());
                    }
                    if (entityName == "DreamMoveBlock") {
                        MoveBlock.Directions direction = Util.GetPrivateProperty<MoveBlock.Directions>(entity, "Direction", isPublic: true);
                        loggedEntity.Properties.Add("direction", direction.ToString());
                    }

                    if (entityName == "Cloud") {
                        Cloud cloud = entity as Cloud;
                        bool fragile = Util.GetPrivateProperty<bool>(cloud, "fragile");
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
                        CassetteBlock cassetteBlock = entity as CassetteBlock;
                        Color color = Util.GetPrivateProperty<Color>(cassetteBlock, "color");
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

                //CassetteBlock

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

                    entities.Add(loggedEntity);
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
                    }

                    logged = true;
                }



                //Glider, TheoCrystal
                if (entityName == "Glider" || entityName == "RespawningJellyfish" || entityName == "TheoCrystal" || entityName == "CrystalBomb") {
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

                if (logged) {
                    AddColliderInfoToLoggedEntity(loggedEntity, collider);
                    entities.Add(loggedEntity);

                } else if (IgnoreEntityNames.Contains(entityName) == false) {
                    otherEntities.Add(loggedEntity);
                }
            }

            string pathJson = ConsistencyTrackerModule.GetPathToFile($"{ConsistencyTrackerModule.LogsFolder}/room-layout.json");
            PhysicsLogRoomLayout roomLayout = new PhysicsLogRoomLayout() {
                DebugRoomName = debugRoomName,
                LevelBounds = level.Bounds.ToJsonRectangle(),
                SolidTiles = solidTileData,
                Entities = entities,
                OtherEntities = otherEntities,
            };
            VisitedRoomsLayouts.Add(roomLayout);

            //File.WriteAllText(pathJson, JsonConvert.SerializeObject(VisitedRoomsLayouts));
            //SaveRoomLayoutsToFile(VisitedRoomsLayouts);

            Mod.Log($"Room layout saving done!");
        }


        private void SaveRoomLayoutsToFile(List<PhysicsLogRoomLayout> rooms) {
            if (IsFirstWriteLayout) {
                IsFirstWriteLayout = false;
                ShiftFiles(FileType.Layout);
            }

            string pathJson = GetFilePath(FileType.Layout, 0);

            PhysicsLogLayoutFile file = new PhysicsLogLayoutFile() {
                ChapterName = RecordingStartedInChapterName,
                SideName = RecordingStartedInSideName,
                RecordingStarted = RecordingStarted,
                FrameCount = FrameNumber,
                Rooms = rooms,
            };

            File.WriteAllText(pathJson, JsonConvert.SerializeObject(file));
        }

        public static void AddColliderInfoToLoggedEntity(LoggedEntity loggedEntity, Collider collider) {
            if (collider == null) {
                Mod.Log($"Entity '{loggedEntity.Type}' has no collider set!");
                return;
            }

            if (collider is Hitbox) {
                Hitbox hitbox = collider as Hitbox;
                loggedEntity.Properties.Add("hitbox", new JsonRectangle() {
                    X = hitbox.Position.X,
                    Y = hitbox.Position.Y,
                    Width = hitbox.Width,
                    Height = hitbox.Height,
                });
            } else if (collider is Circle) {
                Circle hitcircle = collider as Circle;
                loggedEntity.Properties.Add("hitcircle", new JsonCircle() {
                    X = hitcircle.Position.X,
                    Y = hitcircle.Position.Y,
                    Radius = hitcircle.Radius,
                });
            }
        }

        private void ShiftFiles(FileType fileType) {
            //logs are stored in numbered files from 0 to MaxLogFiles
            //shift all files up by 1, deleting the last one

            for (int i = MaxLogFiles - 1; i >= 0; i--) {
                string pathFrom = GetFilePath(fileType, i);
                string pathTo = GetFilePath(fileType, i + 1);

                if (File.Exists(pathFrom)) {
                    File.Move(pathFrom, pathTo);
                }
            }

            //delete the last file
            string pathDelete = GetFilePath(fileType, MaxLogFiles);
            if (File.Exists(pathDelete)) {
                File.Delete(pathDelete);
            }
        }

        private string GetFilePath(FileType fileType, int fileNumber) {
            string fileName = fileType == FileType.PhysicsLog ? LogFileName : LayoutFileName;
            fileName = $"{fileNumber}_{fileName}";

            return ConsistencyTrackerModule.GetPathToFile($"{FolderName}/{fileName}");
        }

        public List<PhysicsLogLayoutFile> GetAvailableLayoutFiles() {

            List<PhysicsLogLayoutFile> files = new List<PhysicsLogLayoutFile>();

            for (int i = 0; i < MaxLogFiles; i++) {
                string path = GetFilePath(FileType.Layout, i);
                if (File.Exists(path)) {
                    string content = File.ReadAllText(path);
                    files.Add(JsonConvert.DeserializeObject<PhysicsLogLayoutFile>(content));
                } else {
                    break;
                }
            }
            return files;
        }
    }
}
