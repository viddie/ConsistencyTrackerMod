using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class PacePingManager {
        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        private const string FOLDER_NAME = "pace-ping";
        private const string SAVED_STATE_FILE_NAME_BASE = "state";
        private const string SAVE_STATE_SECRET_FILE_NAME_BASE = "state-secret_DONT_SHOW_ON_STREAM";

        private string SavedStateFileName;
        public string SaveStateSecretFileName;

        private const int PROGRESS_LENGTH = 20;
        private const string CHAR_NO_PROGRESS = ":black_large_square:"; //□,-
        private const string CHAR_PROGRESS = ":white_large_square:"; //■,▇
        private const string CHAR_DEATH = ":skull:";
        private const string CHAR_ALIVE = ":green_square:";
        private const string CHAR_RESET = ":leftwards_arrow_with_hook:";
        private const string CHAR_GOAL = ":checkered_flag:";
        private const string CHAR_TROPHY = ":trophy:";


        private bool PBPingedThisRun { get; set; }
        private bool PBPingedSkipRegularCheck { get; set; }
        private bool UpdatedMessageWithDeath { get; set; }
        private DiscordWebhookResponse EmbedMessage { get; set; }
        private RoomDetails LastRoomDetails { get; set; }

        #region Data Structures
        private class RoomDetails {
            public string ChapterField { get; set; }
            public string Description { get; set; }

            public string RoomName { get; set; }
            public int RoomNumberInChapter { get; set; }
            public int ChapterRoomCount { get; set; }

            public string CurrentRunNumber { get; set; }
            public string TopPercentString { get; set; }

            public float Progress { get; set; }
            public string Distance { get; set; }

            public string AdditionEmbed1Title { get; set; }
            public string AdditionEmbed1Content { get; set; }

            public string AdditionEmbed2Title { get; set; }
            public string AdditionEmbed2Content { get; set; }
            public string AdditionEmbed3Title { get; set; }
            public string AdditionEmbed3Content { get; set; }
            public string AdditionEmbed4Title { get; set; }
            public string AdditionEmbed4Content { get; set; }
        }

        public enum PbPingType {
            NoPing,
            PingOnPbEntry,
            PingOnPbPassed,
        }

        public class PaceStateSecret {
            [JsonProperty("webhookUrl")]
            public string WebhookUrl { get; set; }

            [JsonProperty("webhookUrlAllDeaths")]
            public string WebhookUrlAllDeaths { get; set; }
        }

        public class PaceState {
            //Defaults
            [JsonProperty("pingName")]
            public string PingName {get; set;} = "Default";
            
            [JsonProperty("webhookUsername")]
            public string WebhookUsername { get; set; } = $"Pace-Bot";

            [JsonProperty("defaultPingMessage")]
            public string DefaultPingMessage { get; set; } = $"We got a run to '{{room:name}}'!";

            [JsonProperty("defaultPingDescription")]
            public string DefaultPingDescription { get; set; } = $"";

            [JsonProperty("afterPingDeathMessage")]
            public string AfterPingDeathMessage { get; set; } = $"Death to '{{room:name}}' ({{room:roomNumberInChapter}}/{{chapter:roomCount}})";

            [JsonProperty("pingCooldownSeconds")]
            public int PingCooldownSeconds { get; set; } = 30;

            //On every golden death
            [JsonProperty("allDeathsMessage")]
            public string AllDeathsMessage { get; set; } = $"Death to '{{room:name}}' ({{room:roomNumberInChapter}}/{{chapter:roomCount}})";

            //On PB
            [JsonProperty("pbPingMessage")]
            public string PbPingMessage { get; set; } = $"On PB pace right now! Room '{{room:name}}' ({{room:roomNumberInChapter}}/{{chapter:roomCount}})";

            //On Win
            [JsonProperty("winMessage")]
            public string WinMessage { get; set; } = $"WIN!!!";


            //Map Specific Settings
            [JsonProperty("mapSettings")]
            public Dictionary<string, MapSettings> MapSpecificSettings { get; set; } = new Dictionary<string, MapSettings>();

            //Ping Timings
            [JsonProperty("pacePingTimings")]
            public Dictionary<string, List<PaceTiming>> PacePingTimings { get; set; } = new Dictionary<string, List<PaceTiming>>();

            //Additional Embed Info
            [JsonProperty("additionalEmbed1Title")]
            public string AdditionEmbed1Title { get; set; } = "Best Runs";
            [JsonProperty("additionalEmbed1Content")]
            public string AdditionEmbed1Content { get; set; } = "{pb:best} | {pb:best#2} | {pb:best#3} | {pb:best#4} | {pb:best#5}";

            [JsonProperty("additionalEmbed2Title")]
            public string AdditionEmbed2Title { get; set; } = "Best Runs (Session)";
            [JsonProperty("additionalEmbed2Content")]
            public string AdditionEmbed2Content { get; set; } = "{pb:bestSession} | {pb:bestSession#2} | {pb:bestSession#3} | {pb:bestSession#4} | {pb:bestSession#5}";

            [JsonProperty("additionalEmbed3Title")]
            public string AdditionEmbed3Title { get; set; }
            [JsonProperty("additionalEmbed3Content")]
            public string AdditionEmbed3Content { get; set; }

            [JsonProperty("additionalEmbed4Title")]
            public string AdditionEmbed4Title { get; set; }
            [JsonProperty("additionalEmbed4Content")]
            public string AdditionEmbed4Content { get; set; }


        }

        public class PaceTiming {
            [JsonProperty("debugRoomName")]
            public string DebugRoomName { get; set; } //"a-08"

            [JsonProperty("customPingMessage")]
            public string CustomPingMessage { get; set; }

            [JsonProperty("lastPingedAt")]
            public DateTime LastPingedAt { get; set; }
        }

        public class MapSettings {
            [JsonProperty("pingsEnabled")]
            public bool PingsEnabled { get; set; }
            
            [JsonProperty("pbPingType")]
            public PbPingType PbPingType { get; set; }
        }
        #endregion



        public PaceStateSecret StateSecret { get; set; }
        public PaceState State { get; set; }
        public MapSettings CurrentMapSettings {
            get {
                if (Mod.CurrentChapterStats == null || Mod.CurrentChapterPath == null) return new MapSettings(); //Not in a map
                string currentMap = Mod.CurrentChapterStats.ChapterSID;
                if (State.MapSpecificSettings == null || !State.MapSpecificSettings.TryGetValue(currentMap, out MapSettings settings)) return new MapSettings();
                return settings;
            }
            set {
                if (Mod.CurrentChapterStats == null || Mod.CurrentChapterPath == null) return; //Not in a map
                string currentMap = Mod.CurrentChapterStats.ChapterSID;
                if (State.MapSpecificSettings == null) State.MapSpecificSettings = new Dictionary<string, MapSettings>();
                State.MapSpecificSettings[currentMap] = value;
                SaveState();
            }
        }

        public PacePingManager() {
            LoadState();
        }

        public PacePingManager(int iteration) {
            SavedStateFileName = SAVED_STATE_FILE_NAME_BASE + "_" + iteration + ".json";
            SaveStateSecretFileName = SAVE_STATE_SECRET_FILE_NAME_BASE + "_" + iteration + ".json";
            LoadState();
        }

        #region Events
        public void Hook() {
            Events.Events.OnChangedRoom += Events_OnChangedRoom;
            Events.Events.OnEnteredPbRoomWithGolden += Events_OnEnteredPbRoomWithGolden;
            Events.Events.OnExitedPbRoomWithGolden += Events_OnExitedPbRoomWithGolden;
            Events.Events.OnResetSession += Events_OnResetRun;
            Events.Events.OnRunEnded += EventsOnRunEnded;
        }
        
        public void UnHook() {
            Events.Events.OnChangedRoom -= Events_OnChangedRoom;
            Events.Events.OnEnteredPbRoomWithGolden -= Events_OnEnteredPbRoomWithGolden;
            Events.Events.OnExitedPbRoomWithGolden -= Events_OnExitedPbRoomWithGolden;
            Events.Events.OnResetRun -= Events_OnResetRun;
            Events.Events.OnRunEnded -= EventsOnRunEnded;
        }


        private void Events_OnChangedRoom(string roomName, bool isPreviousRoom) {
            CheckPacePing(Mod.CurrentChapterPath, Mod.CurrentChapterStats);
        }
        
        private void Events_OnEnteredPbRoomWithGolden() {
            Mod.Log($"Triggered PB Entered event");
            if (!Mod.ModSettings.PacePingEnabled || PBPingedThisRun) {
                return;
            }

            if (!CurrentMapSettings.PingsEnabled || CurrentMapSettings.PbPingType != PbPingType.PingOnPbEntry) {
                return;
            }

            SendPing(Mod.CurrentChapterPath, Mod.CurrentChapterStats, Mod.CurrentChapterPath.CurrentRoom, State.PbPingMessage);
            PBPingedThisRun = true;
            PBPingedSkipRegularCheck = true;
        }
        
        private void Events_OnExitedPbRoomWithGolden() {
            Mod.Log($"Triggered PB Exited event");
            if (!Mod.ModSettings.PacePingEnabled || PBPingedThisRun) {
                return;
            }
            
            if (!CurrentMapSettings.PingsEnabled || CurrentMapSettings.PbPingType != PbPingType.PingOnPbPassed) {
                return;
            }

            SendPing(Mod.CurrentChapterPath, Mod.CurrentChapterStats, Mod.CurrentChapterPath.CurrentRoom, State.PbPingMessage);
            PBPingedThisRun = true;
            PBPingedSkipRegularCheck = true;
        }
        
        private void Events_OnResetRun() {
            ResetRun(Mod.CurrentChapterPath, Mod.CurrentChapterStats);
        }
        
        private void EventsOnRunEnded(bool died, bool won) {
            if (died) {
                DiedWithGolden(Mod.CurrentChapterPath, Mod.CurrentChapterStats);
            } else if (won) {
                try { //we DONT want to crash when winning, just in case
                    CollectedGolden(Mod.CurrentChapterPath, Mod.CurrentChapterStats);
                } catch (Exception ex) {
                    Mod.Log($"An exception occurred on CollectedGoldenBerry: {ex}", isFollowup: true);
                }
            }
        }
        #endregion

        #region State IO
        private void LoadState() {
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME));
            bool doSave = false;

            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SavedStateFileName);
            string stateSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SaveStateSecretFileName);

            if (File.Exists(stateFilePath)) {
                string stateFileContents = File.ReadAllText(stateFilePath);
                State = JsonConvert.DeserializeObject<PaceState>(stateFileContents);
            } else {
                State = new PaceState();
                doSave = true;
            }

            if (File.Exists(stateSecretFilePath)) {
                string stateSecretFileContents = File.ReadAllText(stateSecretFilePath);
                StateSecret = JsonConvert.DeserializeObject<PaceStateSecret>(stateSecretFileContents);
            } else {
                StateSecret = new PaceStateSecret();
                doSave = true;
            }

            if (doSave) {
                SaveState();
            }
        }

        private void SaveState() {
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SavedStateFileName);
            string stateSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SaveStateSecretFileName);
            File.WriteAllText(stateFilePath, JsonConvert.SerializeObject(State, Formatting.Indented));
            File.WriteAllText(stateSecretFilePath, JsonConvert.SerializeObject(StateSecret));
        }

        private bool DeleteState() {
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SavedStateFileName);
            string stateSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SaveStateSecretFileName);
            if (!File.Exists(stateFilePath) || !File.Exists(stateSecretFilePath)) return false;
            try {
                File.Delete(stateFilePath);
                File.Delete(stateSecretFilePath);
                return true;
            } catch (Exception) {
                return false;
            }
        }

        public bool RenameState(int iteration) {
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SavedStateFileName);
            string stateSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SaveStateSecretFileName);
            SavedStateFileName = SAVED_STATE_FILE_NAME_BASE + "_" + iteration + ".json";
            SaveStateSecretFileName = SAVE_STATE_SECRET_FILE_NAME_BASE + "_" + iteration + ".json";
            SaveState();
            if (!File.Exists(stateFilePath) || !File.Exists(stateSecretFilePath)) return false;
            try {
                File.Delete(stateFilePath);
                File.Delete(stateSecretFilePath);
                return true;
            } catch (Exception) {
                return false;
            }
        }
        #endregion

        #region Mod Options Actions
        public bool SetCurrentRoomPacePingEnabled(bool setEnabled) {
            bool isNowEnabled = false;
            PathInfo path = Mod.CurrentChapterPath;
            if (path?.CurrentRoom == null) return false;

            string id = path.ChapterSID;

            PaceTiming paceTiming = GetPaceTiming(id, path.CurrentRoom.DebugRoomName);
            if (paceTiming == null && setEnabled) {
                if (!State.PacePingTimings.ContainsKey(id)) {
                    State.PacePingTimings.Add(id, new List<PaceTiming>());
                }

                State.PacePingTimings[id].Add(new PaceTiming() {
                    DebugRoomName = path.CurrentRoom.DebugRoomName,
                    CustomPingMessage = null,
                    LastPingedAt = DateTime.MinValue
                });

                isNowEnabled = true;
            } else if (paceTiming != null) {
                if (setEnabled) {
                    isNowEnabled = true;
                } else {
                    State.PacePingTimings[id].Remove(paceTiming);
                }
            }

            SaveState();
            return isNowEnabled;
        }

        public void TestPingForCurrentRoom() {
            PathInfo path = Mod.CurrentChapterPath;
            if (path == null || path.CurrentRoom == null) return;

            CheckPacePing(path, Mod.CurrentChapterStats, ignoreGolden: true);
        }

        public void ReloadStateFile() {
            LoadState();
        }

        public void SavePingName(string name) {
            State.PingName = name;
            SaveState();
        }

        public void SaveDiscordWebhook(string webhook) {
            StateSecret.WebhookUrl = webhook;
            SaveState();
        }
        public void SaveDiscordWebhookAllDeaths(string webhook) {
            StateSecret.WebhookUrlAllDeaths = webhook;
            SaveState();
        }

        public void SaveDefaultPingMessage(string message) {
            State.DefaultPingMessage = message;
            SaveState();
        }
        public void SavePBPingMessage(string message) {
            State.PbPingMessage = message;
            SaveState();
        }
        public void SaveAllDeathsMessage(string message) {
            State.AllDeathsMessage = message;
            SaveState();
        }

        public void SaveCustomPingMessage(string message) {
            PathInfo path = Mod.CurrentChapterPath;
            if (path == null || path.CurrentRoom == null) return;

            string id = path.ChapterSID;

            PaceTiming paceTiming = GetPaceTiming(id, path.CurrentRoom.DebugRoomName);
            if (paceTiming == null) return;

            paceTiming.CustomPingMessage = message;
            SaveState();
        }

        public bool DeletePing() {
            return DeleteState();
        }
        #endregion

        #region State
        public void CheckPacePing(PathInfo path, ChapterStats stats, bool ignoreGolden = false) {
            if (path == null) return; //No path = no ping

            if (!CurrentMapSettings.PingsEnabled) return; //Map ping disabled = no ping

            RoomInfo currentRoom = path.CurrentRoom;
            if (currentRoom == null) return; //Not on path = no ping
            if (!Mod.ModSettings.PacePingEnabled) return;

            if (!stats.ModState.PlayerIsHoldingGolden && !ignoreGolden) return; //No golden = no ping

            if (EmbedMessage != null) {
                SendUpdatePing(path, stats);
            }

            if (PBPingedSkipRegularCheck) {
                PBPingedSkipRegularCheck = false;
                return; //Pinged from the PB ping, skip checking normal pace ping
            }
            
            PaceTiming paceTiming = GetPaceTiming(path.ChapterSID, currentRoom.DebugRoomName);
            if (paceTiming == null) {
                Mod.LogVerbose($"No ping timing setup for room '{currentRoom.GetFormattedRoomName(StatManager.RoomNameType)}'");
                return; //No pace ping setup for current room = no ping
            }

            if (paceTiming.LastPingedAt != DateTime.MinValue && DateTime.Now - paceTiming.LastPingedAt < TimeSpan.FromSeconds(State.PingCooldownSeconds)) {
                return; //On cooldown = no ping
            }
            paceTiming.LastPingedAt = DateTime.Now;
            SaveState();

            string message = paceTiming.CustomPingMessage ?? State.DefaultPingMessage;
            SendPing(path, stats, currentRoom, message);
        }

        public void ResetRun(PathInfo path, ChapterStats stats) {
            Mod.Log($"Resetting Pace Ping run EmbedMessage == null -> {(EmbedMessage == null ? "true" : "false")}");
            if (EmbedMessage != null && UpdatedMessageWithDeath == false) {
                DiedWithGolden(path, stats, reset: true);
            }

            PBPingedThisRun = false;
            PBPingedSkipRegularCheck = false;
            UpdatedMessageWithDeath = false;
            EmbedMessage = null;
        }

        public void DiedWithGolden(PathInfo path, ChapterStats stats, bool reset = false) {
            if (Mod.ModSettings.PacePingAllDeathsEnabled) SendAllDeathsMessage(path, stats);

            if (EmbedMessage == null) return; //no ping, no follow-up message
            PBPingedThisRun = false;
            PBPingedSkipRegularCheck = false;
            UpdatedMessageWithDeath = true;

            string message = reset ? "Reset the run" : State.AfterPingDeathMessage;
            try {
                message = Mod.StatsManager.FormatVariableFormat(message);

                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = State.WebhookUsername,
                    Content = EmbedMessage.Content + " -> " + message,
                    Embeds = GetEmbedsForRoom(path, stats, false, true, reset),
                }, StateSecret.WebhookUrl, DiscordWebhookAction.SendFinal);
            } catch (Exception) {
                // ignored
            }
        }

        public void CollectedGolden(PathInfo path, ChapterStats stats) {
            try { //Just in case. We DONT want a crash when winning
                if (EmbedMessage == null) return; //no ping, no follow-up message

                string message = State.WinMessage;
                message = Mod.StatsManager.FormatVariableFormat(message);

                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = State.WebhookUsername,
                    Content = EmbedMessage.Content + " -> " + message,
                    Embeds = GetEmbedsForRoom(path, stats, true),
                }, StateSecret.WebhookUrl, DiscordWebhookAction.SendFinal);
            } catch (Exception ex) {
                Mod.Log($"An exception occurred while trying to send win message: {ex}", isFollowup: true);
            }
        }
        #endregion

        #region Message Sending
        public void SendPing(PathInfo path, ChapterStats stats, RoomInfo pingRoom, string message) {
            Mod.Log($"Sending pace ping! (Room: {pingRoom.GetFormattedRoomName(StatManager.RoomNameType)}, message: '{message}')");
            try {
                message = Mod.StatsManager.FormatVariableFormat(message);

                if (EmbedMessage == null) {
                    SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                        Username = State.WebhookUsername,
                        Content = message,
                        Embeds = GetEmbedsForRoom(path, stats),
                    }, StateSecret.WebhookUrl, DiscordWebhookAction.SendUpdate);
                } else {
                    SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                        Username = State.WebhookUsername,
                        Content = message,
                    }, StateSecret.WebhookUrl, DiscordWebhookAction.Separate);
                }
            } catch (Exception ex) {
                Mod.Log($"An exception occurred while trying to send pace ping: {ex}", isFollowup: true);
            }
        }

        public void SendUpdatePing(PathInfo path, ChapterStats stats) {
            if (EmbedMessage == null) return;

            try {
                string message = EmbedMessage.Content;
                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = State.WebhookUsername,
                    Content = message,
                    Embeds = GetEmbedsForRoom(path, stats),
                }, StateSecret.WebhookUrl, DiscordWebhookAction.SendUpdate);
            } catch (Exception ex) {
                Mod.Log($"An exception occurred while trying to update pace ping embed: {ex}");
            }
        }

        public List<DiscordWebhookRequest.Embed> GetEmbedsForRoom(PathInfo path, ChapterStats stats, bool collectedGolden = false, bool died = false, bool reset = false) {
            if (path == null) return null;
            if (stats == null) return null;
            if (path.CurrentRoom == null && LastRoomDetails == null) return null;

            RoomDetails details = new RoomDetails();

            if ((died || reset || path.CurrentRoom == null) && LastRoomDetails != null) { //Take old details 
                details = LastRoomDetails;

                if (died && !reset) {
                    if (!string.IsNullOrEmpty(State.AdditionEmbed1Title) && !string.IsNullOrEmpty(State.AdditionEmbed1Content)) {
                        details.AdditionEmbed1Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed1Title);
                        details.AdditionEmbed1Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed1Content);
                    }
                    if (!string.IsNullOrEmpty(State.AdditionEmbed2Title) && !string.IsNullOrEmpty(State.AdditionEmbed2Content)) {
                        details.AdditionEmbed2Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed2Title);
                        details.AdditionEmbed2Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed2Content);
                    }
                    if (!string.IsNullOrEmpty(State.AdditionEmbed3Title) && !string.IsNullOrEmpty(State.AdditionEmbed3Content)) {
                        details.AdditionEmbed3Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed3Title);
                        details.AdditionEmbed3Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed3Content);
                    }
                    if (!string.IsNullOrEmpty(State.AdditionEmbed4Title) && !string.IsNullOrEmpty(State.AdditionEmbed4Content)) {
                        details.AdditionEmbed4Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed4Title);
                        details.AdditionEmbed4Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed4Content);
                    }
                }
            } else {
                string description = State.DefaultPingDescription;
                details.Description = Mod.StatsManager.FormatVariableFormat(description);

                string chapterName = path.ChapterName.Replace(":monikadsidespack_cassette_finale: ", "");
                string sideAddition = path.SideName == "A-Side" ? "" : $" [{path.SideName}]";
                details.ChapterField = $"{chapterName}{sideAddition}";

                details.CurrentRunNumber = collectedGolden ? "#WIN" : "#" + Mod.StatsManager.FormatVariableFormat(CurrentRunPbStat.RunCurrentPbStatus);
                details.TopPercentString = collectedGolden ? "0%" : Mod.StatsManager.FormatVariableFormat(CurrentRunPbStat.RunTopXPercent);

                details.RoomName = path.CurrentRoom == null ? EmbedMessage.Embeds[0].Fields[0].Value : path.CurrentRoom.GetFormattedRoomName(StatManager.RoomNameType);
                int lastRoomNumberCorrection = collectedGolden && path.CurrentRoom != null && path.CurrentRoom.IsNonGameplayRoom ? 1 : 0;
                details.Distance = path.CurrentRoom == null ? EmbedMessage.Embeds[0].Fields[1].Value : $"({path.CurrentRoom.RoomNumberInChapter - lastRoomNumberCorrection}/{path.GameplayRoomCount})";

                details.Progress = collectedGolden ? 1 : ((float)path.CurrentRoom.RoomNumberInChapter / path.GameplayRoomCount);
                if (details.Progress == 1 && !collectedGolden) {
                    details.Progress = 0.9999f;
                }

                if (!string.IsNullOrEmpty(State.AdditionEmbed1Title) && !string.IsNullOrEmpty(State.AdditionEmbed1Content)) {
                    details.AdditionEmbed1Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed1Title);
                    details.AdditionEmbed1Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed1Content);
                }
                if (!string.IsNullOrEmpty(State.AdditionEmbed2Title) && !string.IsNullOrEmpty(State.AdditionEmbed2Content)) {
                    details.AdditionEmbed2Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed2Title);
                    details.AdditionEmbed2Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed2Content);
                }
                if (!string.IsNullOrEmpty(State.AdditionEmbed3Title) && !string.IsNullOrEmpty(State.AdditionEmbed3Content)) {
                    details.AdditionEmbed3Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed3Title);
                    details.AdditionEmbed3Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed3Content);
                }
                if (!string.IsNullOrEmpty(State.AdditionEmbed4Title) && !string.IsNullOrEmpty(State.AdditionEmbed4Content)) {
                    details.AdditionEmbed4Title = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed4Title);
                    details.AdditionEmbed4Content = Mod.StatsManager.FormatVariableFormat(State.AdditionEmbed4Content);
                }

                LastRoomDetails = details;
            }

            int progressLength = PROGRESS_LENGTH;
            string progressString = "";
            float progress = collectedGolden ? 1 : details.Progress;

            bool currentIndicator = true;
            for (int i = 0; i < progressLength; i++) {
                if (Math.Floor(progress * progressLength) >= i) {
                    progressString += CHAR_PROGRESS;
                } else {
                    if (currentIndicator) {
                        currentIndicator = false;
                        progressString = progressString.Remove(progressString.Length - CHAR_PROGRESS.Length);
                        progressString += died ? (reset ? CHAR_RESET : CHAR_DEATH) : CHAR_ALIVE;
                    }

                    progressString += CHAR_NO_PROGRESS;
                }
            }
            progressString += $" {CHAR_GOAL}" + (collectedGolden ? $" {CHAR_TROPHY}" : "");

            List<DiscordWebhookRequest.Embed> embeds = new List<DiscordWebhookRequest.Embed>() {
                new DiscordWebhookRequest.Embed(){
                    Title = details.ChapterField,
                    Description = string.IsNullOrEmpty(details.Description) ? null : details.Description,
                    Color = 15258703,
                    Fields = new List<DiscordWebhookRequest.Field>(){
                        new DiscordWebhookRequest.Field() { Inline = true, Name = $"Current Room", Value = $"{details.RoomName}" },
                        new DiscordWebhookRequest.Field() { Inline = true, Name = $"Distance", Value = $"{details.Distance}" },
                        new DiscordWebhookRequest.Field() { Inline = false, Name = "Progress", Value = $"{progressString}" },
                        new DiscordWebhookRequest.Field() { Inline = true, Name = "Current Run #", Value = $"{details.CurrentRunNumber}" },
                        new DiscordWebhookRequest.Field() { Inline = true, Name = "Top %", Value = $"{details.TopPercentString}" },
                    }
                },
            };

            if (!string.IsNullOrEmpty(State.AdditionEmbed1Title) && !string.IsNullOrEmpty(State.AdditionEmbed1Content)) {
                embeds[0].Fields.Add(new DiscordWebhookRequest.Field() { Inline = false, Name = details.AdditionEmbed1Title, Value = details.AdditionEmbed1Content });
            }
            if (!string.IsNullOrEmpty(State.AdditionEmbed2Title) && !string.IsNullOrEmpty(State.AdditionEmbed2Content)) {
                embeds[0].Fields.Add(new DiscordWebhookRequest.Field() { Inline = false, Name = details.AdditionEmbed2Title, Value = details.AdditionEmbed2Content });
            }
            if (!string.IsNullOrEmpty(State.AdditionEmbed3Title) && !string.IsNullOrEmpty(State.AdditionEmbed3Content)) {
                embeds[0].Fields.Add(new DiscordWebhookRequest.Field() { Inline = false, Name = details.AdditionEmbed3Title, Value = details.AdditionEmbed3Content });
            }
            if (!string.IsNullOrEmpty(State.AdditionEmbed4Title) && !string.IsNullOrEmpty(State.AdditionEmbed4Content)) {
                embeds[0].Fields.Add(new DiscordWebhookRequest.Field() { Inline = false, Name = details.AdditionEmbed4Title, Value = details.AdditionEmbed4Content });
            }

            return embeds;
        }

        public void SendAllDeathsMessage(PathInfo path, ChapterStats stats) {
            string message = State.AllDeathsMessage;
            try {
                message = Mod.StatsManager.FormatVariableFormat(message);

                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = State.WebhookUsername,
                    Content = message,
                }, StateSecret.WebhookUrlAllDeaths, DiscordWebhookAction.Separate);
            } catch (Exception) {
                // ignored
            }
        }

        public enum DiscordWebhookAction {
            SendUpdate,
            SendFinal,
            Separate
        }
        public void SendDiscordWebhookMessage(DiscordWebhookRequest request, string url, DiscordWebhookAction action) {
            DiscordWebhookResponse localMessage = EmbedMessage;
            Task.Run(() => {
                WebClient client = new WebClient();
                client.Headers.Add("Content-Type", "application/json");
                string payload = JsonConvert.SerializeObject(request);

                string response;
                if (localMessage == null || action == DiscordWebhookAction.Separate) {
                    response = client.UploadString(url + "?wait=true", payload);
                } else {
                    if (request.Embeds == null) {
                        request.Embeds = localMessage.Embeds;
                    }
                    response = client.UploadString(url + "/messages/" + localMessage.Id + "?wait=true", "PATCH", payload);
                }

                Mod.Log($"Discord webhook response: {response}", isFollowup: true);

                DiscordWebhookResponse webhookResponse = JsonConvert.DeserializeObject<DiscordWebhookResponse>(response);
                if (webhookResponse == null) {
                    Mod.Log($"Couldn't parse discord webhook response to DiscordWebhookResponse", isFollowup: true);
                    return;
                }

                switch (action) {
                    case DiscordWebhookAction.SendUpdate:
                        EmbedMessage = webhookResponse;
                        break;
                    case DiscordWebhookAction.SendFinal:
                    default:
                        EmbedMessage = null;
                        break;
                }
            });
        }
        #endregion

        #region Misc
        public PaceTiming GetPaceTiming(string chapterSID, string debugRoomName, bool dontLog = false) {
            if (State.PacePingTimings == null) {
                State.PacePingTimings = new Dictionary<string, List<PaceTiming>>();
            }
            if (chapterSID == null) {
                Mod.Log($"{nameof(chapterSID)} was null");
                return null;
            }

            if (!State.PacePingTimings.TryGetValue(chapterSID, out var timings)) {
                if (!dontLog) {
                    Mod.Log($"Didn't find room timings for chapter {chapterSID}");
                }
                return null;
            }

            return timings.FirstOrDefault(timing => timing.DebugRoomName == debugRoomName);
        }
        #endregion
    }

    public class MultiPacePingManager {
        private const string FOLDER_NAME = "pace-ping";
        private const string SAVED_STATE_FILE_NAME = "state";
        private const string SAVE_STATE_SECRET_FILE_NAME = "state-secret_DONT_SHOW_ON_STREAM";

        public List<PacePingManager> PacePingManagers { get; }
        public int CurrSelected;

        public MultiPacePingManager() {
            PacePingManagers = new List<PacePingManager>();
            LoadState();
            CurrSelected = 0;
        }

        private void LoadState() {
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME));

            int currIteration = 0;
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SAVED_STATE_FILE_NAME + "_" + currIteration + ".json");
            string oldStateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SAVED_STATE_FILE_NAME + ".json");

            // Migration logic
            if (!File.Exists(stateFilePath) && File.Exists(oldStateFilePath)) {
                string secretFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SAVE_STATE_SECRET_FILE_NAME + "_" + currIteration + ".json");
                string oldSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SAVE_STATE_SECRET_FILE_NAME + ".json");

                File.Copy(oldStateFilePath, stateFilePath);
                File.Copy(oldSecretFilePath, secretFilePath);
            }

            do {
                PacePingManager manager = new PacePingManager(currIteration);
                PacePingManagers.Add(manager);
                currIteration++;
                stateFilePath = ConsistencyTrackerModule.GetPathToFile(FOLDER_NAME, SAVED_STATE_FILE_NAME + "_" + currIteration + ".json");
            } while (File.Exists(stateFilePath));
        }

        public void Hook() {
            foreach (var manager in PacePingManagers) {
                manager.Hook();
            }
        }

        public void UnHook() {
            foreach (PacePingManager manager in PacePingManagers) {
                manager.UnHook();
            }
        }        

        public IEnumerable<PacePingManager> GetManagers() {
            return PacePingManagers;
        }

        public void SetSelectedPing(int idx) {
            CurrSelected = idx;
        }

        public PacePingManager GetSelectedPing() {
            return PacePingManagers[CurrSelected];
        }

        public PacePingManager AddNewPing() {
            PacePingManager newManager = new PacePingManager(PacePingManagers.Count);
            newManager.Hook();
            PacePingManagers.Add(newManager);
            return newManager;
        }

        public bool DeleteCurrentPing() {
            if (PacePingManagers.Count <= 1) {
                return false;
            }
            bool deleted = GetSelectedPing().DeletePing();

            if(deleted) {
                GetSelectedPing().UnHook();
                PacePingManagers.RemoveAt(CurrSelected);
                if (CurrSelected >= PacePingManagers.Count) {
                    CurrSelected = PacePingManagers.Count - 1;
                } else {
                    for (int i = CurrSelected; i < PacePingManagers.Count; i++) {
                        PacePingManagers[i].RenameState(i);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
