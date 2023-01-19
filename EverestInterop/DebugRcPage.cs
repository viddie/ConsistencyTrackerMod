using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using Celeste.Mod;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop {
    public enum RCErrorCode {
        OK = 0,
        StatsNotFound = 1,
        PathNotFound = 2,
    }

    public static class DebugRcPage {

        private static UpdateCache CurrentStateCache = new UpdateCache();
        private static UpdateCache CurrentChapterStatsCache = new UpdateCache();
        private static UpdateCache CurrentChapterPathCache = new UpdateCache();

        private static readonly RCEndPoint InfoEndPoint = new RCEndPoint() {
            Path = "/cct/info",
            Name = "Consistency Tracker Info",
            InfoHTML = "List some CCT info.",
            Handle = c => {
                StringBuilder builder = new StringBuilder();
                //Everest.DebugRC.WriteHTMLStart(c, builder);
                //WriteLine(builder, $"{{ \"test\": \"Message :D\" }}");
                builder.Append($"{{ \"test\": \"Message :D\" }}");
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");
                //Everest.DebugRC.WriteHTMLEnd(c, builder);
                Everest.DebugRC.Write(c, builder.ToString());
            }
        };

        private static readonly RCEndPoint StateEndPoint = new RCEndPoint() {
            Path = "/cct/state",
            Name = "Consistency Tracker State",
            InfoHTML = "Fetches the current state of the mod.",
            Handle = c => {
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");
                c.Response.AddHeader("Content-Type", "application/json");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;

                if (mod.CurrentUpdateFrame <= CurrentStateCache.LastUpdateFrame) {
                    Everest.DebugRC.Write(c, CurrentStateCache.LastResponse);
                }

                string message = null;
                if (mod.CurrentChapterStats == null) {
                    message = GetErrorMessageJson(RCErrorCode.PathNotFound);
                    Everest.DebugRC.Write(c, message);
                    return;
                }

                message = $"{mod.CurrentChapterStats.CurrentRoom}\n{mod.CurrentChapterStats.ChapterDebugName};{mod.CurrentChapterStats.ModState}\n";

                CurrentStateCache.LastResponse = message;
                CurrentStateCache.LastUpdateFrame = mod.CurrentUpdateFrame;

                Everest.DebugRC.Write(c, message);
            }
        };

        //private static readonly RCEndPoint StateEndPoint = new RCEndPoint() {
        //    Path = "/cct/state",
        //    Name = "Consistency Tracker State",
        //    InfoHTML = "Fetches the current state of the mod.",
        //    Handle = c => {
        //        c.Response.AddHeader("Access-Control-Allow-Origin", "*");
        //        c.Response.AddHeader("Content-Type", "application/json");

        //        ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;

        //        if (mod.CurrentUpdateFrame <= CurrentStateCache.LastUpdateFrame) {
        //            Everest.DebugRC.Write(c, CurrentStateCache.LastResponse);
        //        }

        //        string message = null;
        //        if (mod.CurrentChapterStats == null) {
        //            message = GetErrorMessageJson(RCErrorCode.PathNotFound);
        //            Everest.DebugRC.Write(c, message);
        //            return;
        //        }

        //        string roomObj = JsonFormatObject("currentRoom", mod.CurrentChapterStats.CurrentRoom.ToJson());
        //        string chapterName = JsonFormatField("chapterName", mod.CurrentChapterStats.ChapterDebugName);
        //        string modStateObj = JsonFormatObject("modState", mod.CurrentChapterStats.ModState.ToJson());
        //        //builder.Append($"{{ {ErrorCodeJson(RCErrorCode.OK)}, {roomObj}, {chapterName}, {modStateObj} }}");

        //        message = JsonFormat(RCErrorCode.OK, roomObj, chapterName, modStateObj);

        //        CurrentStateCache.LastResponse = message;
        //        CurrentStateCache.LastUpdateFrame = mod.CurrentUpdateFrame;

        //        Everest.DebugRC.Write(c, message);
        //    }
        //};

        private static readonly RCEndPoint CurrentChapterStatsEndPoint = new RCEndPoint() {
            Path = "/cct/currentChapterStats",
            Name = "Consistency Tracker Current Chapter",
            InfoHTML = "Fetches the stats of the current chapter.",
            Handle = c => {
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");
                c.Response.AddHeader("Content-Type", "application/json");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;

                if (mod.CurrentUpdateFrame <= CurrentChapterStatsCache.LastUpdateFrame) {
                    Everest.DebugRC.Write(c, CurrentChapterStatsCache.LastResponse);
                }

                string message = null;
                if (mod.CurrentChapterStats == null) {
                    message = GetErrorMessageJson(RCErrorCode.PathNotFound);
                    Everest.DebugRC.Write(c, message);
                    return;
                }

                //string roomObj = JsonFormatObject("currentRoom", mod.CurrentChapterStats.CurrentRoom.ToJson());
                //string chapterName = JsonFormatField("chapterName", mod.CurrentChapterStats.ChapterDebugName);
                //string modStateObj = JsonFormatObject("modState", mod.CurrentChapterStats.ModState.ToJson());
                //message = JsonFormat(RCErrorCode.OK, roomObj, chapterName, modStateObj);

                message = mod.CurrentChapterStats.ToChapterStatsString();

                CurrentChapterStatsCache.LastResponse = message;
                CurrentChapterStatsCache.LastUpdateFrame = mod.CurrentUpdateFrame;

                Everest.DebugRC.Write(c, message);
            }
        };

        private static readonly RCEndPoint CurrentChapterPathEndPoint = new RCEndPoint() {
            Path = "/cct/currentChapterPath",
            Name = "Consistency Tracker Current Chapter",
            InfoHTML = "Fetches the stats of the current chapter.",
            Handle = c => {
                c.Response.AddHeader("Access-Control-Allow-Origin", "*");
                c.Response.AddHeader("Content-Type", "application/json");

                ConsistencyTrackerModule mod = ConsistencyTrackerModule.Instance;

                if (mod.CurrentUpdateFrame <= CurrentChapterPathCache.LastUpdateFrame) {
                    Everest.DebugRC.Write(c, CurrentChapterPathCache.LastResponse);
                }

                string message = null;
                if (mod.CurrentChapterPath == null || mod.CurrentChapterPath.RoomCount == 0) {
                    //message = GetErrorMessageJson(RCErrorCode.PathNotFound);
                    //Everest.DebugRC.Write(c, message);
                    Everest.DebugRC.Write(c, "");
                    return;
                }

                //string roomObj = JsonFormatObject("currentRoom", mod.CurrentChapterStats.CurrentRoom.ToJson());
                //string chapterName = JsonFormatField("chapterName", mod.CurrentChapterStats.ChapterDebugName);
                //string modStateObj = JsonFormatObject("modState", mod.CurrentChapterStats.ModState.ToJson());
                //message = JsonFormat(RCErrorCode.OK, roomObj, chapterName, modStateObj);

                message = mod.CurrentChapterPath.ToString();

                CurrentChapterPathCache.LastResponse = message;
                CurrentChapterPathCache.LastUpdateFrame = mod.CurrentUpdateFrame;

                Everest.DebugRC.Write(c, message);
            }
        };


        public static void Load() {
            Everest.DebugRC.EndPoints.Add(InfoEndPoint);
            Everest.DebugRC.EndPoints.Add(StateEndPoint);
            Everest.DebugRC.EndPoints.Add(CurrentChapterStatsEndPoint);
            Everest.DebugRC.EndPoints.Add(CurrentChapterPathEndPoint);
        }

        public static void Unload() {
            Everest.DebugRC.EndPoints.Remove(InfoEndPoint); 
            Everest.DebugRC.EndPoints.Remove(StateEndPoint);
            Everest.DebugRC.EndPoints.Remove(CurrentChapterStatsEndPoint);
            Everest.DebugRC.EndPoints.Remove(CurrentChapterPathEndPoint);
        }

        private static void WriteLine(StringBuilder builder, string text) {
            builder.Append($@"{text}<br />");
        }
        private static string GetErrorMessageJson(RCErrorCode code) {
            string errorMessage = JsonFormatField("errorMessage", ErrorCodeToMessage(code));
            return JsonFormat(code, errorMessage);
            //builder.Append($"{{ \"errorMessage\": \"{text}\", \"errorCode\": {(int)code} }}");
        }
        private static string ErrorCodeJson(RCErrorCode code) {
            return $"\"errorCode\":{(int)code}";
        }

        private static string JsonFormatObject(string name, string jsonObject) {
            string result = $"\"{name}\":{jsonObject}";
            return result;
        }
        private static string JsonFormatField(string name, string value) {
            string result = $"\"{name}\":\"{value}\"";
            return result;
        }
        private static string JsonFormatField(string name, int value) {
            string result = $"\"{name}\":{value}";
            return result;
        }
        private static string JsonFormatField(string name, bool value) {
            string result = $"\"{name}\":{JsonFormatBool(value)}";
            return result;
        }

        private static string JsonFormat(RCErrorCode code, params string[] values) {
            string result = $"{{ {ErrorCodeJson(RCErrorCode.OK)}, ";
            foreach (string val in values) {
                result += $"{val}, ";
            }

            result = result.Substring(0, result.Length - 2);
            result += $" }}";

            return result;
        }

        private static string JsonFormatBool(bool b) {
            return b ? "true" : "false";
        }

        private static string ErrorCodeToMessage(RCErrorCode code, string details = null) {
            switch (code) {
                case RCErrorCode.OK:
                    return $"OK";
                case RCErrorCode.StatsNotFound:
                    return $"Stats not found";
                case RCErrorCode.PathNotFound:
                    return $"Path not found";
                default:
                    return "";
            }
        }

        private class UpdateCache {
            public long LastUpdateFrame { get; set; } = -1;
            public string LastResponse { get; set; }
        }
    }
}
