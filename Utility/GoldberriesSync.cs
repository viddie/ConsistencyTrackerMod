using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Models.Goldberries;
using Celeste.Mod.ConsistencyTracker.Stats;
using Monocle;
using Newtonsoft.Json;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    /// <summary>
    /// Sends the player's ingame status to goldberries.net (see ingame-status-mod-spec.md).
    /// All game state is read on the main thread, all HTTP requests run on a single background worker thread,
    /// so requests never block gameplay and are sent in the order they happened.
    /// </summary>
    public class GoldberriesSync {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        private static ConsistencyTrackerSettings Settings => Mod.ModSettings;

        public const string ProductionBaseUrl = "https://goldberries.net";
#if DEBUG
        public const string LocalhostBaseUrl = "http://localhost";
#endif
        public const string AppName = "Consistency Tracker";
        public const string AuthEndpointPath = "/cct/goldberriesAuth";

        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(2);
        private const int RequestTimeoutMs = 10000;
        private const int MaxStringLength = 128;
        private const int MaxRoomNumber = 100000;

#if DEBUG
        public static string BaseUrl => Settings.GoldberriesUseLocalhost ? LocalhostBaseUrl : ProductionBaseUrl;
#else
        public static string BaseUrl => ProductionBaseUrl;
#endif

        #region State

        public enum AuthStatus {
            NotAuthorized,
            Checking,
            Authorized,
            Invalid,
            Unreachable,
        }
        public AuthStatus AuthState { get; private set; } = AuthStatus.NotAuthorized;
        public string AuthPlayerName { get; private set; }

        /// <summary>
        /// Invoked on the main thread whenever the auth state or the challenges of the current map change.
        /// Used by the mod options menu to refresh its items.
        /// </summary>
        public Action OnStateChanged;

        // Worker thread
        private BlockingCollection<Action> Jobs;
        private Thread Worker;

        // Map/challenge lookup. Key: map bin
        private readonly object CacheLock = new object();
        private readonly Dictionary<string, LookupResult> ChallengeCache = new Dictionary<string, LookupResult>();
        private readonly HashSet<string> PendingLookups = new HashSet<string>();
        public string CurrentMapBin { get; private set; }

        // Tracking state (main thread only)
        private bool InMap;
        private Level CurrentLevel;
        private string CurrentDebugRoomName;
        private string CurrentMapName;
        private int? LastOnPathRoomNumber;
        private DateTime LastSentAt = DateTime.MinValue;
        private string LastSentEvent;
        private string LastSentStatusJson;
        private bool HasSentInMap;
        /// <summary>A reset payload, built when the run ended. Dropped when the run ended because the player left the map.</summary>
        private IngameStatusRequest PendingReset;
        /// <summary>The in-game mod options menu, watched to send a status when it closes with a different effective challenge.</summary>
        private TextMenu OptionsMenu;
        private bool OptionsMenuShown;
        private int? ChallengeIdOnMenuOpen;
        /// <summary>Send a status once the player is alive again after a death/reset that didn't reload the room (e.g. savestates).</summary>
        private bool PendingStatusAfterRunEnd;

        #endregion

        #region Hook / UnHook

        public void Hook() {
            Jobs = new BlockingCollection<Action>();
            Worker = new Thread(WorkerLoop) {
                IsBackground = true,
                Name = "CCT Goldberries Sync",
            };
            Worker.Start();

            // Subscribed after the main module's handlers, so the CCT state is already updated when these run
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
            Everest.Events.Level.OnExit += Level_OnExit;
            On.Celeste.Level.Update += Level_Update;
            Events.Events.OnRunStarted += Events_OnRunStarted;
            Events.Events.OnRunEnded += Events_OnRunEnded;

            CheckSession();
        }

        public void UnHook() {
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
            Everest.Events.Level.OnExit -= Level_OnExit;
            On.Celeste.Level.Update -= Level_Update;
            Events.Events.OnRunStarted -= Events_OnRunStarted;
            Events.Events.OnRunEnded -= Events_OnRunEnded;

            Jobs?.CompleteAdding();
        }

        #endregion

        #region Game Events

        private void Level_OnLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            try {
                HandleLoadLevel(level, playerIntro, isFromLoader);
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: exception in OnLoadLevel: {ex}");
            }
        }
        private void HandleLoadLevel(Level level, Player.IntroTypes playerIntro, bool isFromLoader) {
            if (level.Session.LevelData == null) return;

            // The run of the previous map/attempt ended before this load, so its reset goes out first
            FlushPendingReset();

            if (isFromLoader) {
                InMap = true;
                CurrentLevel = level;
                LastOnPathRoomNumber = null;
                UpdateCurrentMap(level.Session);
            } else if (playerIntro == Player.IntroTypes.Respawn && PendingStatusAfterRunEnd) {
                // Respawned after the run ended, so the next run starts from scratch
                LastOnPathRoomNumber = null;
            }
            CurrentDebugRoomName = level.Session.Level;
            PendingStatusAfterRunEnd = false;

            // Covers entering a map, room transitions, respawns and teleports. Duplicates are filtered out.
            SendStatus();
        }

        private void Level_OnExit(Level level, LevelExit exit, LevelExit.Mode mode, Session session, HiresSnow snow) {
            try {
                HandleExit(mode);
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: exception in OnExit: {ex}");
            }
        }
        private void HandleExit(LevelExit.Mode mode) {
            if (mode == LevelExit.Mode.Restart || mode == LevelExit.Mode.GoldenBerryRestart) {
                // The map is reloaded, which sends a fresh status
                FlushPendingReset();
                PendingStatusAfterRunEnd = false;
                return;
            }

            // Leaving the map mid-run is an exit, not a reset
            PendingReset = null;
            PendingStatusAfterRunEnd = false;
            SendEvent(IngameStatusRequest.EventExit);
            InMap = false;
            CurrentLevel = null;
            HasSentInMap = false;
        }

        private void Level_Update(On.Celeste.Level.orig_Update orig, Level self) {
            orig(self);

            if (!InMap || self != CurrentLevel) return;

            try {
                HandleUpdate(self);
            } catch (Exception ex) {
                Mod.LogEvery(600, $"Goldberries sync: exception in Level.Update: {ex}");
            }
        }
        private void HandleUpdate(Level self) {
            FlushPendingReset();

            if (PendingStatusAfterRunEnd) {
                Player player = self.Tracker.GetEntity<Player>();
                if (player != null && !player.Dead) {
                    PendingStatusAfterRunEnd = false;
                    LastOnPathRoomNumber = null;
                    SendStatus();
                }
            }

            CheckOptionsMenuClosed();

            if (DateTime.UtcNow - LastSentAt >= HeartbeatInterval) {
                SendStatus(force: true);
            }
        }

        private void Events_OnRunStarted() {
            // Runs started while a new map is still being loaded are covered by the following OnLoadLevel
            if (CurrentLevel == null || Engine.Scene != CurrentLevel) return;
            try {
                SendStatus();
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: exception in OnRunStarted: {ex}");
            }
        }

        private void Events_OnRunEnded(bool died, bool won) {
            try {
                HandleRunEnded(died, won);
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: exception in OnRunEnded: {ex}");
            }
        }
        private void HandleRunEnded(bool died, bool won) {
            if (!InMap) return;

            if (won) {
                SendEvent(IngameStatusRequest.EventWin, inGoldenRun: true);
                return;
            }

            if (died) {
                SendEvent(IngameStatusRequest.EventDeath, inGoldenRun: true);
            } else {
                // Only known to be a reset once it's clear that the player didn't leave the map
                PendingReset = BuildStatus(IngameStatusRequest.EventReset, inGoldenRun: true);
            }
            PendingStatusAfterRunEnd = true;
        }

        /// <summary>
        /// Called when the in-game mod options menu is created. The challenge can be changed many times while navigating
        /// the options, so a changed challenge is only sent once the menu is closed.
        /// </summary>
        public void WatchOptionsMenu(TextMenu menu) {
            OptionsMenu = menu;
            OptionsMenuShown = false;
            ChallengeIdOnMenuOpen = GetEffectiveChallengeId();
        }

        private void CheckOptionsMenuClosed() {
            if (OptionsMenu == null) return;
            // The menu is added to the scene after it's created, so it's only closed once it was shown and got removed again
            if (OptionsMenu.Scene != null) {
                OptionsMenuShown = true;
                return;
            }
            if (!OptionsMenuShown) return;

            OptionsMenu = null;
            if (GetEffectiveChallengeId() != ChallengeIdOnMenuOpen) {
                SendStatus(force: true);
            }
        }

        private void FlushPendingReset() {
            if (PendingReset == null) return;
            IngameStatusRequest reset = PendingReset;
            PendingReset = null;
            Enqueue(reset);
        }

        #endregion

        #region Sending

        /// <summary>
        /// Whether statuses should be sent for the current state.
        /// </summary>
        public bool CanSync {
            get {
                if (!Settings.GoldberriesSyncEnabled) return false;
                if (string.IsNullOrEmpty(Settings.GoldberriesApiKey)) return false;
                if (AuthState == AuthStatus.Invalid) return false;
                PathSegment segment = Mod.CurrentChapterPathSegmentList?.CurrentSegment;
                return segment == null || segment.SyncEnabled;
            }
        }

        /// <param name="force">Send even if nothing changed since the last status.</param>
        private void SendStatus(bool force = false) {
            if (!InMap || !CanSync) return;

            IngameStatusRequest status = BuildStatus(IngameStatusRequest.EventStatus);
            string json = $"{JsonConvert.SerializeObject(status)}|{status.ManualChallengeId}|{status.StoredAutoChallengeId}|{status.MapBin}|{status.IsFgr}";
            if (!force && LastSentEvent == IngameStatusRequest.EventStatus && LastSentStatusJson == json) {
                return;
            }
            LastSentStatusJson = json;
            Enqueue(status);
        }

        private void SendEvent(string eventType, bool? inGoldenRun = null) {
            if (!InMap || !CanSync) return;
            // Don't send an exit/death/... to a map the server never heard about
            if (eventType == IngameStatusRequest.EventExit && !HasSentInMap) return;
            Enqueue(BuildStatus(eventType, inGoldenRun));
        }

        /// <summary>
        /// Sends an exit event if the player is in a map, so the website stops showing them as online
        /// immediately when syncing gets disabled.
        /// </summary>
        public void SendExitBeforeDisabling() {
            if (!InMap || !HasSentInMap || LastSentEvent == IngameStatusRequest.EventExit) return;
            Enqueue(BuildStatus(IngameStatusRequest.EventExit));
            HasSentInMap = false;
        }

        private void Enqueue(IngameStatusRequest status) {
            LastSentAt = DateTime.UtcNow;
            LastSentEvent = status.Event;
            HasSentInMap = true;

            string apiKey = Settings.GoldberriesApiKey;
            string baseUrl = BaseUrl;

            EnqueueJob(() => {
                // Resolved here, so that a map lookup queued before this status is already done
                status.ChallengeId = status.ManualChallengeId ?? GetAutoChallenge(status.MapBin, status.IsFgr)?.Id ?? status.StoredAutoChallengeId;
                status.MapName = status.ChallengeId == null ? Truncate(status.FallbackMapName) : null;

                string body = JsonConvert.SerializeObject(status);
                string response = HttpRequest("POST", $"{baseUrl}/api/ingame-status", apiKey, body, out int statusCode);
                if (statusCode == 200) {
                    Mod.LogVerbose($"Goldberries sync: sent '{status.Event}' -> {body}");
                    return;
                }

                Mod.Log($"Goldberries sync: sending '{status.Event}' failed with status {statusCode}: {response} | Body: {body}");
                if (statusCode == 401) {
                    MainThreadHelper.Do(() => {
                        if (apiKey != Settings.GoldberriesApiKey) return;
                        SetAuthState(AuthStatus.Invalid, null);
                    });
                }
            });
        }

        private IngameStatusRequest BuildStatus(string eventType, bool? inGoldenRun = null) {
            PathSegment segment = Mod.CurrentChapterPathSegmentList?.CurrentSegment;
            IngameStatusRequest status = new IngameStatusRequest() {
                Event = eventType,
                DebugRoomName = Truncate(CurrentDebugRoomName),
                InGoldenRun = inGoldenRun ?? Mod.IsInGoldenRun,
                MapBin = CurrentMapBin,
                FallbackMapName = CurrentMapName,
                ManualChallengeId = segment?.ChallengeId,
                StoredAutoChallengeId = segment?.ChallengeIdAuto,
                IsFgr = Mod.IsInFgrMode,
            };

            PathInfo path = Mod.CurrentChapterPath;
            if (path == null || path.GameplayRoomCount == 0) return status;

            int maxRoomNumber = Math.Min(path.GameplayRoomCount, MaxRoomNumber);
            status.MaxRoomNumber = maxRoomNumber;

            RoomInfo room = GetCurrentPathRoom(path);
            if (room != null && room.RoomNumberInChapter >= 1) {
                LastOnPathRoomNumber = Math.Min(room.RoomNumberInChapter, maxRoomNumber);
                // Short CCT name (e.g. "ST-2"). The custom name has its own field, so it isn't mixed in here
                status.RoomName = Truncate(room.GetFormattedRoomName(RoomNameDisplayType.AbbreviationAndRoomNumberInCP, CustomNameBehavior.Ignore));
                status.CustomRoomName = Truncate(room.CustomRoomName);
            }
            status.RoomNumber = LastOnPathRoomNumber;

            ChapterStats stats = Mod.CurrentChapterStats;
            if (stats == null) return status;

            int? overallPb = null;
            if (stats.GoldenCollectedCount > 0) {
                overallPb = maxRoomNumber;
            } else {
                RoomInfo pbRoom = PersonalBestStat.GetFurthestDeathRoom(path, stats);
                if (pbRoom != null && pbRoom.RoomNumberInChapter >= 1) overallPb = pbRoom.RoomNumberInChapter;
            }

            int? sessionPb = null;
            if (stats.GoldenCollectedCountSession > 0) {
                sessionPb = maxRoomNumber;
            } else {
                Tuple<RoomInfo, int> sessionPbRoom = PersonalBestStat.GetSessionPBRoomFromLastRuns(path, stats.LastGoldenRuns);
                if (sessionPbRoom != null) {
                    int number = path.GetRunRoomNumberInChapter(sessionPbRoom.Item1);
                    if (number >= 1) sessionPb = number;
                }
            }

            // The room of the ongoing run has been reached as well
            if (status.InGoldenRun && status.RoomNumber != null) {
                overallPb = Math.Max(overallPb ?? 0, status.RoomNumber.Value);
                sessionPb = Math.Max(sessionPb ?? 0, status.RoomNumber.Value);
            }

            status.OverallPbRoomNumber = overallPb == null ? (int?)null : Math.Min(overallPb.Value, maxRoomNumber);
            status.SessionPbRoomNumber = sessionPb == null ? (int?)null : Math.Min(sessionPb.Value, maxRoomNumber);
            return status;
        }

        private RoomInfo GetCurrentPathRoom(PathInfo path) {
            if (CurrentDebugRoomName == null) return null;
            string roomName = Mod.ResolveRoomNameInActiveChapter(CurrentDebugRoomName);
            roomName = Mod.ResolveGroupedRoomName(roomName);
            if (path.IgnoredRooms.Contains(roomName)) return null;
            return path.GetRoom(roomName);
        }

        #endregion

        #region Map / Challenge Lookup

        private void UpdateCurrentMap(Session session) {
            ChapterMetaInfo chapterInfo = new ChapterMetaInfo(session);
            // goldberries.net stores maps by the path of the played side's .bin. MapData.Filename is exactly that (equal to the
            // SID for A-Sides). The SID can't be used for other sides: Everest merges e.g. "1H-Map"/"1B-Map"/"Map-X" or
            // meta.yaml-defined sides into the A-Side's AreaData, so the SID + "-B"/"-C" doesn't always name the real file.
            string bin = chapterInfo.MapBin?.Replace('\\', '/');
            CurrentMapBin = string.IsNullOrEmpty(bin) ? null : $"{bin}.bin";
            CurrentMapName = chapterInfo.ChapterName + (session.Area.Mode == AreaMode.Normal ? "" : $" [{chapterInfo.SideName}]");

            LookupCurrentMap();
            UpdateAutoChallengeInSegment();
            OnStateChanged?.Invoke();
        }

        /// <summary>
        /// Fetches the challenges of the current map, unless they are already known.
        /// </summary>
        public void LookupCurrentMap() {
            string bin = CurrentMapBin;
            if (bin == null || !Settings.GoldberriesSyncEnabled || string.IsNullOrEmpty(Settings.GoldberriesApiKey)) return;

            lock (CacheLock) {
                if (ChallengeCache.ContainsKey(bin) || !PendingLookups.Add(bin)) return;
            }

            string baseUrl = BaseUrl;
            EnqueueJob(() => {
                LookupResult result = FetchChallenges(baseUrl, bin);
                lock (CacheLock) {
                    PendingLookups.Remove(bin);
                    if (result != null) ChallengeCache[bin] = result;
                }
                if (result == null) return;

                Mod.Log($"Goldberries sync: found {result.MapChallenges.Count} map challenge(s) for '{bin}': {string.Join(", ", result.MapChallenges.Select(c => $"{c.Id}"))}" +
                    $" and {result.CampaignChallenges.Count} campaign challenge(s): {string.Join(", ", result.CampaignChallenges.Select(c => $"{c.Id}"))}");
                MainThreadHelper.Do(() => {
                    if (bin != CurrentMapBin) return;
                    UpdateAutoChallengeInSegment();
                    OnStateChanged?.Invoke();
                });
            });
        }

        private class LookupResult {
            /// <summary>Non-rejected challenges of all maps with the bin.</summary>
            public List<GbChallenge> MapChallenges = new List<GbChallenge>();
            /// <summary>Non-rejected full game challenges of the campaigns of those maps. Used in FGR mode.</summary>
            public List<GbChallenge> CampaignChallenges = new List<GbChallenge>();
        }

        /// <returns>The challenges, or null if a request failed.</returns>
        private LookupResult FetchChallenges(string baseUrl, string bin) {
            string response = HttpRequest("GET", $"{baseUrl}/api/map/find-by-bin-path.php?bin={Uri.EscapeDataString(bin)}", null, null, out int statusCode);
            if (statusCode != 200) {
                Mod.Log($"Goldberries sync: map lookup for '{bin}' failed with status {statusCode}: {response}");
                return null;
            }

            try {
                List<GbMap> maps = JsonConvert.DeserializeObject<List<GbMap>>(response) ?? new List<GbMap>();
                LookupResult result = new LookupResult();
                foreach (GbMap map in maps) {
                    string mapResponse = HttpRequest("GET", $"{baseUrl}/api/map?id={map.Id}&challenges=true", null, null, out int mapStatusCode);
                    if (mapStatusCode != 200) {
                        Mod.Log($"Goldberries sync: fetching challenges of map {map.Id} failed with status {mapStatusCode}: {mapResponse}");
                        return null;
                    }

                    GbMap fullMap = JsonConvert.DeserializeObject<GbMap>(mapResponse);
                    if (fullMap?.Challenges == null) continue;
                    foreach (GbChallenge challenge in fullMap.Challenges.Where(c => !c.IsRejected)) {
                        challenge.ParentName = fullMap.Name;
                        result.MapChallenges.Add(challenge);
                    }
                }

                foreach (int campaignId in maps.Where(m => m.CampaignId != null).Select(m => m.CampaignId.Value).Distinct()) {
                    string campaignResponse = HttpRequest("GET", $"{baseUrl}/api/campaign/campaign.php?id={campaignId}&challenges=true", null, null, out int campaignStatusCode);
                    if (campaignStatusCode != 200) {
                        Mod.Log($"Goldberries sync: fetching challenges of campaign {campaignId} failed with status {campaignStatusCode}: {campaignResponse}");
                        return null;
                    }

                    GbCampaign campaign = JsonConvert.DeserializeObject<GbCampaign>(campaignResponse);
                    if (campaign?.Challenges == null) continue;
                    foreach (GbChallenge challenge in campaign.Challenges.Where(c => !c.IsRejected)) {
                        challenge.ParentName = campaign.Name;
                        result.CampaignChallenges.Add(challenge);
                    }
                }
                return result;
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: couldn't parse map lookup for '{bin}': {ex}");
                return null;
            }
        }

        /// <returns>
        /// The selectable challenges for the current map, or null if they aren't known (yet). In FGR mode these are
        /// the full game challenges of the map's campaign, otherwise the challenges of the map itself.
        /// </returns>
        public List<GbChallenge> GetCurrentChallenges() {
            return GetChallenges(CurrentMapBin, Mod.IsInFgrMode);
        }

        public bool IsCurrentLookupPending {
            get {
                lock (CacheLock) {
                    return CurrentMapBin != null && PendingLookups.Contains(CurrentMapBin);
                }
            }
        }

        private List<GbChallenge> GetChallenges(string bin, bool isFgr) {
            if (bin == null) return null;
            lock (CacheLock) {
                if (!ChallengeCache.TryGetValue(bin, out LookupResult result)) return null;
                return isFgr ? result.CampaignChallenges : result.MapChallenges;
            }
        }

        private GbChallenge GetAutoChallenge(string bin, bool isFgr) {
            return SelectAutoChallenge(GetChallenges(bin, isFgr), Settings.GoldberriesPreferredClearType);
        }

        public static GbChallenge SelectAutoChallenge(List<GbChallenge> challenges, GoldberriesClearType preferredType) {
            if (challenges == null || challenges.Count == 0) return null;
            if (challenges.Count == 1) return challenges[0];

            // A C/FC challenge (has_fc) covers both clear types, so it's the fallback before giving up on the preference
            GbChallenge preferred = preferredType == GoldberriesClearType.FullClear
                ? challenges.FirstOrDefault(c => c.IsFullClear)
                : challenges.FirstOrDefault(c => c.IsRegularClear);
            return preferred ?? challenges.FirstOrDefault(c => c.HasFc) ?? challenges[0];
        }

        /// <summary>
        /// Stores the automatically selected challenge in the active path segment, so it is known before the lookup is done next time.
        /// </summary>
        public void UpdateAutoChallengeInSegment() {
            PathSegment segment = Mod.CurrentChapterPathSegmentList?.CurrentSegment;
            List<GbChallenge> challenges = GetCurrentChallenges();
            if (segment == null || challenges == null) return;

            int? autoId = SelectAutoChallenge(challenges, Settings.GoldberriesPreferredClearType)?.Id;
            // FGR paths span multiple maps. One that isn't on goldberries (or has no campaign) shouldn't wipe the campaign challenge
            if (autoId == null && Mod.IsInFgrMode) return;
            if (segment.ChallengeIdAuto == autoId) return;
            segment.ChallengeIdAuto = autoId;
            Mod.SaveActivePath();
        }

        /// <summary>
        /// The challenge ID that is sent with the current status, or null if none is known.
        /// </summary>
        public int? GetEffectiveChallengeId() {
            PathSegment segment = Mod.CurrentChapterPathSegmentList?.CurrentSegment;
            return segment?.ChallengeId ?? GetAutoChallenge(CurrentMapBin, Mod.IsInFgrMode)?.Id ?? segment?.ChallengeIdAuto;
        }

        public void ClearCache() {
            lock (CacheLock) {
                ChallengeCache.Clear();
            }
        }

        #endregion

        #region Auth

        public string GetAuthUrl() {
            int port = global::Celeste.Mod.Core.CoreModule.Settings.DebugRCPort;
            string returnUrl = $"http://localhost:{port}{AuthEndpointPath}";
            return $"{BaseUrl}/auth-application?name={Uri.EscapeDataString(AppName)}&scopes=ingame_status&return_url={Uri.EscapeDataString(returnUrl)}";
        }

        public void StartAuthorization() {
            string url = GetAuthUrl();
            Mod.Log($"Goldberries sync: opening auth page '{url}'");
            try {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: couldn't open the auth page: {ex}");
            }
        }

        /// <summary>
        /// Called by the DebugRC auth endpoint. Must be called on the main thread.
        /// </summary>
        public void SetApiKey(string apiKey) {
            Mod.Log($"Goldberries sync: received a new API key");
            Settings.GoldberriesApiKey = apiKey;
            Mod.SaveSettings();
            CheckSession();
        }

        /// <summary>
        /// Deletes the API key locally. The key stays valid on goldberries.net until revoked there.
        /// </summary>
        public void RevokeAuthorization() {
            SendExitBeforeDisabling();
            Settings.GoldberriesApiKey = null;
            Mod.SaveSettings();
            SetAuthState(AuthStatus.NotAuthorized, null);
        }

        /// <summary>
        /// Checks the API key against goldberries.net and fetches the name of the player it belongs to.
        /// </summary>
        public void CheckSession() {
            string apiKey = Settings.GoldberriesApiKey;
            if (string.IsNullOrEmpty(apiKey)) {
                SetAuthState(AuthStatus.NotAuthorized, null);
                return;
            }

            SetAuthState(AuthStatus.Checking, null);
            string baseUrl = BaseUrl;
            EnqueueJob(() => {
                string response = HttpRequest("GET", $"{baseUrl}/api/auth/check_session.php", apiKey, null, out int statusCode);

                AuthStatus state;
                string playerName = null;
                if (statusCode == 200) {
                    state = AuthStatus.Authorized;
                    try {
                        GbAccount account = JsonConvert.DeserializeObject<GbAccount>(response);
                        playerName = account?.Player?.Name ?? account?.ClaimedPlayer?.Name;
                    } catch (Exception ex) {
                        Mod.Log($"Goldberries sync: couldn't parse check_session response: {ex}");
                    }
                } else if (statusCode == 401 || statusCode == 403) {
                    state = AuthStatus.Invalid;
                } else {
                    state = AuthStatus.Unreachable;
                }
                Mod.Log($"Goldberries sync: session check -> {state} (status {statusCode}, player '{playerName}')");

                MainThreadHelper.Do(() => {
                    if (apiKey != Settings.GoldberriesApiKey) return;
                    SetAuthState(state, playerName);
                    if (state == AuthStatus.Authorized) {
                        LookupCurrentMap();
                        SendStatus();
                    }
                });
            });
        }

        private void SetAuthState(AuthStatus state, string playerName) {
            AuthState = state;
            AuthPlayerName = playerName;
            OnStateChanged?.Invoke();
        }

