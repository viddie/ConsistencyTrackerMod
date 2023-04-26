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

        public class PaceStateSecret {
            [JsonProperty("webhookUrl")]
            public string WebhookUrl { get; set; } = null; //https://discord.com/api/webhooks/1035343625829236786/P-QfeaFDtb1lHWXKBpauZiBLna7BSfAHOWEQ45Een6fEZvoxiB5rAaoivjsZOOCLM_VJ
        }
        
        public class PaceState {
            [JsonProperty("defaultPingMessage")]
            public string DefaultPingMessage { get; set; } = $"We got a run to {{room:name}} <:pausefrogeline:991638917944184842>! @viddie_";

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
        public void ToggleCurrentRoomPacePing() {
            PathInfo path = Mod.CurrentChapterPath;
            if (path == null || path.CurrentRoom == null) return;
            
            string id = path.ChapterSID;

            PaceTiming paceTiming = GetPaceTiming(id, path.CurrentRoom.DebugRoomName);
            if (paceTiming == null) {
                if (!State.PacePingTimings.ContainsKey(id)) {
                    State.PacePingTimings.Add(id, new List<PaceTiming>());
                }

                State.PacePingTimings[id].Add(new PaceTiming() {
                    DebugRoomName = path.CurrentRoom.DebugRoomName,
                    CustomPingMessage = null,
                    LastPingedAt = DateTime.MinValue
                });
            } else {
                State.PacePingTimings[id].Remove(paceTiming);
            }

            SaveState();
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
        #endregion

        public PaceTiming GetPaceTiming(string chapterSID, string debugRoomName) {
            if (State.PacePingTimings == null) {
                State.PacePingTimings = new Dictionary<string, List<PaceTiming>>();
            }
            
            if (!State.PacePingTimings.TryGetValue(chapterSID, out List<PaceTiming> timings)) {
                Mod.Log($"Didn't find room timings for chapter {chapterSID}");
                return null;
            }

            return timings.FirstOrDefault(timing => timing.DebugRoomName == debugRoomName);
        }

        public void CheckPacePing(PathInfo path, ChapterStats stats, bool ignoreGolden = false) {
            if (path == null) return; //No path = no ping
            if (path.CurrentRoom == null) return; //Not on path = no ping

            if (!stats.ModState.PlayerIsHoldingGolden && ignoreGolden == false) return; //No golden = no ping
            PaceTiming paceTiming = GetPaceTiming(path.ChapterSID, path.CurrentRoom.DebugRoomName);
            if (paceTiming == null) {
                Mod.Log($"No ping timing setup for room '{path.CurrentRoom.GetFormattedRoomName(StatManager.RoomNameType)}'");
                return; //No pace ping setup for current room = no ping
            }

            SendPing(path, stats, paceTiming);
        }

        public void SendPing(PathInfo path, ChapterStats stats, PaceTiming paceTiming) {
            RoomInfo rInfo = path.FindRoom(paceTiming.DebugRoomName);

            Mod.Log($"Sending pace ping! (Room: {rInfo.GetFormattedRoomName(StatManager.RoomNameType)})");
            try {
                paceTiming.LastPingedAt = DateTime.Now;
                SaveState();

                string message = paceTiming.CustomPingMessage ?? State.DefaultPingMessage;
                message = Mod.StatsManager.FormatVariableFormat(message);

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

                SendDiscordWebhookMessage(new DiscordWebhookRequest() {
                    Username = "Happylandeline",
                    Content = message,
                    Embeds = new List<DiscordWebhookRequest.Embed>() {
                    new DiscordWebhookRequest.Embed(){
                        //Author = new DiscordWebhookRequest.Author() { Name = "Embed Author" },
                        Title = chapterField,
                        Description = $"Of the campaign '{campaign}'",
                        Color = 15258703,
                        Fields = new List<DiscordWebhookRequest.Field>(){
                            //new DiscordWebhookRequest.Field() { Inline = false, Name = "Chapter", Value = chapterField },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Entries Total", Value = $"{entries}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Entries Session", Value = $"{entriesSession}" },
                            new DiscordWebhookRequest.Field() { Inline = false, Name = "Total Success Rate", Value = $"{StatManager.FormatPercentage(totalSuccessRate)}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Overall Deaths", Value = $"{totalDeaths}" },
                            new DiscordWebhookRequest.Field() { Inline = true, Name = "Overall Deaths Session", Value = $"{totalDeathsSession}" },
                        }
                    },
                },
                }, StateSecret.WebhookUrl);
            } catch (Exception ex) {
                Mod.Log($"An exception occurred while trying to send pace ping: {ex}", isFollowup:true);
            }
        }

        public void SendDiscordWebhookMessage(DiscordWebhookRequest request, string url) {
            WebClient client = new WebClient();
            client.Headers.Add("Content-Type", "application/json");
            string payload = JsonConvert.SerializeObject(request);
            client.UploadData(url, Encoding.UTF8.GetBytes(payload));
        }
    }
}
