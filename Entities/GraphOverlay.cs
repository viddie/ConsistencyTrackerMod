using System;
using System.Collections.Generic;
using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    [Tracked]
    public class GraphOverlay : Entity {

        private static readonly int WIDTH = 1920;
        private static readonly int HEIGHT = 1080;

        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        private static bool Enabled => Mod.ModSettings.IngameOverlayGraphEnabled;
        private static StatTextPosition Anchor => Mod.ModSettings.IngameOverlayGraphPosition;
        private new static int Width => Mod.ModSettings.IngameOverlayGraphWidth;
        private new static int Height => Mod.ModSettings.IngameOverlayGraphHeight;
        private static int OffsetX => Mod.ModSettings.IngameOverlayGraphOffsetX;
        private static int OffsetY => Mod.ModSettings.IngameOverlayGraphOffsetY;
        private static int RoomsPadding => Mod.ModSettings.IngameOverlayGraphRoomsPadding;
        private static int BarSpacing => Mod.ModSettings.IngameOverlayGraphBarSpacing;
        private static bool ShowGoldenPbBar => Mod.ModSettings.IngameOverlayGraphShowGoldenPbBar;
        private static int HiddenRoomsIndicatorWidth => Mod.ModSettings.IngameOverlayGraphHiddenRoomsIndicatorWidth;
        private static int BackgroundDim => Mod.ModSettings.IngameOverlayGraphBackgroundDim;

        private Dictionary<RoomInfo, Tuple<int, float, int, float>> ChokeRateData { get; set; }
        private int HighestDifficulty { get; set; }
        private RoomInfo PbRoom { get; set; }
        private RoomInfo PbRoomSession { get; set; }
        private RoomInfo RoomToDisplay { get; set; }

        public static Color testColor = Color.Yellow;

        public GraphOverlay() {
            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
            Events.Events.OnAfterSavingStats += EventsOnAfterSavingStats;
        }

        private void EventsOnAfterSavingStats() {
            UpdateOverlay();
        }

        /**
         * Update the overlay with new choke rate data. Only call when stats changed.
         */
        public void UpdateOverlay() {
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;
            if (path == null || stats == null) return;
            
            ChokeRateData = ChokeRateStat.GetRoomData(path, stats);
            PbRoom = StatsUtil.GetFurthestGoldenRun(path, stats);
            PbRoomSession = StatsUtil.GetFurthestGoldenRunSession(path, stats);
            
            HighestDifficulty = 0;
            foreach (RoomInfo rInfo in path.WalkPath()) {
                int roomDifficulty = rInfo.DifficultyWeight;
                if (roomDifficulty == -1) {
                    roomDifficulty = ConsoleCommands.GetRoomDifficultyBasedOnStats(ChokeRateData, rInfo);
                }
                HighestDifficulty = Math.Max(HighestDifficulty, roomDifficulty);
            }
            HighestDifficulty = Math.Max(1, HighestDifficulty);

            if (path.CurrentRoom != null) {
                RoomToDisplay = path.CurrentRoom;
            }
        }
        
        public override void Render() {
            base.Render();
            
            PathInfo path = Mod.CurrentChapterPath;
            ChapterStats stats = Mod.CurrentChapterStats;
            
            if (!Enabled || !(Engine.Scene is Level) || path == null || stats == null) {
                return;
            }

            //Draw the overlay
            Vector2 position = ResolvePosition(Anchor, Width, Height, OffsetX, OffsetY);
            Draw.Rect(position.X - 1, position.Y - 1, Width + 2, Height + 2, new Color(0, 0, 0, BackgroundDim / 10f));
            
            //The graph should display 1 bar per room on the path. The bar widths should be scaled to fit the available area.
            //HOWEVER, each bar needs to be exactly a pixel integer. If there is extra space, it should be padded left and right of
            //the graph area.
            //There should be exactly 1 pixel of space between each bar
            
            int gameplayRoomCount = path.GameplayRoomCount;
            RoomInfo currentRoom = RoomToDisplay;
            int currentRoomNumber = currentRoom?.RoomNumberInChapter ?? 1;
            int barCount = Math.Min(gameplayRoomCount, 1 + 2 * RoomsPadding);
            int displayMinRoomNumber;
            if (currentRoomNumber <= RoomsPadding || barCount == gameplayRoomCount) {
                displayMinRoomNumber = 1;
            } else if (gameplayRoomCount - currentRoomNumber <= RoomsPadding) {
                displayMinRoomNumber = gameplayRoomCount - RoomsPadding * 2;
            } else {
                displayMinRoomNumber = currentRoomNumber - RoomsPadding;
            }
            int displayMaxRoomNumber = displayMinRoomNumber + barCount - 1;

            int hidingRoomsBefore = Math.Max(0, displayMinRoomNumber - 1);
            float hidingBeforePercent = hidingRoomsBefore / (float)gameplayRoomCount;
            int hidingRoomsAfter = Math.Max(0, gameplayRoomCount - displayMaxRoomNumber);
            float hidingAfterPercent = hidingRoomsAfter / (float)gameplayRoomCount;
            
            int beforeBarsOffset = hidingRoomsBefore > 0 ? BarSpacing + HiddenRoomsIndicatorWidth : 0;
            int afterBarsOffset = hidingRoomsAfter > 0 ? BarSpacing + HiddenRoomsIndicatorWidth : 0;

            int availableBarWidth = Width - beforeBarsOffset - afterBarsOffset;
            bool currentRoomIndicatorExplicit = Mod.ModSettings.IngameOverlayGraphCurrentRoomExplicit;
            int availableBarHeight = Height - (ShowGoldenPbBar ? 3 + 1 : 0) - (currentRoomIndicatorExplicit ? 3 + 1 : 0);
            int barWidth = (availableBarWidth - ((barCount - 1) * BarSpacing)) / barCount;
            int paddingX = (availableBarWidth - (barWidth * barCount) - ((barCount - 1) * BarSpacing)) / 2;
            
            //Walk path and draw the bars
            int barsDrawn = 0;
            bool visitedCurrent = false;
            bool gotGolden = stats.GoldenCollectedCount > 0;
            bool isFirstCheckpoint = true;
            
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                bool breakout = false;
                foreach (RoomInfo rInfo in cpInfo.GameplayRooms) {
                    //Dont display this room if its not within the room range to display
                    //Only display rooms that are within RoomsPadding of the current room number
                    if (rInfo.RoomNumberInChapter < displayMinRoomNumber) {
                        continue;
                    }
                    if (rInfo.RoomNumberInChapter > displayMaxRoomNumber) {
                        breakout = true;
                        break;
                    }

                    int roomDifficulty = rInfo.DifficultyWeight;
                    if (roomDifficulty == -1 && ChokeRateData != null) {
                        roomDifficulty = ConsoleCommands.GetRoomDifficultyBasedOnStats(ChokeRateData, rInfo);
                    }
                    int barHeight = (int)(availableBarHeight * ((double)roomDifficulty / HighestDifficulty));
                    barHeight = Math.Max(1, barHeight);
                    Color barColor = !currentRoomIndicatorExplicit && rInfo.RoomNumberInChapter == currentRoomNumber ? Color.Red : Color.White;
                    visitedCurrent = rInfo.RoomNumberInChapter == currentRoomNumber || visitedCurrent;
                    if (!visitedCurrent) {
                        barColor = Color.Gray;
                    }
                    
                    //Draw checkpoint indicator over the empty space before this bar
                    if (!isFirstCheckpoint && rInfo.RoomNumberInCP == 1 && Mod.ModSettings.IngameOverlayGraphShowCheckpointIndicator) {
                        RoomInfo previousRoom = rInfo.PreviousRoomInChapter;
                        bool isSameMap = previousRoom == null || rInfo.UID == previousRoom.UID;
                        Color color = isSameMap ? Color.Gold : Color.Cyan;
                        //Position of the indicator: It should occupy the gap between the last drawn bar and this one. Center it in the gap.
                        //Width: If there is no space, omit the indicator. If there is any space, occupy: 1 pixel, or 2 pixels if the available space is an even number of pixels
                        //Height: full height of the graph. Including drawing over the golden PB bar if it exists.
                        int lastBarX = (int)(paddingX + position.X + (barWidth * (barsDrawn)) + (BarSpacing * (barsDrawn - 1)) + beforeBarsOffset);
                        if (BarSpacing > 0) {
                            int indicatorWidth = BarSpacing % 2 == 0 ? 2 : 1;
                            int indicatorX = lastBarX + (BarSpacing - indicatorWidth) / 2;
                            Draw.Rect(indicatorX, position.Y, indicatorWidth, availableBarHeight + (ShowGoldenPbBar ? 3 + 1 : 0), color);
                        }
                    }
                    
                    //Draw bar
                    Draw.Rect(paddingX + position.X + (barWidth * barsDrawn) + (BarSpacing * barsDrawn) + beforeBarsOffset, 
                              position.Y + (availableBarHeight - barHeight),
                              barWidth,
                              barHeight,
                              barColor);

                    //Golden PB indicator
                    if (ShowGoldenPbBar) {
                        if (gotGolden || (PbRoom != null && PbRoom.RoomNumberInChapter >= rInfo.RoomNumberInChapter)) {
                            bool showBarPadding = PbRoom != null && ((PbRoom.RoomNumberInChapter == rInfo.RoomNumberInChapter && !gotGolden) || rInfo.RoomNumberInChapter == displayMaxRoomNumber);
                            bool halfBar = showBarPadding && !gotGolden && PbRoom.RoomNumberInChapter == rInfo.RoomNumberInChapter && (rInfo.RoomNumberInChapter != displayMaxRoomNumber || rInfo.RoomNumberInChapter == gameplayRoomCount);
                            Color goldenBarColor = Color.Gold;
                            if (PbRoomSession != null &&
                                PbRoomSession.RoomNumberInChapter >= rInfo.RoomNumberInChapter) {
                                goldenBarColor = Color.Orange;
                            }
                            Draw.Rect(paddingX + position.X + (barWidth * barsDrawn) + (BarSpacing * barsDrawn) + beforeBarsOffset, 
                                      position.Y + availableBarHeight + 1, 
                                      barWidth * (halfBar ? 0.5f : 1) + (BarSpacing * (showBarPadding ? 0 : 1)), 
                                      3,
                                      goldenBarColor);
                        }
                    }
                    
                    //Current room indicator
                    if (currentRoom != null && rInfo.RoomNumberInChapter == currentRoom.RoomNumberInChapter && currentRoomIndicatorExplicit) {
                        int heightOffset = ShowGoldenPbBar ? 3 + 1 : 0;
                        Draw.Rect(paddingX + position.X + (barWidth * barsDrawn) + (BarSpacing * barsDrawn) + beforeBarsOffset, 
                                  position.Y + availableBarHeight + heightOffset + 1,
                                  barWidth, 
                                  3,
                                  Color.Red);
                    }
                    
                    barsDrawn++;
                }

                isFirstCheckpoint = false;

                if (breakout) break;
            }
            
            //Draw a 1 pixel bar on the left and right to indicate how many rooms are hidden
            if (hidingBeforePercent > 0) {
                int barHeight = (int)(availableBarHeight * hidingBeforePercent);
                Draw.Rect(paddingX + position.X, 
                          position.Y + availableBarHeight - barHeight,
                          HiddenRoomsIndicatorWidth, 
                          availableBarHeight * hidingBeforePercent,
                          Color.Teal);
            }
            if (hidingAfterPercent > 0) {
                int barHeight = (int)(availableBarHeight * hidingAfterPercent);
                Draw.Rect(paddingX + position.X + (barWidth * barsDrawn) + (BarSpacing * barsDrawn) + beforeBarsOffset, 
                          position.Y + availableBarHeight - barHeight, 
                          HiddenRoomsIndicatorWidth,
                          barHeight,
                          Color.Teal);
            }
        }
        
        
        private static Vector2 ResolvePosition(StatTextPosition pos, int width, int height, int offsetX, int offsetY) {
            Vector2 position = new Vector2();
            
            switch (pos) {
                case StatTextPosition.TopLeft:
                    position.X = 0 + offsetX;
                    position.Y = 0 + offsetY;
                    break;

                case StatTextPosition.TopCenter:
                    position.X = (WIDTH / 2) - (width / 2) + offsetX;
                    position.Y = 0 + offsetY;
                    break;

                case StatTextPosition.TopRight:
                    position.X = WIDTH - width - offsetX;
                    position.Y = 0 + offsetY;
                    break;
                    
                case StatTextPosition.MiddleLeft:
                    position.X = 0 + offsetX;
                    position.Y = (HEIGHT / 2) - (height / 2) + offsetY;
                    break;

                case StatTextPosition.MiddleCenter:
                    position.X = (WIDTH / 2) - (width / 2) + offsetX;
                    position.Y = (HEIGHT / 2) - (height / 2) + offsetY;
                    break;

                case StatTextPosition.MiddleRight:
                    position.X = WIDTH - width - offsetX;
                    position.Y = (HEIGHT / 2) - (height / 2) + offsetY;
                    break;

                    
                case StatTextPosition.BottomLeft:
                    position.X = 0 + offsetX;
                    position.Y = HEIGHT - height - offsetY;
                    break;

                case StatTextPosition.BottomCenter:
                    position.X = (WIDTH / 2) - (width / 2) + offsetX;
                    position.Y = HEIGHT - height - offsetY;
                    break;

                case StatTextPosition.BottomRight:
                    position.X = WIDTH - width - offsetX;
                    position.Y = HEIGHT - height - offsetY;
                    break;
            }

            return position;
        }
    }
}