#if DEBUG
        /// <summary>
        /// Called when the target server changes (localhost toggle in DEBUG builds).
        /// </summary>
        public void ChangedServer() {
            ClearCache();
            CheckSession();
            LookupCurrentMap();
        }
#endif

        #endregion

        #region HTTP / Worker

        private void EnqueueJob(Action job) {
            if (Jobs == null || Jobs.IsAddingCompleted) return;
            try {
                Jobs.Add(job);
            } catch (InvalidOperationException) {
                // Unloading
            }
        }

        private void WorkerLoop() {
            foreach (Action job in Jobs.GetConsumingEnumerable()) {
                try {
                    job();
                } catch (Exception ex) {
                    Mod.Log($"Goldberries sync: job failed: {ex}");
                }
            }
        }

        /// <summary>
        /// Synchronous HTTP request, only call from the worker thread.
        /// </summary>
        /// <param name="statusCode">The HTTP status code, or 0 if the server couldn't be reached.</param>
        /// <returns>The response body, or null if the server couldn't be reached.</returns>
        private static string HttpRequest(string method, string url, string apiKey, string body, out int statusCode) {
            try {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Timeout = RequestTimeoutMs;
                request.ReadWriteTimeout = RequestTimeoutMs;
                request.Accept = "application/json";
                request.UserAgent = $"ConsistencyTracker/{Mod.Metadata?.VersionString}";
                if (apiKey != null) {
                    request.Headers["X-API-Key"] = apiKey;
                }

                if (body != null) {
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    request.ContentType = "application/json";
                    request.ContentLength = bytes.Length;
                    using (Stream stream = request.GetRequestStream()) {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    statusCode = (int)response.StatusCode;
                    return ReadBody(response);
                }
            } catch (WebException ex) when (ex.Response is HttpWebResponse errorResponse) {
                using (errorResponse) {
                    statusCode = (int)errorResponse.StatusCode;
                    return ReadBody(errorResponse);
                }
            } catch (Exception ex) {
                Mod.Log($"Goldberries sync: {method} '{url}' failed: {ex.Message}");
                statusCode = 0;
                return null;
            }
        }

        private static string ReadBody(HttpWebResponse response) {
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) {
                return reader.ReadToEnd();
            }
        }

        private static string Truncate(string value) {
            if (value == null) return null;
            value = value.Trim();
            if (value.Length == 0) return null;
            return value.Length > MaxStringLength ? value.Substring(0, MaxStringLength) : value;
        }

        #endregion
    }
}
