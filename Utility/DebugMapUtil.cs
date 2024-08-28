using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Editor;
using Monocle;
using System.Web.UI;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class DebugMapUtil {

        public static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        public PathRecorder PathRec = new PathRecorder();
        public bool IsRecording { get; set; } = false;

        public DebugMapUtil() {}

        #region Hooks
        public void Hook() {
            On.Celeste.Editor.MapEditor.Render += MapEditor_Render;
            On.Celeste.Editor.MapEditor.SelectionCheck += MapEditor_SelectionCheck;
            On.Celeste.Editor.MapEditor.ctor += MapEditor_ctor;
        }
        
        public void UnHook() {
            On.Celeste.Editor.MapEditor.Render -= MapEditor_Render;
            On.Celeste.Editor.MapEditor.SelectionCheck -= MapEditor_SelectionCheck;
            On.Celeste.Editor.MapEditor.ctor -= MapEditor_ctor;
        }
        #endregion

        #region Path Recording 
        public void StartRecording() {
            PathRec = new PathRecorder();
            PathRec.AddCheckpoint(Vector2.Zero, PathRecorder.DefaultCheckpointName);
            IsRecording = true;
        }

        public void StopRecording() {
            PathInfo path = PathRec.ToPathInfo();
            
            ChapterMetaInfo chapterInfo = null;
            if (Engine.Scene is Level level && level.Session != null) {
                chapterInfo = new ChapterMetaInfo(level.Session);
            }
            Mod.SetCurrentChapterPath(path, chapterInfo);
            Mod.Log($"Recorded path:\n{JsonConvert.SerializeObject(Mod.CurrentChapterPath)}", isFollowup: true);
            Mod.SavePathToFile();
            Mod.SaveChapterStats();//Output stats with updated path
            
            IsRecording = false;
            PathRec = null;
        }

        public void AbortRecording() {
            IsRecording = false;
            PathRec = null;
        }
        #endregion

        #region Events
        private void MapEditor_Render(On.Celeste.Editor.MapEditor.orig_Render orig, MapEditor self) {
            orig(self);

            List<LevelTemplate> levels = Util.GetPrivateProperty<List<LevelTemplate>>(self, "levels");
            Camera camera = Util.GetPrivateStaticProperty<Camera>(self, "Camera");
            ConsistencyTrackerSettings settings = Mod.ModSettings;
            PathInfo currentPath = Mod.CurrentChapterPath;

            if (IsRecording) {
                StartSpriteBatch();

                foreach (LevelTemplate template in levels) {
                    for (int cpIndex = 0; cpIndex < PathRec.Checkpoints.Count; cpIndex++) {
                        for (int rIndex = 0; rIndex < PathRec.Checkpoints[cpIndex].Count; rIndex++) {
                            string rDebugName = PathRec.Checkpoints[cpIndex][rIndex];
                            if (template.Name != rDebugName) continue;

                            bool isTransition = PathRec.IsTransitionRoom(template.Name);
                            string displayName = $"{PathRec.CheckpointAbbreviations[cpIndex]}-{rIndex + 1}";
                            Color color = Color.Teal;
                            float scaleDivider = 6f;
                            
                            if (isTransition) {
                                displayName += $"\nTransition";
                                color = Color.Orange;
                                scaleDivider += 2;
                            }
                            
                            DrawTextOnRoom(template, camera, displayName, scaleDivider);
                            OutlineRoom(template, camera, color);
                        }
                    }
                }

                EndSpriteBatch();
                return;
            }

            
            if (currentPath == null) return;

            if (settings.ShowCCTRoomNamesOnDebugMap) {
                StartSpriteBatch();

                foreach (LevelTemplate template in levels) {
                    string name = template.Name;
                    RoomInfo rInfo = currentPath.GetRoom(name);
                    if (rInfo == null) {
                        string resolvedName = Mod.ResolveGroupedRoomName(name);
                        rInfo = currentPath.GetRoom(resolvedName);

                        if (rInfo == null) {
                            continue;
                        }
                    }

                    string formattedName;
                    int scaleDivider = 6;

                    if (settings.LiveDataCustomNameBehavior == CustomNameBehavior.Override || settings.LiveDataCustomNameBehavior == CustomNameBehavior.Ignore) {
                        formattedName = rInfo.GetFormattedRoomName(settings.LiveDataRoomNameDisplayType);
                    } else {
                        formattedName = rInfo.GetFormattedRoomName(settings.LiveDataRoomNameDisplayType, CustomNameBehavior.Override);
                        if (!string.IsNullOrEmpty(rInfo.CustomRoomName)) {
                            formattedName += $"\n{rInfo.GetFormattedRoomName(settings.LiveDataRoomNameDisplayType, CustomNameBehavior.Ignore)}";
                            scaleDivider += 2;
                        }
                    }


                    // PacePingManager.PaceTiming paceTiming = Mod.PacePingManager.GetPaceTiming(currentPath.ChapterSID, rInfo.DebugRoomName, dontLog: true);
                    PacePingManager.PaceTiming paceTiming = Mod.MultiPacePingManager.Get(0).GetPaceTiming(currentPath.ChapterSID, rInfo.DebugRoomName, dontLog: true);

                    if (paceTiming != null) {
                        formattedName = $"{formattedName}\n>Ping<";
                        scaleDivider += 2;
                    }

                    DrawTextOnRoom(template, camera, formattedName, scaleDivider);
                }

                EndSpriteBatch();
            }

            if (settings.ShowSuccessRateBordersOnDebugMap) { //Insert Mod Option setting here
                StartSpriteBatch();

                foreach (LevelTemplate template in levels) {
                    string name = template.Name;
                    RoomInfo rInfo = currentPath.GetRoom(name);
                    if (rInfo == null) {
                        string resolvedName = Mod.ResolveGroupedRoomName(name);
                        rInfo = currentPath.GetRoom(resolvedName);

                        if (rInfo == null) {
                            continue;
                        }
                    }

                    RoomStats rStats = Mod.CurrentChapterStats.GetRoom(rInfo);
                    Color color = Color.Gray;
                    float successRate = rStats.AverageSuccessOverSelectedN() * 100;
                    if (successRate >= settings.LiveDataChapterBarLightGreenPercent) {
                        color = new Color(0, 230, 0);
                    } else if (successRate >= settings.LiveDataChapterBarGreenPercent) {
                        color = Color.Green;
                    } else if (successRate >= settings.LiveDataChapterBarYellowPercent) {
                        color = new Color(194, 194, 41);
                    } else if (!float.IsNaN(successRate)) {
                        color = new Color(231, 45, 45);
                    }

                    OutlineRoom(template, camera, color);
                }

                EndSpriteBatch();
            }
        }
        
        private bool MapEditor_SelectionCheck(On.Celeste.Editor.MapEditor.orig_SelectionCheck orig, MapEditor self, Vector2 point) {
            if (!IsRecording) {
                return orig(self, point);
            }

            List<LevelTemplate> levels = Util.GetPrivateProperty<List<LevelTemplate>>(self, "levels");
            LevelTemplate template = FindLevelTemplateByPoint(levels, point);

            if (template != null) {
                Mod.Log($"Clicked on {template.Name} ({template.X}, {template.Y}): Room contains {template.Checkpoints.Count} checkpoints and {template.Spawns.Count} respawns");

                if (PathRec.ContainsRoom(template.Name)) {
                    if (PathRec.IsTransitionRoom(template.Name)) {
                        bool removed = PathRec.RemoveRoom(template.Name);
                        if (!removed) {
                            PathRec.SetTransitionRoom(template.Name, false);
                        }
                    } else {
                        PathRec.SetTransitionRoom(template.Name, true);
                    }
                } else {
                    PathRec.AddRoom(template.Name);
                    if (template.Checkpoints.Count > 0) {
                        string cpDialogName = $"{Mod.CurrentChapterStats.ChapterSIDDialogSanitized}_{template.Name}";
                        Mod.Log($"cpDialogName: {cpDialogName}");
                        string cpName = Dialog.Get(cpDialogName);
                        Mod.Log($"Dialog.Get says: {cpName}");
                        if (cpName.StartsWith("[") && cpName.EndsWith("]")) cpName = null;

                        PathRec.AddCheckpoint(template.Checkpoints.First(), cpName);
                    }
                }
                
                
            } else {
                Mod.Log($"Clicked on empty space at ({point.X}, {point.Y})");
            }

            return orig(self, point);
        }

        private void MapEditor_ctor(On.Celeste.Editor.MapEditor.orig_ctor orig, MapEditor self, AreaKey area, bool reloadMapData) {
            orig(self, area, reloadMapData);
            Mod.Log($"Opened Debug Map, saving stats...");
            Mod.SaveChapterStats();
        }
        #endregion

        #region Util
        public static Vector2 ScreenToDebugMap(Vector2 point, Camera camera) {
            point -= new Vector2(960f, 540f);
            point /= camera.Zoom;
            point += camera.Position;
            return point;
        }
        public static LevelTemplate FindLevelTemplateByPoint(List<LevelTemplate> levels, Vector2 point) {
            //Find the first level template that contains the point
            //positive X direction is right, positive Y direction is down
            foreach (LevelTemplate template in levels) {
                if (template.X <= point.X && point.X <= template.X + template.Width) {
                    if (template.Y <= point.Y && point.Y <= template.Y + template.Height) {
                        return template;
                    }
                }
            }
            return null;
        }
        public static Vector2 DebugMapToScreen(Vector2 point, Camera camera) {
            point -= camera.Position;
            point = new Vector2((float)Math.Ceiling(point.X), (float)Math.Ceiling(point.Y));
            point *= camera.Zoom;
            point += new Vector2(960f, 540f);
            return point;
        }

        #endregion

        #region Draw Functions
        public static void StartSpriteBatch() {
            Draw.SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Engine.ScreenMatrix);
        }
        public static void EndSpriteBatch() {
            Draw.SpriteBatch.End();
        }
        
        public void DrawTextOnRoom(LevelTemplate template, Camera camera, string text, float scaleDivider = 6) {
            Vector2 pos = DebugMapToScreen(new Vector2(template.X + template.Width / 2, template.Y), camera);
            ActiveFont.DrawOutline(
                text,
                pos,
                new Vector2(0.5f, 0),
                Vector2.One * camera.Zoom / scaleDivider,
                Color.White * 0.9f,
                2f * camera.Zoom / 6,
                Color.Black * 0.7f);
        }

        public void OutlineRoom(LevelTemplate template, Camera camera, Color color) {
            Vector2 pos = DebugMapToScreen(new Vector2(template.X, template.Y), camera);

            //Draw.Rect(pos, width * camera.Zoom, height * camera.Zoom, new Color(color.R, color.G, color.B, 0));
            Vector2 topRight = pos + new Vector2(template.Width * camera.Zoom, 0);
            Vector2 bottomLeft = pos + new Vector2(0, template.Height * camera.Zoom);
            Vector2 bottomRight = pos + new Vector2(template.Width * camera.Zoom, template.Height * camera.Zoom);
            
            float thickness = 1.5f * camera.Zoom;
            Draw.Line(pos, topRight, color, thickness);
            Draw.Line(pos, bottomLeft, color, thickness);
            Draw.Line(topRight, bottomRight, color, thickness);
            Draw.Line(bottomLeft, bottomRight, color, thickness);
        }
        #endregion
    }
}
