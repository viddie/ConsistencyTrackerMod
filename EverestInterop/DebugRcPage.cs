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
using Celeste.Mod.ConsistencyTracker.EverestInterop.Models;
using Newtonsoft.Json;
using Celeste.Mod.ConsistencyTracker.Stats;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop {
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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;
                string content = "CCT API up and running :thumbsup_emoji:";

                if (requestedJson) {
                    InfoResponse response = new InfoResponse() {
                        message = content,
                        modVersion = ConsistencyTrackerModule.ModVersion,
                        hasPath = mod.CurrentChapterPath != null,
                        hasStats = mod.CurrentChapterStats != null,
                        formatsLoaded = new List<StatFormat>(mod.StatsManager.Formats.Select((kv) => kv.Key)),
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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

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
                        chapterName = mod.CurrentChapterStats.ChapterDebugName,
                        modState = mod.CurrentChapterStats.ModState,
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string currentRoom = mod.CurrentChapterStats.CurrentRoom.ToString();
                    string modState = $"{mod.CurrentChapterStats.ChapterDebugName};{mod.CurrentChapterStats.ModState}";
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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

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
                            lightGreenCutoff = (float)mod.ModSettings.ExternalOverlayChapterBarLightGreenPercent / 100,
                            greenCutoff = (float)mod.ModSettings.ExternalOverlayChapterBarGreenPercent / 100,
                            yellowCutoff = (float)mod.ModSettings.ExternalOverlayChapterBarYellowPercent / 100,
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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

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
                        chapterStats = mod.CurrentChapterStats,
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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                //if (mod.CurrentUpdateFrame <= CurrentChapterPathCache.LastUpdateFrame && CurrentChapterPathCache.LastRequestedJson == requestedJson) {
                //    WriteResponse(c, CurrentChapterPathCache.LastResponse);
                //    return;
                //}

                if (mod.CurrentChapterPath == null || mod.CurrentChapterPath.RoomCount == 0) {
                    WriteErrorResponse(c, RCErrorCode.PathNotFound, requestedJson);
                    return;
                }


                if (requestedJson) {
                    ChapterPathResponse response = new ChapterPathResponse() {
                        path = mod.CurrentChapterPath,
                    };
                    responseStr = FormatResponseJson(RCErrorCode.OK, response);

                } else {
                    string chapterPathString = mod.CurrentChapterPath.ToString();
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
            Name = "Consistency Tracker List All Paths [JSON only]",
            InfoHTML = "Get a list of all available paths",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                string pathsFolder = ConsistencyTrackerModule.GetPathToFolder(ConsistencyTrackerModule.PathsFolder);
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
            Name = "Consistency Tracker Get Path File [GET] [JSON only]",
            InfoHTML = "Get the path file for a map",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                string map = GetQueryParameter(c, "map");
                if (map == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "map");
                    return;
                }

                map = map.Replace(".", "");
                map = map.Replace("/", "");
                map = map.Replace("\\", "");

                string baseFolder = ConsistencyTrackerModule.GetPathToFolder(ConsistencyTrackerModule.PathsFolder);
                string combinedPath = Path.Combine(baseFolder, $"{map}.txt");
                if (!File.Exists(combinedPath)) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't read file '{combinedPath}'");
                    return;
                }
                
                PathInfo pathInfo = null;
                try {
                    pathInfo = mod.GetPathInputInfo(map);
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
            Name = "Consistency Tracker Set Path File [POST] [JSON only]",
            InfoHTML = "Set the path file for a map. Put path info object in the body of the POST request ",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;

                string map = GetQueryParameter(c, "map");
                if (map == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "map");
                    return;
                }

                map = map.Replace(".", "");
                map = map.Replace("/", "");
                map = map.Replace("\\", "");

                string baseFolder = ConsistencyTrackerModule.GetPathToFolder(ConsistencyTrackerModule.PathsFolder);
                string combinedPath = Path.Combine(baseFolder, $"{map}.txt");

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
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
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
                        requestedFormats[i] = mod.StatsManager.FormatVariableFormat(requestedFormats[i]);
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
        // |            /cct/getFormat              |
        // +------------------------------------------+
        private static readonly RCEndPoint GetFormatEndPoint = new RCEndPoint() {
            Path = "/cct/getFormat",
            PathHelp = "/cct/getFormat?format={formatName}",
            Name = "Consistency Tracker Get Live-Data Format",
            InfoHTML = "Gets the result of an existing live-data format.",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string responseStr = null;
                
                string format = GetQueryParameter(c, "format");

                if (format == null) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "format");
                    return;
                }

                if (!mod.StatsManager.HadPass) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "No stat pass has been done yet (enter a map first)");
                    return;
                }

                string text = mod.StatsManager.GetLastPassFormatText(format);

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

        private static bool HasRequestedJson(HttpListenerContext c) {
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

            c.Response.AddHeader("Content-Type", responseType);
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
            SettingsEndPoint,
            StateEndPoint,
            CurrentChapterStatsEndPoint,
            AddGoldenDeathEndPoint,
            CurrentChapterPathEndPoint,
            ListAllPathsEndPoint,
            GetPathFileEndPoint,
            SetPathFileEndPoint,
            ParseFormatEndPoint,
            GetFormatEndPoint
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
