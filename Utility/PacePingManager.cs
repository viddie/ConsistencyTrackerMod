using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.PhysicsLog;
using Celeste.Mod.ConsistencyTracker.Stats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class PacePingManager {

        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        private const string FolderName = "pace-ping";
        private const string SavedStateFileName = "state.json";
        public const string SaveStateSecretFileName = "state-secret_DONT_SHOW_ON_STREAM.json";
        private bool PingedThisRun { get; set; } = false;

        public class PaceStateSecret {
            [JsonProperty("webhookUrl")]
            public string WebhookUrl { get; set; } = null; //https://discord.com/api/webhooks/1035343625829236786/P-QfeaFDtb1lHWXKBpauZiBLna7BSfAHOWEQ45Een6fEZvoxiB5rAaoivjsZOOCLM_VJ
        }
        
        public class PaceState {
            [JsonProperty("webhookUsername")]
            public string WebhookUsername { get; set; } = $"Pace-Bot";
            
            [JsonProperty("defaultPingMessage")]
            public string DefaultPingMessage { get; set; } = $"We got a run to '{{room:name}}'!";

            [JsonProperty("defaultPingDescription")]
            public string DefaultPingDescription { get; set; } = $"Of the campaign '{{campaign:name}}'";

            [JsonProperty("defaultDeathMessage")]
            public string DefaultDeathMessage { get; set; } = $"Death to '{{room:name}}' ({{room:roomNumberInChapter}}/{{chapter:roomCount}})";

            [JsonProperty("winMessage")]
            public string WinMessage { get; set; } = $"WIN!!!";

            [JsonProperty("pacePingTimings")]
            public Dictionary<string, List<PaceTiming>> PacePingTimings { get; set; } = new Dictionary<string, List<PaceTiming>>();
        }

        public class PaceTiming {
            [JsonProperty("debugRoomName")]
            public string DebugRoomName { get; set; } //"a-08"

            [JsonProperty("customPingMessage")]
            public string CustomPingMessage { get; set; }

            [JsonProperty("lastPingedAt")]
            public DateTime LastPingedAt { get; set; }
        }



        public PaceStateSecret StateSecret { get; set; }
        public PaceState State { get; set; }

        public PacePingManager() {
            LoadState();
        }


        #region State IO
        private void LoadState() {
            ConsistencyTrackerModule.CheckFolderExists(ConsistencyTrackerModule.GetPathToFile(FolderName));
            bool doSave = false;
            
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FolderName, SavedStateFileName);
            string stateSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FolderName, SaveStateSecretFileName);

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
            string stateFilePath = ConsistencyTrackerModule.GetPathToFile(FolderName, SavedStateFileName);
            string stateSecretFilePath = ConsistencyTrackerModule.GetPathToFile(FolderName, SaveStateSecretFileName);
            File.WriteAllText(stateFilePath, JsonConvert.SerializeObject(State, Formatting.Indented));
            File.WriteAllText(stateSecretFilePath, JsonConvert.SerializeObject(StateSecret));
        }
        #endregion

        #region Mod Options Actions
        public bool SetCurrentRoomPacePingEnabled(bool isEnabled) {
            bool isNowEnabled = false;
            PathInfo path = Mod.CurrentChapterPath;
            if (path == null || path.CurrentRoom == null) return isNowEnabled;
            
            string id = path.ChapterSID;

            PaceTiming paceTiming = GetPaceTiming(id, path.CurrentRoom.DebugRoomName);
            if (paceTiming == null && isEnabled) {
                if (!State.PacePingTimings.ContainsKey(id)) {
                    State.PacePingTimings.Add(id, new List<PaceTiming>());
                }

                State.PacePingTimings[id].Add(new PaceTiming() {
                    DebugRoomName = path.CurrentRoom.DebugRoomName,
                    CustomPingMessage = null,
                    LastPingedAt = DateTime.MinValue
                });

                isNowEnabled = true;
            } else if (paceTiming != null && isEnabled) {
                isNowEnabled = true;
            } else if (paceTiming != null && !isEnabled) {
                State.PacePingTimings[id].Remove(paceTiming);
            }

            SaveState();
            return isNowEnabled;
        }

        public void TestPingForCurrentRoom() {
            PathInfo path = Mod.CurrentChapterPath;
            if (path == null || path.CurrentRoom == null) return;

            CheckPacePing(path, Mod.CurrentChapterStats, ignoreGolden:true);
        }

        public void SaveDiscordWebhook(string webhook) {
            StateSecret.WebhookUrl = webhook;
            SaveState();
        }

        public void SaveDefaultPingMessage(string message) {
            State.DefaultPingMessage = message;
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
        #endregion

        public PaceTiming GetPaceTiming(string chapterSID, string debugRoomName, bool dontLog = false) {
            if (State.PacePingTimings == null) {
                State.PacePingTimings = new Dictionary<string, List<PaceTiming>>();
            }
            
            if (!State.PacePingTimings.TryGetValue(chapterSID, out List<PaceTiming> timings)) {
                if (!dontLog) {
                    Mod.Log($"Didn't find room timings for chapter {chapterSID}");
                }
                return null;
            }

            return timings.FirstOrDefault(timing => timing.DebugRoomName == debugRoomName);
        }

        public void CheckPacePing(PathInfo path, ChapterStats stats, bool ignoreGolden = false) {
            if (path == null) return; //No path = no ping
            if (path.CurrentRoom == null) return; //Not on path = no ping
            if (!Mod.ModSettings.PacePingEnabled) return;

            if (!stats.ModState.PlayerIsHoldingGolden && ignoreGolden == false) return; //No golden = no ping
            PaceTiming paceTiming = GetPaceTiming(path.ChapterSID, path.CurrentRoom.DebugRoomName);
            if (paceTiming == null) {
                Mod.Log($"No ping timing setup for room '{path.CurrentRoom.GetFormattedRoomName(StatManager.RoomNameType)}'");
                return; //No pace ping setup for current room = no ping
            }

            SendPing(path, stats, paceTiming);
        }
        
        public void SendPing(PathInfo path, ChapterStats stats, PaceTiming paceTiming) {
            RoomInfo rInfo = path.GetRoom(paceTiming.DebugRoomName);

            Mod.Log($"Sending pace ping! (Room: {rInfo.GetFormattedRoomName(StatManager.RoomNameType)})");
            try {
                paceTiming.LastPingedAt = DateTime.Now;
                SaveState();

                string message = paceTiming.CustomPingMessage ?? State.DefaultPingMessage;
                message = Mod.StatsManager.FormatVariableFormat(message);

                string description = State.DefaultPingDescription;
                description = Mod.StatsManager.FormatVariableFormat(description);

                string campaign = path.CampaignName;
                string chapterName = path.ChapterName;
                string sideAddition = path.SideName == "A-Side" ? "" : $" {path.SideName}";
                string chapterField = $"{chapterName}{sideAddition}";

                Dictionary<RoomInfo, Tuple<int, float, int, float>> roomData = ChokeRateStat.GetRoomData(path, stats);
                Tuple<int, float, int, float> currentRoomData = roomData[rInfo];
                int entries = currentRoomData.Item1;
                int entriesSession = currentRoomData.Item3;

                int totalDeaths = path.Stats.GoldenBerryDeaths;
                int totalDeathsSession = path.Stats.GoldenBerryDeathsSession;

                float totalSuccessRate = path.Stats.SuccessRate;

                string pbs = "{pb:best} | {pb:best#2} | {pb:best#3} | {pb:best#4} | {pb:best#5}";
                pbs = Mod.StatsManager.FormatVariableFormat(pbs);
                string pbsSession = "{pb:bestSession} | {pb:bestSession#2} | {pb:bestSession#3} | {pb:bestSession#4} | {pb:bestSession#5}";
                pbsSession = Mod.StatsManager.FormatVariableFormat(pbsSession);

                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = State.WebhookUsername,
                    Content = message,
                    Embeds = new List<DiscordWebhookRequest.Embed>() {
                    new DiscordWebhookRequest.Embed(){
                        //Author = new DiscordWebhookRequest.Author() { Name = "Embed Author" },
                        Title = chapterField,
                        Description = description,
                        Color = 15258703,
                        Fields = new List<DiscordWebhookRequest.Field>(){
                            new DiscordWebhookRequest.Field() { Inline = true, Name = $"Entries to '{rInfo.GetFormattedRoomName(StatManager.RoomNameType)}'", Value = $"{entries}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Entries This Session", Value = $"{entriesSession}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Chapter Success Rate", Value = $"{StatManager.FormatPercentage(totalSuccessRate)}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Golden Deaths", Value = $"{totalDeaths}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Golden Deaths (Session)", Value = $"{totalDeathsSession}" },
                            new DiscordWebhookRequest.Field() { Inline = false, Name = "Best Runs", Value = $"> {pbs}" },
                            new DiscordWebhookRequest.Field() { Inline = false, Name = "Best Runs (Session)", Value = $"> {pbsSession}" },
                            }
                        },
                    },
                }, StateSecret.WebhookUrl);

                PingedThisRun = true;
            } catch (Exception ex) {
                Mod.Log($"An exception occurred while trying to send pace ping: {ex}", isFollowup:true);
            }
        }

        public void ResetRun() {
            PingedThisRun = false;
        }
        public void DiedWithGolden() {
            if (!PingedThisRun) return; //no ping, no follow-up message
            PingedThisRun = false;

            string message = State.DefaultDeathMessage;
            message = Mod.StatsManager.FormatVariableFormat(message);

            SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                Username = State.WebhookUsername,
                Content = message,
            }, StateSecret.WebhookUrl);
        }

        public void CollectedGolden() {
            try { //Just in case. We DONT want a crash when winning
                if (!PingedThisRun) return; //no ping, no follow-up message
                PingedThisRun = false;

                string message = State.WinMessage;
                message = Mod.StatsManager.FormatVariableFormat(message);

                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = State.WebhookUsername,
                    Content = message,
                }, StateSecret.WebhookUrl);
            } catch (Exception ex) {
                Mod.Log($"An exception occurred while trying to send win message: {ex}", isFollowup: true);
            }
        }

        public void SendDiscordWebhookMessage(DiscordWebhookRequest request, string url) {
            Task.Run(() => {
                WebClient client = new WebClient();
                client.Headers.Add("Content-Type", "application/json");
                string payload = JsonConvert.SerializeObject(request);
                client.UploadData(url, Encoding.UTF8.GetBytes(payload));
            });
        }
    }
}
