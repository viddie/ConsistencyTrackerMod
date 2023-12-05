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

namespace Celeste.Mod.ConsistencyTracker.Utility {
    public class DebugMapUtil {

        public static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        public PathRecorder PathRec;
        public bool IsRecording { get; set; }

        public DebugMapUtil() {}

        #region Hooks
        public void Hook() {
            On.Celeste.Editor.MapEditor.Render += MapEditor_Render;
            On.Celeste.Editor.MapEditor.SelectionCheck += MapEditor_SelectionCheck;
        }
        public void UnHook() {
            On.Celeste.Editor.MapEditor.Render -= MapEditor_Render;
            On.Celeste.Editor.MapEditor.SelectionCheck -= MapEditor_SelectionCheck;
        }
        #endregion

        #region Path Recording 
        public void StartRecording() {
            PathRec = new PathRecorder();
            IsRecording = true;
        }

        public void StopRecording() {
            PathInfo path = PathRec.ToPathInfo();
            Mod.CurrentChapterPath = path;
            Mod.SavePathToFile();

            IsRecording = false;
            PathRec = null;
        }

        public void AbortRecording() {
            IsRecording = false;
            PathRec = null;
        }
        #endregion

        #region Events
        public void MapEditor_Render(On.Celeste.Editor.MapEditor.orig_Render orig, MapEditor self) {
            orig(self);

            if (Mod.CurrentChapterPath == null) return;

            List<LevelTemplate> levels = Util.GetPrivateProperty<List<LevelTemplate>>(self, "levels");
            Camera camera = Util.GetPrivateStaticProperty<Camera>(self, "Camera");
            ConsistencyTrackerSettings settings = Mod.ModSettings;
            PathInfo currentPath = Mod.CurrentChapterPath;

            if (settings.ShowCCTRoomNamesOnDebugMap) {
                Draw.SpriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp,
                        DepthStencilState.None,
                        RasterizerState.CullNone,
                        null,
                        Engine.ScreenMatrix);

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


                    PacePingManager.PaceTiming paceTiming = Mod.PacePingManager.GetPaceTiming(currentPath.ChapterSID, rInfo.DebugRoomName, dontLog: true);
                    if (paceTiming != null) {
                        formattedName = $"{formattedName}\n>Ping<";
                        scaleDivider += 2;
                    }

                    int x = template.X;
                    int y = template.Y;

                    Vector2 pos = DebugMapToScreen(new Vector2(x + template.Width / 2, y), camera);

                    ActiveFont.DrawOutline(
                        formattedName,
                        pos,
                        new Vector2(0.5f, 0),
                        Vector2.One * camera.Zoom / scaleDivider,
                        Color.White * 0.9f,
                        2f * camera.Zoom / 6,
                        Color.Black * 0.7f);
                }

                Draw.SpriteBatch.End();
            }

            if (settings.ShowSuccessRateBordersOnDebugMap) { //Insert Mod Option setting here
                Draw.SpriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.LinearClamp,
                        DepthStencilState.None,
                        RasterizerState.CullNone,
                        null,
                        Engine.ScreenMatrix);

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
                    if (successRate > settings.LiveDataChapterBarLightGreenPercent) {
                        color = new Color(0, 230, 0);
                    } else if (successRate > settings.LiveDataChapterBarGreenPercent) {
                        color = Color.Green;
                    } else if (successRate > settings.LiveDataChapterBarYellowPercent) {
                        color = new Color(194, 194, 41);
                    } else if (!float.IsNaN(successRate)) {
                        color = new Color(231, 45, 45);
                    }

                    int x = template.X;
                    int y = template.Y;

                    int width = template.Width;
                    int height = template.Height;

                    Vector2 pos = DebugMapToScreen(new Vector2(x, y), camera);

                    //Draw.Rect(pos, width * camera.Zoom, height * camera.Zoom, new Color(color.R, color.G, color.B, 0));
                    Vector2 topRight = pos + new Vector2(width * camera.Zoom, 0);
                    Vector2 bottomLeft = pos + new Vector2(0, height * camera.Zoom);
                    Vector2 bottomRight = pos + new Vector2(width * camera.Zoom, height * camera.Zoom);

                    float thickness = 1.5f * camera.Zoom;
                    Draw.Line(pos, topRight, color, thickness);
                    Draw.Line(pos, bottomLeft, color, thickness);
                    Draw.Line(topRight, bottomRight, color, thickness);
                    Draw.Line(bottomLeft, bottomRight, color, thickness);
                }

                Draw.SpriteBatch.End();
            }
        }

        public bool MapEditor_SelectionCheck(On.Celeste.Editor.MapEditor.orig_SelectionCheck orig, MapEditor self, Vector2 point) {
            List<LevelTemplate> levels = Util.GetPrivateProperty<List<LevelTemplate>>(self, "levels");
            LevelTemplate template = FindLevelTemplateByPoint(levels, point);

            if (template != null) {
                Mod.Log($"Clicked on {template.Name} ({template.X}, {template.Y}): Room contains {template.Checkpoints.Count} checkpoints and {template.Spawns.Count} respawns");
            } else {
                Mod.Log($"Clicked on empty space at ({point.X}, {point.Y})");
            }

            return orig(self, point);
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
    }
}
