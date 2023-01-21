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

        //private static readonly List<RCEndPoint> EndPoints = new List<RCEndPoint>() {
        //    InfoEndPoint,
        //    StateEndPoint,
        //    CurrentChapterStatsEndPoint,
        //    CurrentChapterPathEndPoint,
        //    ParseFormatEndPoint
        //};

        private static readonly UpdateCache CurrentStateCache = new UpdateCache();
        private static readonly UpdateCache CurrentChapterStatsCache = new UpdateCache();
        private static readonly UpdateCache CurrentChapterPathCache = new UpdateCache();
        private static readonly UpdateCache ParseFormatCache = new UpdateCache();

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

                string response = null;
                string content = "CCT API up and running :thumbsup_emoji:";

                if (requestedJson) {
                    string field = FormatFieldJson("status", content);
                    response = FormatResponseJson(RCErrorCode.OK, field);
                } else {
                    string field = FormatFieldPlain("status", content);
                    response = FormatResponsePlain(RCErrorCode.OK, field);
                }

                WriteResponse(c, response);
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
                string response = null;

                if (mod.CurrentUpdateFrame <= CurrentStateCache.LastUpdateFrame && CurrentStateCache.LastRequestedJson == requestedJson) {
                    WriteResponse(c, CurrentStateCache.LastResponse);
                    return;
                }

                if (mod.CurrentChapterStats == null) {
                    WriteErrorResponse(c, RCErrorCode.StatsNotFound, requestedJson);
                    return;
                }


                if (requestedJson) {
                    string roomObj = FormatObjectStringJson("currentRoom", mod.CurrentChapterStats.CurrentRoom.ToJson());
                    string chapterName = FormatFieldJson("chapterName", mod.CurrentChapterStats.ChapterDebugName);
                    string modStateObj = FormatObjectStringJson("modState", mod.CurrentChapterStats.ModState.ToJson());
                    response = FormatResponseJson(RCErrorCode.OK, roomObj, chapterName, modStateObj);

                } else {
                    string currentRoom = mod.CurrentChapterStats.CurrentRoom.ToString();
                    string modState = $"{mod.CurrentChapterStats.ChapterDebugName};{mod.CurrentChapterStats.ModState}";
                    response = FormatResponsePlain(RCErrorCode.OK, currentRoom, modState);
                }

                CurrentStateCache.Update(mod.CurrentUpdateFrame, response, requestedJson);
                WriteResponse(c, response);
            }
        };

        // +------------------------------------------+
        // |             /cct/settings                |
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
                string response = null;


                string generalObj = FormatObjectFieldJson("general", new Dictionary<string, object>() {
                    ["colorblindMode"] = mod.ModSettings.ExternalOverlayColorblindMode,
                    ["fontFamily"] = mod.ModSettings.ExternalOverlayFontFamily,
                });
                string roomAttemptsObj = FormatObjectFieldJson("roomAttemptsDisplay", new Dictionary<string, object>() {
                    ["enabled"] = mod.ModSettings.ExternalOverlayRoomAttemptsDisplayEnabled,
                });
                string goldenShareObj = FormatObjectFieldJson("goldenShareDisplay", new Dictionary<string, object>() {
                    ["enabled"] = mod.ModSettings.ExternalOverlayGoldenShareDisplayEnabled,
                    ["showSession"] = mod.ModSettings.ExternalOverlayGoldenShareDisplayShowSession,
                });
                string goldenPBObj = FormatObjectFieldJson("goldenPBDisplay", new Dictionary<string, object>() {
                    ["enabled"] = mod.ModSettings.ExternalOverlayGoldenPBDisplayEnabled,
                });
                string chapterBarObj = FormatObjectFieldJson("chapterBarDisplay", new Dictionary<string, object>() {
                    ["enabled"] = mod.ModSettings.ExternalOverlayChapterBarEnabled,
                    ["borderWidthMultiplier"] = mod.ModSettings.ExternalOverlayChapterBorderWidthMultiplier,
                    ["lightGreenCutoff"] = ((float)mod.ModSettings.ExternalOverlayChapterBarLightGreenPercent / 100),
                    ["greenCutoff"] = ((float)mod.ModSettings.ExternalOverlayChapterBarGreenPercent / 100),
                    ["yellowCutoff"] = ((float)mod.ModSettings.ExternalOverlayChapterBarYellowPercent / 100),
                });
                string textStatsObj = FormatObjectFieldJson("textStatsDisplay", new Dictionary<string, object>() {
                    ["enabled"] = mod.ModSettings.ExternalOverlayTextDisplayEnabled,
                    ["preset"] = mod.ModSettings.ExternalOverlayTextDisplayPreset,
                    ["leftEnabled"] = mod.ModSettings.ExternalOverlayTextDisplayLeftEnabled,
                    ["middleEnabled"] = mod.ModSettings.ExternalOverlayTextDisplayMiddleEnabled,
                    ["rightEnabled"] = mod.ModSettings.ExternalOverlayTextDisplayRightEnabled,
                });

                string settingsObjString = FormatJson(generalObj, roomAttemptsObj, goldenShareObj, goldenPBObj, chapterBarObj, textStatsObj);
                string settingsObj = FormatObjectStringJson("settings", settingsObjString);

                response = FormatResponseJson(RCErrorCode.OK, settingsObj);

                WriteResponse(c, response);
            }
        };
        #endregion

        #region Stats Endpoints
        // +------------------------------------------+
        // |        /cct/currentChapterStats          |
        // +------------------------------------------+
        private static readonly RCEndPoint CurrentChapterStatsEndPoint = new RCEndPoint() {
            Path = "/cct/currentChapterStats",
            Name = "Consistency Tracker Current Chapter",
            InfoHTML = "Fetches the stats of the current chapter.",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string response = null;

                if (mod.CurrentUpdateFrame <= CurrentChapterStatsCache.LastUpdateFrame && CurrentChapterStatsCache.LastRequestedJson == requestedJson) {
                    WriteResponse(c, CurrentChapterStatsCache.LastResponse);
                    return;
                }

                if (mod.CurrentChapterStats == null) {
                    WriteErrorResponse(c, RCErrorCode.StatsNotFound, requestedJson);
                    return;
                }


                if (requestedJson) {
                    string chapterStats = FormatObjectStringJson("chapterStats", mod.CurrentChapterStats.ToJson());
                    response = FormatResponseJson(RCErrorCode.OK, chapterStats);

                } else {
                    string chapterStats = mod.CurrentChapterStats.ToChapterStatsString();
                    response = FormatResponsePlain(RCErrorCode.OK, chapterStats);
                }

                CurrentChapterStatsCache.Update(mod.CurrentUpdateFrame, response, requestedJson);
                WriteResponse(c, response);
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
                    response = FormatResponseJson(RCErrorCode.OK);

                } else {
                    response = FormatResponsePlain(RCErrorCode.OK);
                }

                CurrentChapterStatsCache.Update(mod.CurrentUpdateFrame, response, requestedJson);
                WriteResponse(c, response);
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
                string response = null;

                string singleFormat = null;
                if (c.Request.HttpMethod == "GET") {
                    singleFormat = GetQueryParameter(c, "format");
                } else if (c.Request.HttpMethod == "POST") {
                    if (c.Request.HasEntityBody) {
                        using (var reader = new StreamReader(c.Request.InputStream, c.Request.ContentEncoding)) {
                            singleFormat = reader.ReadToEnd();
                        }
                    } else {
                        WriteErrorResponse(c, RCErrorCode.PostNoBody, requestedJson);
                        return;
                    }
                } else {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedMethod, requestedJson, c.Request.HttpMethod);
                    return;
                }

                if (singleFormat == null || singleFormat == "") { //Missing parameter "format"
                    WriteErrorResponseWithDetails(c, RCErrorCode.MissingParamter, requestedJson, "format");
                    return;
                }

                string[] formatSplitLines = singleFormat.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

                List<string> requestedFormats = new List<string>();
                foreach (string line in formatSplitLines) {
                    requestedFormats.Add(line.Trim());
                }

                try {
                    for (int i = 0; i < requestedFormats.Count; i++) {
                        requestedFormats[i] = mod.StatsManager.FormatVariableFormat(requestedFormats[i]);
                    }
                } catch (NoStatPassException) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, "No stat pass has been done yet (enter a map first)");
                    return;
                }

                if (requestedJson) {
                    requestedFormats = FormatStringListForJson(requestedFormats);
                    string formats = FormatArrayJson("formats", requestedFormats);
                    response = FormatResponseJson(RCErrorCode.OK, formats);

                } else {
                    string content = string.Join(PlainLineSplitToken, requestedFormats);
                    response = FormatResponsePlain(RCErrorCode.OK, content);
                }

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
            Name = "Consistency Tracker Current Chapter",
            InfoHTML = "Fetches the stats of the current chapter.",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string response = null;

                if (mod.CurrentUpdateFrame <= CurrentChapterPathCache.LastUpdateFrame && CurrentChapterPathCache.LastRequestedJson == requestedJson) {
                    WriteResponse(c, CurrentChapterPathCache.LastResponse);
                    return;
                }

                if (mod.CurrentChapterPath == null || mod.CurrentChapterPath.RoomCount == 0) {
                    WriteErrorResponse(c, RCErrorCode.PathNotFound, requestedJson);
                    return;
                }


                if (requestedJson) {
                    string path = FormatObjectStringJson("path", mod.CurrentChapterPath.ToJson());
                    response = FormatResponseJson(RCErrorCode.OK, path);

                } else {
                    string chapterPathString = mod.CurrentChapterPath.ToString();
                    response = FormatResponsePlain(RCErrorCode.OK, chapterPathString);
                }

                CurrentChapterPathCache.Update(mod.CurrentUpdateFrame, response, requestedJson);
                WriteResponse(c, response);
            }
        };

        // +------------------------------------------+
        // |            /cct/listAllPaths             |
        // +------------------------------------------+
        private static readonly RCEndPoint ListAllPathsEndPoint = new RCEndPoint() {
            Path = "/cct/listAllPaths",
            Name = "Consistency Tracker List All Paths",
            InfoHTML = "Get a list of all available paths [json only]",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                //if (!requestedJson) {
                //    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                //    return;
                //}

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string response = null;

                string pathsFolder = ConsistencyTrackerModule.GetPathToFolder(ConsistencyTrackerModule.PathsFolder);
                string[] allPathFiles = Directory.GetFiles(pathsFolder);
                List<string> allPathFilesNames = new List<string>(allPathFiles.Select((path) => Path.GetFileNameWithoutExtension(path)));

                if (requestedJson) {
                    List<string> jsonFormattedFiles = FormatStringListForJson(allPathFilesNames);
                    string content = FormatArrayJson("availableMaps", jsonFormattedFiles);
                    response = FormatResponseJson(RCErrorCode.OK, content);

                } else {
                    string content = string.Join("\n", allPathFilesNames);
                    response = FormatResponsePlain(RCErrorCode.OK, content);
                }

                WriteResponse(c, response);
            }
        };


        // +------------------------------------------+
        // |            /cct/getPathFile              |
        // +------------------------------------------+
        private static readonly RCEndPoint GetPathFileEndPoint = new RCEndPoint() {
            Path = "/cct/getPathFile",
            PathHelp = "/cct/getPathFile?map={map}",
            Name = "Consistency Tracker Get Path File",
            InfoHTML = "Get the path file for a map [json only]",
            Handle = c => {
                bool requestedJson = HasRequestedJson(c);
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");

                if (!requestedJson) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.UnsupportedAccept, requestedJson, "text/plain");
                    return;
                }

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;
                string response = null;

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

                string content = File.ReadAllText(combinedPath);
                PathInfo pathInfo = null;
                try {
                    pathInfo = PathInfo.ParseString(content);
                } catch (Exception) {
                    WriteErrorResponseWithDetails(c, RCErrorCode.ExceptionOccurred, requestedJson, $"Couldn't parse contents of path file '{map}'");
                    return;
                }

                //Response
                string path = FormatObjectStringJson("path", pathInfo.ToJson());
                response = FormatResponseJson(RCErrorCode.OK, path);

                WriteResponse(c, response);
            }
        };
        #endregion



        #region Load / Unload
        public static void Load() {
            foreach (RCEndPoint endpoint in EndPoints) {
                Everest.DebugRC.EndPoints.Add(endpoint);
            }
            //Everest.DebugRC.EndPoints.Add(InfoEndPoint);
            //Everest.DebugRC.EndPoints.Add(StateEndPoint);
            //Everest.DebugRC.EndPoints.Add(CurrentChapterStatsEndPoint);
            //Everest.DebugRC.EndPoints.Add(CurrentChapterPathEndPoint);
            //Everest.DebugRC.EndPoints.Add(ParseFormatEndPoint);
        }
        public static void Unload() {
            foreach (RCEndPoint endpoint in EndPoints) {
                Everest.DebugRC.EndPoints.Remove(endpoint);
            }
            //Everest.DebugRC.EndPoints.Remove(InfoEndPoint);
            //Everest.DebugRC.EndPoints.Remove(StateEndPoint);
            //Everest.DebugRC.EndPoints.Remove(CurrentChapterStatsEndPoint);
            //Everest.DebugRC.EndPoints.Remove(CurrentChapterPathEndPoint);
            //Everest.DebugRC.EndPoints.Remove(ParseFormatEndPoint);
        }
        #endregion

        #region All Responses
        public static string GetQueryParameter(HttpListenerContext c, string parameter) {
            NameValueCollection args = Everest.DebugRC.ParseQueryString(c.Request.RawUrl);
            string value = args[parameter];
            if (value != null) return HttpUtility.UrlDecode(value);
            return null;
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
            return requestedJson ? FormatResponseJson(code) : FormatResponsePlain(code);
        }
        public static string GetErrorResponseWithDetails(RCErrorCode code, bool requestedJson, string details) {
            return requestedJson ? FormatResponseJson(code, new List<string>(), details) : FormatResponsePlain(code, new List<string>(), details);
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
        #endregion

        #region JSON Responses
        public static string FormatResponseJson(RCErrorCode code, params string[] values) {
            List<string> list = values.ToList();
            return FormatResponseJson(code, list);
        }
        public static string FormatResponseJson(RCErrorCode code, List<string> values, string errorCodeDetails=null) {
            string errorCode = ErrorCodeJson(code);
            string errorMessage = FormatFieldJson("errorMessage", code.ToMessage(errorCodeDetails));
            if (values == null) values = new List<string>();
            values.Add(errorCode);
            values.Add(errorMessage);
            return FormatJson(values);
        }
        public static string FormatJson(params string[] values) {
            List<string> list = values.ToList();
            return FormatJson(list);
        }
        public static string FormatJson(List<string> values) {
            string result = $"{{";

            foreach (string val in values) {
                result += $"{val},";
            }

            if (values.Count > 0) {
                result = result.Substring(0, result.Length - 1);
            }

            result += $"}}";

            return result;
        }


        public static string ErrorCodeJson(RCErrorCode code) {
            return $"\"errorCode\":{(int)code}";
        }

        public static string FormatObjectStringJson(string name, string jsonObject) {
            string result = $"\"{name}\":{jsonObject}";
            return result;
        }
        public static string FormatObjectFieldJson(string name, Dictionary<string, object> objects) {
            List<string> fields = new List<string>();

            foreach (string key in objects.Keys) {
                fields.Add(FormatFieldJson(key, objects[key]));
            }

            string objectString = FormatJson(fields);

            return FormatObjectStringJson(name, objectString);
        }
        public static string FormatArrayJson(string name, List<string> jsonStringsArray) {
            string result = $"\"{name}\":[";
            foreach (string jsonString in jsonStringsArray) {
                result += $"{jsonString},";
            }
            if (jsonStringsArray.Count > 0) {
                result = result.Substring(0, result.Length-1);
            }
            result += "]";
            return result;
        }
        public static string FormatFieldJson(string name, string value) {
            return $"\"{name}\":\"{value}\"";
        }
        public static string FormatFieldJson(string name, int value) {
            return $"\"{name}\":{value}";
        }
        public static string FormatFieldJson(string name, bool value) {
            return $"\"{name}\":{FormatBoolJson(value)}";
        }
        public static string FormatFieldJson(string name, double value) {
            return $"\"{name}\":{value}";
        }
        public static string FormatFieldJson(string name, float value) {
            return $"\"{name}\":{value}";
        }
        public static string FormatFieldJson(string name, object value) {
            if (value is int) return FormatFieldJson(name, (int)value);
            else if (value is bool) return FormatFieldJson(name, (bool)value);
            else if (value is float) return FormatFieldJson(name, (float)value);
            else if (value is double) return FormatFieldJson(name, (double)value);
            else if (value is string) return FormatFieldJson(name, (string)value);

            return $"\"{name}\":\"{value}\"";
        }
        public static string FormatBoolJson(bool b) {
            return b ? "true" : "false";
        }

        public static List<string> FormatStringListForJson(List<string> stringsList) {
            return new List<string>(stringsList.Select((s) => $"\"{s}\""));
        }
        public static List<string> FormatStringListForJson(params string[] stringsArray) {
            return new List<string>(stringsArray.Select((s) => $"\"{s}\""));
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
            string result = $"{name}{PlainFieldSplitToken}{FormatBoolJson(value)}";
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
            StateEndPoint,
            CurrentChapterStatsEndPoint,
            CurrentChapterPathEndPoint,
            ParseFormatEndPoint,
            ListAllPathsEndPoint,
            GetPathFileEndPoint,
            AddGoldenDeathEndPoint,
            SettingsEndPoint
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
