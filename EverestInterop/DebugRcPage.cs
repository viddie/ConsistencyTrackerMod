using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Celeste.Mod;
using System.Web;
using Celeste.Mod.ConsistencyTracker.Exceptions;
using Celeste.Mod.ConsistencyTracker.Models;
using Newtonsoft.Json;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.PhysicsLog;
using Monocle;
using Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses;
using Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Requests;
using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop
{
    public enum RCErrorCode {
        OK = 0,
        StatsNotFound = 1,
        PathNotFound = 2,
        MissingParamter = 3,
        ExceptionOccurred = 4,
        UnsupportedMethod = 5,
        PostNoBody = 6,
        UnsupportedAccept = 7,
    }

    public static class DebugRcPage {

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        
        private static readonly UpdateCache CurrentStateCache = new UpdateCache();
        private static readonly UpdateCache CurrentChapterStatsCache = new UpdateCache();
        //private static readonly UpdateCache CurrentChapterPathCache = new UpdateCache();
        //private static readonly UpdateCache ParseFormatCache = new UpdateCache();

        // +------------------------------------------+
        // |               /cct/info                  |
        // +------------------------------------------+
        private static readonly RCEndPoint InfoEndPoint = new RCEndPoint() {
            Path = "/cct/info",
            Name = "Consistency Tracker Info",
            InfoHTML = "List some CCT info.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;
                string content = "CCT API up and running :thumbsup_emoji:";

                if (requestedJson) {
                    InfoResponse response = new InfoResponse() {
                        message = content,
                        modVersion = ConsistencyTrackerModule.VersionsNewest.Mod,
                        hasPath = Mod.CurrentChapterPath != null,
                        hasStats = Mod.CurrentChapterStats != null,
                        formatsLoaded = new List<StatFormat>(Mod.StatsManager.Formats.Select((kv) => kv.Key)),
                    };

                    responseStr = FormatResponseJson(RCErrorCode.OK, response);
                } else {
                    string field = FormatFieldPlain("status", content);
                    responseStr = FormatResponsePlain(RCErrorCode.OK, field);
                }

                WriteResponse(c, responseStr);
            }
        };

        #region Overlay Endpoints
        // +------------------------------------------+
        // |              /cct/state                  |
        // +------------------------------------------+
        private static readonly RCEndPoint StateEndPoint = new RCEndPoint() {
            Path = "/cct/state",
            Name = "Consistency Tracker State",
            InfoHTML = "Fetches the current state of the mod.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                if (mod.CurrentUpdateFrame <= CurrentStateCache.LastUpdateFrame && CurrentStateCache.LastRequestedJson == requestedJson) {
                    WriteResponse(c, CurrentStateCache.LastResponse);
                    return;
                }

                if (mod.CurrentChapterStats == null) {
                    WriteErrorResponse(c, RCErrorCode.StatsNotFound, requestedJson);
                    return;
                }


                if (requestedJson) {
                    StateResponse response = new StateResponse() {
                        currentRoom = mod.CurrentChapterStats.CurrentRoom,
                        chapterName = mod.CurrentChapterStats.ChapterUID,
                        modState = mod.CurrentChapterStats.ModState,
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string currentRoom = mod.CurrentChapterStats.CurrentRoom.ToString();
                    string modState = $"{mod.CurrentChapterStats.ChapterUID};{mod.CurrentChapterStats.ModState}";
                    responseStr = FormatResponsePlain(RCErrorCode.OK, currentRoom, modState);
                }

                CurrentStateCache.Update(mod.CurrentUpdateFrame, responseStr, requestedJson);
                WriteResponse(c, responseStr);
            }
        };

        // +------------------------------------------+
        // |          /cct/overlaySettings            |
        // +------------------------------------------+
        private static readonly RCEndPoint SettingsEndPoint = new RCEndPoint() {
            Path = "/cct/overlaySettings",
            Name = "Consistency Tracker Settings",
            InfoHTML = "Gets the current overlay settings from the mod.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                SettingsResponse response = new SettingsResponse() {
                    settings = new SettingsResponse.Settings() {
                        general = new SettingsResponse.General() {
                            refreshTimeSeconds = mod.ModSettings.ExternalOverlayRefreshTimeSeconds,
                            attemptsCount = mod.ModSettings.ExternalOverlayAttemptsCount,
                            textOutlineSize = mod.ModSettings.ExternalOverlayTextOutlineSize,
                            fontFamily = mod.ModSettings.ExternalOverlayFontFamily,
                            colorblindMode = mod.ModSettings.ExternalOverlayColorblindMode,
                        },
                        roomAttemptsDisplay = new SettingsResponse.RoomAttemptsDisplay() {
                            enabled = mod.ModSettings.ExternalOverlayRoomAttemptsDisplayEnabled,
                        },
                        goldenShareDisplay = new SettingsResponse.GoldenShareDisplay() {
                            enabled = mod.ModSettings.ExternalOverlayGoldenShareDisplayEnabled,
                            showSession = mod.ModSettings.ExternalOverlayGoldenShareDisplayShowSession,
                        },
                        goldenPBDisplay = new SettingsResponse.GoldenPBDisplay() {
                            enabled = mod.ModSettings.ExternalOverlayGoldenPBDisplayEnabled,
                        },
                        chapterBarDisplay = new SettingsResponse.ChapterBarDisplay() {
                            enabled = mod.ModSettings.ExternalOverlayChapterBarEnabled,
                            borderWidthMultiplier = mod.ModSettings.ExternalOverlayChapterBorderWidthMultiplier,
                            lightGreenCutoff = (float)mod.ModSettings.LiveDataChapterBarLightGreenPercent / 100,
                            greenCutoff = (float)mod.ModSettings.LiveDataChapterBarGreenPercent / 100,
                            yellowCutoff = (float)mod.ModSettings.LiveDataChapterBarYellowPercent / 100,
                        },
                        textStatsDisplay = new SettingsResponse.TextStatsDisplay() {
                            enabled = mod.ModSettings.ExternalOverlayTextDisplayEnabled,
                            preset = mod.ModSettings.ExternalOverlayTextDisplayPreset,
                            leftEnabled = mod.ModSettings.ExternalOverlayTextDisplayLeftEnabled,
                            middleEnabled = mod.ModSettings.ExternalOverlayTextDisplayMiddleEnabled,
                            rightEnabled = mod.ModSettings.ExternalOverlayTextDisplayRightEnabled,
                        }
                    },
                };

                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };
        #endregion

        #region Stats Endpoints
        // +------------------------------------------+
        // |        /cct/currentChapterStats          |
        // +------------------------------------------+
        private static readonly RCEndPoint CurrentChapterStatsEndPoint = new RCEndPoint() {
            Path = "/cct/currentChapterStats",
            Name = "Consistency Tracker Current Chapter Stats",
            InfoHTML = "Fetches the stats of the current chapter.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                if (mod.CurrentUpdateFrame <= CurrentChapterStatsCache.LastUpdateFrame && CurrentChapterStatsCache.LastRequestedJson == requestedJson) {
                    WriteResponse(c, CurrentChapterStatsCache.LastResponse);
                    return;
                }

                if (mod.CurrentChapterStats == null) {
                    WriteErrorResponse(c, RCErrorCode.StatsNotFound, requestedJson);
                    return;
                }
                
                if (requestedJson) {
                    ChapterStatsResponse response = new ChapterStatsResponse() {
                        chapterStats = mod.CurrentChapterStats
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string chapterStats = mod.CurrentChapterStats.ToChapterStatsString();
                    responseStr = FormatResponsePlain(RCErrorCode.OK, chapterStats);
                }

                CurrentChapterStatsCache.Update(mod.CurrentUpdateFrame, responseStr, requestedJson);
                WriteResponse(c, responseStr);
            }
        };

        // +------------------------------------------+
        // |           /cct/addGoldenDeath            |
        // +------------------------------------------+
        private static readonly RCEndPoint AddGoldenDeathEndPoint = new RCEndPoint() {
            Path = "/cct/addGoldenDeath",
            PathHelp = "/cct/addGoldenDeath?room={roomDebugName}",
            Name = "Consistency Tracker Add Golden Death",
            InfoHTML = "Adds a golden death to the specified room.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string response = null;

                if (mod.CurrentChapterStats == null) {
                    WriteErrorResponse(c, RCErrorCode.StatsNotFound, requestedJson);
                    return;
                }

                string roomDebugName = GetQueryParameter(c, "room");
                if (roomDebugName == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "room");
                    return;
                }

                if (!mod.CurrentChapterStats.Rooms.ContainsKey(roomDebugName)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Did not find the room '{roomDebugName}' in the current chapter");
                    return;
                }

                mod.CurrentChapterStats.AddGoldenBerryDeath(roomDebugName);
                mod.SaveChapterStats();

                if (requestedJson) {
                    response = FormatResponseJson(RCErrorCode.OK, new Response());

                } else {
                    response = FormatResponsePlain(RCErrorCode.OK);
                }

                CurrentChapterStatsCache.Update(mod.CurrentUpdateFrame, response, requestedJson);
                WriteResponse(c, response);
            }
        };
        #endregion

        #region Path Endpoints
        // +------------------------------------------+
        // |         /cct/currentChapterPath          |
        // +------------------------------------------+
        private static readonly RCEndPoint CurrentChapterPathEndPoint = new RCEndPoint() {
            Path = "/cct/currentChapterPath",
            Name = "Consistency Tracker Current Chapter Path",
            InfoHTML = "Fetches the stats of the current chapter.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;

                //if (mod.CurrentUpdateFrame <= CurrentChapterPathCache.LastUpdateFrame && CurrentChapterPathCache.LastRequestedJson == requestedJson) {
                //    WriteResponse(c, CurrentChapterPathCache.LastResponse);
                //    return;
                //}

                if (Mod.CurrentChapterPath == null || Mod.CurrentChapterPath.RoomCount == 0) {
                    WriteErrorResponse(c, RCErrorCode.PathNotFound, requestedJson);
                    return;
                }


                if (requestedJson) {
                    ChapterPathResponse response = new ChapterPathResponse() {
                        path = Mod.CurrentChapterPath,
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string chapterPathString = Mod.CurrentChapterPath.ToString();
                    responseStr = FormatResponsePlain(RCErrorCode.OK, chapterPathString);
                }

                //CurrentChapterPathCache.Update(mod.CurrentUpdateFrame, responseStr, requestedJson);
                WriteResponse(c, responseStr);
            }
        };

        // +------------------------------------------+
        // |            /cct/listAllPaths             |
        // +------------------------------------------+
        private static readonly RCEndPoint ListAllPathsEndPoint = new RCEndPoint() {
            Path = "/cct/listAllPaths",
            Name = "Consistency Tracker List All Paths [JSON]",
            InfoHTML = "Get a list of all available paths",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                string responseStr = null;

                string pathsFolder = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.PathsFolder);
                string[] allPathFiles = Directory.GetFiles(pathsFolder);
                List<string> allMapNames = new List<string>(allPathFiles.Select((path) => Path.GetFileNameWithoutExtension(path)));

                //if (requestedJson) {
                PathListResponse response = new PathListResponse() {
                    mapNames = allMapNames,
                };
                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                //}
                //else {
                //    string content = string.Join("\n", allPathFilesNames);
                //    responseStr = FormatResponsePlain(RCErrorCode.OK, content);
                //}

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |            /cct/getPathFile              |
        // +------------------------------------------+
        private static readonly RCEndPoint GetPathFileEndPoint = new RCEndPoint() {
            Path = "/cct/getPathFile",
            PathHelp = "/cct/getPathFile?map={map}",
            Name = "Consistency Tracker Get Path File [GET] [JSON]",
            InfoHTML = "Get the path file for a map",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                string responseStr = null;

                string map = GetQueryParameter(c, "map");
                if (map == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "map");
                    return;
                }

                string extension = GetQueryParameter(c, "extension");
                if (extension == null) extension = "json";

                map = map.Replace(".", "");
                map = map.Replace("/", "");
                map = map.Replace("\\", "");

                string combinedPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.PathsFolder, $"{map}.{extension}");
                if (!File.Exists(combinedPath)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't read file '{combinedPath}'");
                    return;
                }
                
                PathInfo pathInfo = null;
                try {
                    PathSegmentList pathList = Mod.GetPathSegmentList(ConsistencyTrackerModule.PathsFolder, map);
                    pathInfo = pathList.CurrentPath;
                } catch (Exception) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse contents of path file '{map}'");
                    return;
                }

                //Response
                ChapterPathResponse response = new ChapterPathResponse() {
                    path = pathInfo,
                };
                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |            /cct/setPathFile              |
        // +------------------------------------------+
        private static readonly RCEndPoint SetPathFileEndPoint = new RCEndPoint() {
            Path = "/cct/setPathFile",
            PathHelp = "/cct/setPathFile?map={map}",
            Name = "Consistency Tracker Set Path File [POST] [JSON]",
            InfoHTML = "Set the path file for a map. Put path info object in the body of the POST request ",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                string responseStr = null;

                string map = GetQueryParameter(c, "map");
                if (map == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "map");
                    return;
                }

                map = map.Replace(".", "");
                map = map.Replace("/", "");
                map = map.Replace("\\", "");

                string combinedPath = ConsistencyTrackerModule.GetPathToFile(ConsistencyTrackerModule.PathsFolder, $"{map}.txt");

                SetPathFileRequest request = null;
                if (c.Request.HttpMethod == "POST") {
                    if (c.Request.HasEntityBody) {
                        string body = GetBodyAsString(c);
                        try {
                            request = JsonConvert.DeserializeObject<SetPathFileRequest>(body);
                        } catch (Exception ex) {
                            WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse request json: {ex}");
                            return;
                        }
                    } else {
                        WriteErrorResponse(c, RCErrorCode.PostNoBody, requestedJson);
                        return;
                    }
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedMethod, requestedJson, c.Request.HttpMethod);
                    return;
                }

                if (request.path == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "path");
                    return;
                }

                try {
                    File.WriteAllText(combinedPath, JsonConvert.SerializeObject(request.path));
                } catch (Exception) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't write contents of path file '{map}'");
                    return;
                }

                //Response
                responseStr = FormatResponseJson(RCErrorCode.OK);

                WriteResponse(c, responseStr);
            }
        };
        #endregion

        #region Combined Stats/Path Endpoints
        // +------------------------------------------+
        // |         /cct/recentChapterData           |
        // +------------------------------------------+
        private static readonly RCEndPoint RecentChapterDataEndPoint = new RCEndPoint() {
            Path = "/cct/recentChapterData",
            Name = "Consistency Tracker Recent Chapter Data",
            InfoHTML = "Fetches the stats/paths of the recently entered maps in the current session.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                List<RecentChapterDataResponse.ChapterData> data = new List<RecentChapterDataResponse.ChapterData>();
                foreach (var item in Mod.LastVisitedChapters) {
                    int selectedIndex = item.Item2 == null ? 0 : item.Item2.SelectedIndex;
                    data.Add(new RecentChapterDataResponse.ChapterData() {
                        path = item.Item2 == null ? null : item.Item2.CurrentPath,
                        stats = item.Item1.SegmentStats[selectedIndex],
                    });
                }
                
                RecentChapterDataResponse response = new RecentChapterDataResponse() {
                    data = data
                };
                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };
        #endregion

        #region Format Endpoints
        // +------------------------------------------+
        // |            /cct/parseFormat              |
        // +------------------------------------------+
        private static readonly RCEndPoint ParseFormatEndPoint = new RCEndPoint() {
            Path = "/cct/parseFormat",
            PathHelp = "/cct/parseFormat?format={format}",
            Name = "Consistency Tracker Parse Live-Data Format",
            InfoHTML = "Parses any arbitrary live-data format. If the format is too long for a GET request, put it in the body of a POST request.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;

                bool isGetRequest = c.Request.HttpMethod == "GET";
                string getFormat = null;
                ParseFormatRequest postRequest = null;

                if (c.Request.HttpMethod == "GET") {
                    getFormat = GetQueryParameter(c, "format");

                } else if (c.Request.HttpMethod == "POST") {
                    if (c.Request.HasEntityBody) {
                        string body = GetBodyAsString(c);
                        try {
                            postRequest = JsonConvert.DeserializeObject<ParseFormatRequest>(body);
                        } catch (Exception ex) {
                            WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse request json: {ex}");
                            return;
                        }
                    } else {
                        WriteErrorResponse(c, RCErrorCode.PostNoBody, requestedJson);
                        return;
                    }
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedMethod, requestedJson, c.Request.HttpMethod);
                    return;
                }

                if (isGetRequest && getFormat == null) { //Missing parameter "format"
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "format");
                    return;

                } else if (!isGetRequest && postRequest.formats == null) { //Missing parameter "format"
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "formats");
                    return;
                }

                List<string> requestedFormats = new List<string>();

                if (isGetRequest) {
                    requestedFormats.Add(getFormat);
                } else {
                    requestedFormats = postRequest.formats;
                }

                DateTime startTime = DateTime.Now;
                try {
                    for (int i = 0; i < requestedFormats.Count; i++) {
                        requestedFormats[i] = Mod.StatsManager.FormatVariableFormat(requestedFormats[i]);
                    }
                } catch (NoStatPassException) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "No stat pass has been done yet (enter a map first)");
                    return;
                }
                DateTime endTime = DateTime.Now;

                if (requestedJson) {
                    ParseFormatResponse response = new ParseFormatResponse() {
                        formats = requestedFormats,
                        calculationTime = (endTime - startTime).TotalMilliseconds,
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string content = string.Join(PlainLineSplitToken, requestedFormats);
                    responseStr = FormatResponsePlain(RCErrorCode.OK, content);
                }

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |             /cct/getFormat               |
        // +------------------------------------------+
        private static readonly RCEndPoint GetFormatEndPoint = new RCEndPoint() {
            Path = "/cct/getFormat",
            PathHelp = "/cct/getFormat?format={formatName}",
            Name = "Consistency Tracker Get Live-Data Format",
            InfoHTML = "Gets the result of an existing live-data format.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;

                string format = GetQueryParameter(c, "format");

                if (format == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "format");
                    return;
                }

                if (!Mod.StatsManager.HadPass) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "No stat pass has been done yet (enter a map first)");
                    return;
                }

                string text = Mod.StatsManager.GetLastPassFormatText(format);

                if (text == null) { //format didnt exist
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Live-Data format '{format}' does not exist");
                    return;
                }


                if (requestedJson) {
                    GetFormatResponse response = new GetFormatResponse() {
                        format = format,
                        text = text,
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string content = text;
                    responseStr = FormatResponsePlain(RCErrorCode.OK, content);
                }

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |         /cct/getPlaceholderList          |
        // +------------------------------------------+
        private static readonly RCEndPoint GetPlaceholderListEndPoint = new RCEndPoint() {
            Path = "/cct/getPlaceholderList",
            PathHelp = "/cct/getPlaceholderList",
            Name = "Consistency Tracker Get Placeholder List [JSON]",
            InfoHTML = "Gets all available placeholders for live-data formatting.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;

                
                List<GetPlaceholderListResponse.Placeholder> placeholders = new List<GetPlaceholderListResponse.Placeholder>();
                foreach (var kvp in Mod.StatsManager.GetPlaceholderExplanationList()) {
                    placeholders.Add(new GetPlaceholderListResponse.Placeholder() {
                        name = kvp.Key,
                        description = kvp.Value,
                    });
                }
                GetPlaceholderListResponse response = new GetPlaceholderListResponse() {
                    placeholders = placeholders,
                };
                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |            /cct/getFormatsList           |
        // +------------------------------------------+
        private static readonly RCEndPoint GetFormatListEndPoint = new RCEndPoint() {
            Path = "/cct/getFormatsList",
            PathHelp = "/cct/getFormatsList",
            Name = "Consistency Tracker Get Formats List [JSON]",
            InfoHTML = "Gets all available formats for live-data.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;

                GetFormatListResponse response = new GetFormatListResponse() {
                    defaultFormats = Mod.StatsManager.GetAvailableDefaultFormatList(),
                    customFormats = Mod.StatsManager.GetCustomFormatList(),
                };
                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |              /cct/saveFormat             |
        // +------------------------------------------+
        private static readonly RCEndPoint SaveFormatEndPoint = new RCEndPoint() {
            Path = "/cct/saveFormat",
            PathHelp = "/cct/saveFormat",
            Name = "Consistency Tracker Save Live-Data Format [POST] [JSON]",
            InfoHTML = "Saves or deletes Live-Data Format. Body must contain 'name' and 'format', if format is empty, the request is treated as delete operation",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                string responseStr = null;

                SaveFormatRequest postRequest = null;

                if (c.Request.HttpMethod == "POST") {
                    if (c.Request.HasEntityBody) {
                        string body = GetBodyAsString(c);
                        try {
                            postRequest = JsonConvert.DeserializeObject<SaveFormatRequest>(body);
                        } catch (Exception ex) {
                            WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse request json: {ex}");
                            return;
                        }
                    } else {
                        WriteErrorResponse(c, RCErrorCode.PostNoBody, requestedJson);
                        return;
                    }
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedMethod, requestedJson, c.Request.HttpMethod);
                    return;
                }

                if (postRequest.name == null || postRequest.name == "") {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "Name must not be empty");
                    return;
                }

                bool isDelete = postRequest.format == "";

                if (isDelete) {
                    bool success = Mod.StatsManager.DeleteFormat(postRequest.name);
                    if (success) {
                        responseStr = FormatResponseJson(RCErrorCode.OK);
                    } else {
                        WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't delete format '{postRequest.name}' as it doesn't exist");
                        return;
                    }
                } else {
                    bool exists = Mod.StatsManager.HasFormat(postRequest.name);
                    if (exists) {
                        bool success = Mod.StatsManager.UpdateFormat(postRequest.name, postRequest.format);
                        if (success) {
                            responseStr = FormatResponseJson(RCErrorCode.OK);
                        } else {
                            WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't update format '{postRequest.name}' as it doesn't exist");
                            return;
                        }
                    } else {
                        Mod.StatsManager.CreateFormat(postRequest.name, postRequest.format);
                        responseStr = FormatResponseJson(RCErrorCode.OK);
                    }
                }


                responseStr = FormatResponseJson(RCErrorCode.OK);
                WriteResponse(c, responseStr);
            }
        };
        #endregion

        #region Misc File Endpoints
        // +------------------------------------------+
        // |           /cct/getFileContent            |
        // +------------------------------------------+
        private static readonly RCEndPoint GetFileContentEndpoint = new RCEndPoint() {
            Path = "/cct/getFileContent",
            PathHelp = "/cct/getFileContent?folder={folder}&file={file}&extension={extension}&subfolder=[subfolder]",
            Name = "Consistency Tracker Get Misc File [GET] [JSON]",
            InfoHTML = "Get any consistency tracker file.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                string responseStr = null;

                string folderName = GetQueryParameter(c, "folder");
                if (folderName == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "folder");
                    return;
                }


                string subFolderName = GetQueryParameter(c, "subfolder");
                string fileName = GetQueryParameter(c, "file");
                if (fileName == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "file");
                    return;
                }
                string extension = GetQueryParameter(c, "extension");
                if (fileName == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "extension");
                    return;
                }

                folderName = SanitizeFolderFileName(folderName);
                subFolderName = SanitizeFolderFileName(subFolderName);
                fileName = SanitizeFolderFileName(fileName);
                extension = SanitizeFolderFileName(extension);
                foreach (var manager in Mod.MultiPacePingManager.GetManagers()) {
                    if ($"{fileName}.{extension}" == manager.SaveStateSecretFileName)
                    {
                        WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"This file is protected from being read via this API");
                        return;
                    }

                }

                string combinedPath;
                if (subFolderName == null) {
                    combinedPath = ConsistencyTrackerModule.GetPathToFile(folderName, $"{fileName}.{extension}");
                } else {
                    combinedPath = ConsistencyTrackerModule.GetPathToFile(folderName, subFolderName, $"{fileName}.{extension}");
                }
                
                if (!File.Exists(combinedPath)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't read file '{combinedPath}'");
                    return;
                }

                
                //Lock for specifically the physics recordings
                Object lockObj = new Object();
                if (folderName == "physics-recordings") {
                    lockObj = PhysicsRecordingsManager.LogFileLock;
                }
                //--------------------------------------------
                string content;
                lock (lockObj) {
                    content = File.ReadAllText(combinedPath);
                }

                //Response
                GetFileContentResponse response = new GetFileContentResponse() {
                    fileName = $"{fileName}.{extension}",
                    fileContent = content,
                };
                responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };

        private static string SanitizeFolderFileName(string name) {
            if (name == null) return null;
            
            name = name.Replace(".", "");
            name = name.Replace("/", "");
            name = name.Replace("\\", "");
            return name;
        }
        
        #endregion

        #region Physics Log Endpoints
        // +------------------------------------------+
        // |          /cct/getPhysicsLogList          |
        // +------------------------------------------+
        private static readonly RCEndPoint GetPhysicsLogFileListEndpoint = new RCEndPoint() {
            Path = "/cct/getPhysicsLogList",
            PathHelp = "/cct/getPhysicsLogList",
            Name = "Consistency Tracker Get Physics Log List [GET] [JSON]",
            InfoHTML = "Lists the available physics log files, acquireable via '/cct/getFileContent' endpoint.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                GetPhysicsLogFileListResponse response = new GetPhysicsLogFileListResponse() { 
                    IsRecording = PhysicsLogger.Settings.IsRecording && Engine.Scene is Level,
                };

                List<PhysicsLogLayoutsFile> recentFiles = Mod.PhysicsLog.RecordingsManager.GetRecentRecordingsLayoutFiles();
                int recordingOffset = PhysicsRecordingsManager.MostRecentRecording;
                for (int i = 0; i < recentFiles.Count; i++) {
                    PhysicsLogLayoutsFile file = recentFiles[i];
                    response.RecentPhysicsLogFiles.Add(new GetPhysicsLogFileListResponse.PhysicsLogFile() {
                        ID = i + recordingOffset,
                        Name = null,
                        SID = file.SID,
                        MapBin = file.MapBin,
                        ChapterName = file.ChapterName,
                        SideName = file.SideName,
                        FrameCount = file.FrameCount,
                        RecordingStarted = file.RecordingStarted,
                    });
                }

                List<PhysicsRecordingsState.PhysicsRecording> savedRecordings = Mod.PhysicsLog.RecordingsManager.GetSavedRecordings();
                for (int i = 0; i < savedRecordings.Count; i++) {
                    PhysicsRecordingsState.PhysicsRecording recording = savedRecordings[i];
                    response.SavedPhysicsRecordings.Add(new GetPhysicsLogFileListResponse.PhysicsLogFile() {
                        ID = recording.ID,
                        Name = recording.Name,
                        SID = recording.SID,
                        MapBin = recording.MapBin,
                        ChapterName = recording.ChapterName,
                        SideName = recording.SideName,
                        FrameCount = recording.FrameCount,
                        RecordingStarted = recording.RecordingStarted,
                    });
                }

                string responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |            /cct/saveRecording            |
        // +------------------------------------------+
        private static readonly RCEndPoint SaveRecordingEndpoint = new RCEndPoint() {
            Path = "/cct/saveRecording",
            PathHelp = "/cct/saveRecording",
            Name = "Consistency Tracker Save Recording [POST] [JSON]",
            InfoHTML = "Saves a physics recording to the saved recordings list.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                SaveRecordingRequest postRequest = null;

                if (c.Request.HttpMethod == "POST") {
                    if (c.Request.HasEntityBody) {
                        string body = GetBodyAsString(c);
                        try {
                            postRequest = JsonConvert.DeserializeObject<SaveRecordingRequest>(body);
                        } catch (Exception ex) {
                            WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse request json: {ex}");
                            return;
                        }
                    } else {
                        WriteErrorResponse(c, RCErrorCode.PostNoBody, requestedJson);
                        return;
                    }
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedMethod, requestedJson, c.Request.HttpMethod);
                    return;
                }

                if (postRequest == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "Couldn't parse request json");
                    return;
                }

                int id = Mod.PhysicsLog.RecordingsManager.SaveRecording(postRequest.LayoutFile, postRequest.PhysicsLog, postRequest.Name);

                IdResponse response = new IdResponse() {
                    ID = id,
                };
                string responseStr = FormatResponseJson(RCErrorCode.OK, response);

                WriteResponse(c, responseStr);
            }
        };


        // +------------------------------------------+
        // |           /cct/renameRecording           |
        // +------------------------------------------+
        private static readonly RCEndPoint RenameRecordingEndpoint = new RCEndPoint() {
            Path = "/cct/renameRecording",
            PathHelp = "/cct/renameRecording",
            Name = "Consistency Tracker Rename Recording [POST] [JSON]",
            InfoHTML = "Renames a physics recording in the saved recordings list.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                RenameRecordingRequest postRequest = null;

                if (c.Request.HttpMethod == "POST") {
                    if (c.Request.HasEntityBody) {
                        string body = GetBodyAsString(c);
                        try {
                            postRequest = JsonConvert.DeserializeObject<RenameRecordingRequest>(body);
                        } catch (Exception ex) {
                            WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse request json: {ex}");
                            return;
                        }
                    } else {
                        WriteErrorResponse(c, RCErrorCode.PostNoBody, requestedJson);
                        return;
                    }
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedMethod, requestedJson, c.Request.HttpMethod);
                    return;
                }

                if (postRequest == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "Couldn't parse request json");
                    return;
                }

                
                bool success = Mod.PhysicsLog.RecordingsManager.RenameRecording(postRequest.ID, postRequest.Name);
                if (success) {
                    WriteResponse(c, RCErrorCode.OK, requestedJson);
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "Couldn't find recording with id " + postRequest.ID);
                    return;
                }
            }
        };

        // +------------------------------------------+
        // |            /cct/deleteRecording          |
        // +------------------------------------------+
        private static readonly RCEndPoint DeleteRecordingEndpoint = new RCEndPoint() {
            Path = "/cct/deleteRecording",
            PathHelp = "/cct/deleteRecording?id={id}",
            Name = "Consistency Tracker Delete Recording [POST] [JSON]",
            InfoHTML = "Deletes a physics recording from the saved recordings list.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                string idStr = GetQueryParameter(c, "id");
                if (idStr == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "id");
                    return;
                }

                if (!int.TryParse(idStr, out int id)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse id '{idStr}' to an integer");
                    return;
                }


                bool success = Mod.PhysicsLog.RecordingsManager.DeleteRecording(id);
                if (success) {
                    WriteResponse(c, RCErrorCode.OK, requestedJson);
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "Couldn't find recording with id " + id);
                    return;
                }
            }
        };

        // +------------------------------------------+
        // |            /cct/segmentRecording         |
        // +------------------------------------------+
        private static readonly RCEndPoint SegmentPhysicsLogEndpoint = new RCEndPoint() {
            Path = "/cct/segmentRecording",
            PathHelp = "/cct/segmentRecording",
            Name = "Consistency Tracker Segment Physics Log [GET]",
            InfoHTML = "Manually segment the physics recording",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                Mod.PhysicsLog.SegmentLog(false);
                WriteResponse(c, RCErrorCode.OK, requestedJson);
            }
        };

        #endregion

        #region Meta Endpoints
        private static readonly RCEndPoint SetRootFolderEndPoint = new RCEndPoint() {
            Path = "/cct/setRootFolder",
            PathHelp = "/cct/setRootFolder?path={path}",
            Name = "Consistency Tracker Set Root Folder [GET] [JSON]",
            InfoHTML = "Sets the root folder of CCT. Omit the parameter to reset to default folder location. Game restart required to take effect.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                string responseStr = null;
                string message = "";

                string path = GetQueryParameter(c, "path");
                if (path == null) {
                    message = $"Root folder location is: {Mod.ModSettings.DataRootFolderLocation}";
                } else if (path == "") {
                    Mod.ModSettings.DataRootFolderLocation = null;
                    message = $"Root folder reset to default.";

                    Mod.SaveSettings();
                } else {
                    Mod.ModSettings.DataRootFolderLocation = path;
                    message = $"Root folder location set to: {path}";

                    Mod.SaveSettings();
                }

                TextResponse response = new TextResponse() {
                    text = message,
                };

                //Response
                responseStr = FormatResponseJson(RCErrorCode.OK, response);
                WriteResponse(c, responseStr);
            }
        };
        
        private static readonly RCEndPoint MadelineScreenPositionEndPoint = new RCEndPoint() {
            Path = "/cct/madelineScreenPosition",
            PathHelp = "/cct/madelineScreenPosition?x={x}&y={y}&width={width}&height={height}",
            Name = "Consistency Tracker Madeline Screen Position [GET] [JSON]",
            InfoHTML = "Checks if Madeline is within the specified rectangle on the screen. If no parameters are specified, the current position is returned.",
            Handle = c => {
                bool requestedJson = CheckRequest(c);
                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }
                
                Player player = Mod.GetPlayer();
                if (player == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Player not found (might be dead)");
                    return;
                }
                Scene scene = Engine.Scene;
                if (!(scene is Level)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Player is not in a level");
                    return;
                }

                Level level = (Level)scene;

                string xStr = GetQueryParameter(c, "x");
                string yStr = GetQueryParameter(c, "y");
                string widthStr = GetQueryParameter(c, "width");
                string heightStr = GetQueryParameter(c, "height");

                Vector2 screenPos = player.Position - level.Camera.Position;

                MadelineScreenPositionResponse response = new MadelineScreenPositionResponse() {
                    madelineScreenPosition = screenPos,
                };
                
                if (xStr == null || yStr == null || widthStr == null || heightStr == null) {
                    WriteResponse(c, FormatResponseJson(RCErrorCode.OK, response));
                    return;
                }
                
                if (!int.TryParse(xStr, out int x)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse x '{xStr}' to an integer");
                    return;
                }
                if (!int.TryParse(yStr, out int y)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse y '{yStr}' to an integer");
                    return;
                }
                if (!int.TryParse(widthStr, out int width)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse width '{widthStr}' to an integer");
                    return;
                }
                if (!int.TryParse(heightStr, out int height)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse height '{heightStr}' to an integer");
                    return;
                }
                
                bool isInRectangle = screenPos.X >= x && screenPos.X <= x + width && screenPos.Y >= y && screenPos.Y <= y + height;

                response.isInBounds = isInRectangle;
                response.x = x;
                response.y = y;
                response.width = width;
                response.height = height;
                
                WriteResponse(c, FormatResponseJson(RCErrorCode.OK, response));
            }
        };
        #endregion

        #region Load / Unload
        public static void Load() {
            foreach (RCEndPoint endpoint in EndPoints) {
                Everest.DebugRC.EndPoints.Add(endpoint);
            }
        }
        public static void Unload() {
            foreach (RCEndPoint endpoint in EndPoints) {
                Everest.DebugRC.EndPoints.Remove(endpoint);
            }
        }
        #endregion

        #region All Responses
        public static string GetQueryParameter(HttpListenerContext c, string parameter) {
            NameValueCollection args = Everest.DebugRC.ParseQueryString(c.Request.RawUrl);
            string value = args[parameter];
            if (value != null) return HttpUtility.UrlDecode(value);
            return null;
        }

        public static string GetBodyAsString(HttpListenerContext c) {
            string body = null;
            using (var reader = new StreamReader(c.Request.InputStream, c.Request.ContentEncoding)) {
                body = reader.ReadToEnd();
            }
            return body;
        }

        public static void WriteErrorResponseWithDetails(HttpListenerContext c, RCErrorCode code, bool requestedJson, string details) {
            string response = GetErrorResponseWithDetails(code, requestedJson, details);
            Everest.DebugRC.Write(c, response);
        }
        public static void WriteErrorResponse(HttpListenerContext c, RCErrorCode code, bool requestedJson) {
            string response = GetErrorResponse(code, requestedJson);
            Everest.DebugRC.Write(c, response);
        }
        public static void WriteResponse(HttpListenerContext c, string response) {
            Everest.DebugRC.Write(c, response);
        }
        public static void WriteResponse(HttpListenerContext c, RCErrorCode code, bool requestedJson) {
            WriteErrorResponse(c, code, requestedJson);
        }

        public static string GetErrorResponse(RCErrorCode code, bool requestedJson) {
            return requestedJson ? FormatErrorResponseJson(code) : FormatResponsePlain(code);
        }
        public static string GetErrorResponseWithDetails(RCErrorCode code, bool requestedJson, string details) {
            return requestedJson ? FormatErrorResponseJson(code, details) : FormatResponsePlain(code, new List<string>(), details);
        }

        public static string ToMessage(this RCErrorCode code, string details=null) {
            switch (code) {
                case RCErrorCode.OK:
                    return $"OK";
                case RCErrorCode.StatsNotFound:
                    return $"Stats not found";
                case RCErrorCode.PathNotFound:
                    return $"Path not found";
                case RCErrorCode.MissingParamter:
                    return $"Parameter '{details}' was not found";
                case RCErrorCode.ExceptionOccurred:
                    return $"An Exception occurred: {details}";
                case RCErrorCode.UnsupportedMethod:
                    return $"HTTP Method '{details}' is not supported by this endpoint";
                case RCErrorCode.PostNoBody:
                    return $"No body was attached to the POST request";
                case RCErrorCode.UnsupportedAccept:
                    return $"The Accept type '{details}' is not supported by this endpoint";
                default:
                    return "";
            }
        }

        /// <summary>
        /// Checks for the Accept header and returns true if it's application/json
        /// </summary>
        private static bool CheckRequest(HttpListenerContext c, bool allowCrossOrigin = true) {
            bool requestedJson = true;
            string responseType = "application/json";

            string[] acceptTypes = c.Request.AcceptTypes;
            if (acceptTypes.Length > 0) {
                foreach (string value in acceptTypes) {
                    if (value == "text/plain") {
                        requestedJson = false;
                        responseType = "text/plain";
                        break;
                    }
                }
            }

            Mod.LogVerbose($"API request to '{c.Request.RawUrl}', Content-Type: '{responseType}'");
            
            c.Response.AddHeader("Content-Type", responseType);
            if (allowCrossOrigin) { 
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");
            }
            
            return requestedJson;
        }

        public static string FormatBool(bool b) {
            return b ? "true" : "false";
        }
        #endregion

        #region JSON Responses
        public static string FormatResponseJson(RCErrorCode code) {
            Response response = new Response {
                errorCode = (int)code,
                errorMessage = code.ToMessage()
            };
            return JsonConvert.SerializeObject(response);
        }
        public static string FormatResponseJson(RCErrorCode code, Response response) {
            response.errorCode = (int)code;
            response.errorMessage = code.ToMessage();
            return JsonConvert.SerializeObject(response);
        }
        public static string FormatErrorResponseJson(RCErrorCode code, string details=null) {
            Response response = new Response {
                errorCode = (int)code,
                errorMessage = code.ToMessage(details)
            };
            return JsonConvert.SerializeObject(response);
        }
        #endregion

        #region Plain Responses
        public static readonly string PlainFieldSplitToken = ";";
        public static readonly string PlainLineSplitToken = "\n";

        public static string FormatResponsePlain(RCErrorCode code, params string[] values) {
            List<string> list = values.ToList();
            return FormatResponsePlain(code, list);
        }
        public static string FormatResponsePlain(RCErrorCode code, List<string> values, string errorDetails=null) {
            string result = $"{ErrorCodeAndMessagePlain(code, errorDetails)}{PlainLineSplitToken}";

            foreach (string val in values) {
                result += $"{val}{PlainLineSplitToken}";
            }

            return result;
        }

        public static string FormatFieldPlain(string name, string value) {
            string result = $"{name}{PlainFieldSplitToken}{value}";
            return result;
        }
        public static string FormatFieldPlain(string name, int value) {
            string result = $"{name}{PlainFieldSplitToken}{value}";
            return result;
        }
        public static string FormatFieldPlain(string name, bool value) {
            string result = $"{name}{PlainFieldSplitToken}{FormatBool(value)}";
            return result;
        }

        public static string ErrorCodeAndMessagePlain(RCErrorCode code, string errorDetails=null) {
            return $"{(int)code}{PlainFieldSplitToken}{code.ToMessage(errorDetails)}";
        }
        #endregion



        //THIS FIELD MUST BE THIS FAR DOWN
        //because of static initialization order, otherwise the list elements are just new RCEndPoint() objects (with null fields)
        private static readonly List<RCEndPoint> EndPoints = new List<RCEndPoint>() {
            InfoEndPoint,

            //General
            SettingsEndPoint,
            StateEndPoint,

            //Stats
            CurrentChapterStatsEndPoint,
            AddGoldenDeathEndPoint,

            //Paths
            CurrentChapterPathEndPoint,
            ListAllPathsEndPoint,
            GetPathFileEndPoint,
            SetPathFileEndPoint,

            //Combined
            RecentChapterDataEndPoint,
            
            //Live-Data
            ParseFormatEndPoint,
            GetFormatEndPoint,
            SaveFormatEndPoint,
            GetPlaceholderListEndPoint,
            GetFormatListEndPoint,

            //Misc File Reading
            GetFileContentEndpoint,

            //Physics Logs
            GetPhysicsLogFileListEndpoint,
            SaveRecordingEndpoint,
            RenameRecordingEndpoint,
            DeleteRecordingEndpoint,
            SegmentPhysicsLogEndpoint,

            //Other
            SetRootFolderEndPoint,
            MadelineScreenPositionEndPoint,
        };

        private class UpdateCache {
            public long LastUpdateFrame { get; set; } = -1;
            public string LastResponse { get; set; }
            public bool LastRequestedJson { get; set; }

            public void Update(long frame, string response, bool requestedJson) {
                LastUpdateFrame = frame;
                LastResponse = response;
                LastRequestedJson = requestedJson;
            }
        }
    }
}
