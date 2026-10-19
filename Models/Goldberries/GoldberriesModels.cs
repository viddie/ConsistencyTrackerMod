using System.Collections.Generic;
using Newtonsoft.Json;

namespace Celeste.Mod.ConsistencyTracker.Models.Goldberries {

    /// <summary>
    /// Body of POST /api/ingame-status. All fields are always sent, null included, so nothing is left to server defaults.
    /// </summary>
    public class IngameStatusRequest {
        public const string EventStatus = "status";
        public const string EventDeath = "death";
        public const string EventReset = "reset";
        public const string EventWin = "win";
        public const string EventExit = "exit";

        [JsonProperty("event")]
        public string Event { get; set; } = EventStatus;
        [JsonProperty("challenge_id")]
        public int? ChallengeId { get; set; }
        [JsonProperty("map_name")]
        public string MapName { get; set; }
        [JsonProperty("room_number")]
        public int? RoomNumber { get; set; }
        [JsonProperty("max_room_number")]
        public int? MaxRoomNumber { get; set; }
        [JsonProperty("room_name")]
        public string RoomName { get; set; }
        [JsonProperty("custom_room_name")]
        public string CustomRoomName { get; set; }
        [JsonProperty("debug_room_name")]
        public string DebugRoomName { get; set; }
        [JsonProperty("in_golden_run")]
        public bool InGoldenRun { get; set; }
        [JsonProperty("session_pb_room_number")]
        public int? SessionPbRoomNumber { get; set; }
        [JsonProperty("overall_pb_room_number")]
        public int? OverallPbRoomNumber { get; set; }

        // Local context captured when the status is built, used to resolve challenge_id/map_name when it's sent
        [JsonIgnore]
        public string MapBin { get; set; }
        [JsonIgnore]
        public string FallbackMapName { get; set; }
        [JsonIgnore]
        public int? ManualChallengeId { get; set; }
        [JsonIgnore]
        public int? StoredAutoChallengeId { get; set; }
        [JsonIgnore]
        public bool IsFgr { get; set; }
    }

    public class GbAccount {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("player")]
        public GbPlayer Player { get; set; }
        [JsonProperty("claimed_player")]
        public GbPlayer ClaimedPlayer { get; set; }
    }

    public class GbPlayer {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class GbMap {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("campaign_id")]
        public int? CampaignId { get; set; }
        [JsonProperty("challenges")]
        public List<GbChallenge> Challenges { get; set; }
    }

    public class GbCampaign {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("challenges")]
        public List<GbChallenge> Challenges { get; set; }
    }

    public class GbChallenge {
        [JsonProperty("id")]
        public int Id { get; set; }
        [JsonProperty("label")]
        public string Label { get; set; }
        [JsonProperty("requires_fc")]
        public bool RequiresFc { get; set; }
        [JsonProperty("has_fc")]
        public bool HasFc { get; set; }
        [JsonProperty("is_rejected")]
        public bool IsRejected { get; set; }
        [JsonProperty("objective")]
        public GbObjective Objective { get; set; }
        [JsonProperty("difficulty")]
        public GbDifficulty Difficulty { get; set; }

        /// <summary>Name of the map or campaign this challenge belongs to. Set locally, only shown when there are multiple.</summary>
        [JsonIgnore]
        public string ParentName { get; set; }

        [JsonIgnore]
        public bool IsRegularClear => !RequiresFc && !HasFc;
        [JsonIgnore]
        public bool IsFullClear => RequiresFc;

        public string GetDisplayName(bool includeParentName) {
            string name = Objective?.Name ?? "Challenge";
            if (!string.IsNullOrEmpty(Objective?.DisplayNameSuffix)) {
                name += $" {Objective.DisplayNameSuffix}";
            }
            if (!string.IsNullOrEmpty(Label)) {
                name += $" {Label}";
            }
            if (RequiresFc) {
                name += " [FC]";
            } else if (HasFc) {
                name += " [C/FC]";
            }
            if (Difficulty?.Name != null) {
                name += $" ({Difficulty.Name})";
            }
            if (includeParentName && ParentName != null) {
                name = $"{ParentName}: {name}";
            }
            return name;
        }
    }

    public class GbObjective {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("display_name_suffix")]
        public string DisplayNameSuffix { get; set; }
    }

    public class GbDifficulty {
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
