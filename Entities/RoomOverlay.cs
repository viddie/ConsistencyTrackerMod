using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    public class RoomOverlay : Entity {
        private Level Level;
        private PathInfo Path;
        private List<Rectangle> CheckpointSeparatorRectangles = new();
        private string PreviousDebugRoomName;
        private List<RoomRectangle> RoomRectangles;

        private const float offsetPixels = 10f; // this should be configurable
        private const float totalLengthMultiplier = 0.9f;
        private const float minCheckpointLengthMultiplier = 0.3f;
        private const float maxCheckpointLengthMultiplier = 0.6f;
        private const float checkpointScale = 2f;
        private const float currentRoomScale = 1.5f;
        private const int roomPaddingPixels = 4;
        private const int checkpointSeparatorPixels = 4;

        public RoomOverlay(Level level) {
            Level = level;

            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;

            Visible = false;
        }

        private bool TryGetInfo(string debugRoomName, out CheckpointInfo currentCheckpoint, out int currentRoom, out int roomsBefore, out int roomsAfter) {
            currentCheckpoint = null;
            currentRoom = 0;
            roomsBefore = 0;
            roomsAfter = 0;

            foreach (var checkpoint in ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints) {
                var roomIndex = checkpoint.Rooms.FindIndex(r => r.DebugRoomName == debugRoomName);
                if (roomIndex >= 0) {
                    currentCheckpoint = checkpoint;
                    currentRoom = roomIndex;
                } else if (currentCheckpoint == null) {
                    roomsBefore += checkpoint.RoomCount;
                } else {
                    roomsAfter += checkpoint.RoomCount;
                }
            }

            return currentCheckpoint != null;
        }

        public override void Update() {
            // build rooms if we must
            var createRooms = RoomRectangles == null;
            if (ConsistencyTrackerModule.Instance.CurrentChapterStats != null &&
                ConsistencyTrackerModule.Instance.CurrentChapterPath != null &&
                createRooms) {
                RoomRectangles = new List<RoomRectangle>();
                foreach (var checkpoint in ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints) {
                    foreach (var room in checkpoint.Rooms) {
                        var stats = ConsistencyTrackerModule.Instance.CurrentChapterStats.GetRoom(room.DebugRoomName);
                        var rect = new RoomRectangle(checkpoint, room, stats);
                        RoomRectangles.Add(rect);
                        Add(rect);
                    }
                }
            }

            // hide if we're showing and shouldn't be
            if (Visible && ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition == OverlayPosition.Disabled) {
                Visible = false;
            }
            // show if we're not showing and should be
            else if (!Visible && ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition != OverlayPosition.Disabled) {
                Visible = true;
            }

            // update the room lengths if we must
            UpdateRoomLengths(createRooms);

            // make rooms tween themselves
            base.Update();

            // update the room positions
            UpdateRoomPositions();

            // set the position based on the current settings
            var overlayPosition = ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition;
            Position = overlayPosition switch {
                OverlayPosition.Bottom => new Vector2(Engine.Width / 2f, Engine.Height - offsetPixels),
                OverlayPosition.Top => new Vector2(Engine.Width / 2f, offsetPixels),
                OverlayPosition.Left => new Vector2(offsetPixels, Engine.Height / 2f),
                OverlayPosition.Right => new Vector2(Engine.Width - offsetPixels, Engine.Height / 2f),
                _ => Vector2.Zero,
            };
        }

        private void UpdateRoomLengths(bool force = false) {
            if (RoomRectangles == null) return;

            // break if the room hasn't changed
            var currentRoomStats = ConsistencyTrackerModule.Instance.CurrentChapterStats.CurrentRoom;
            if (currentRoomStats == null || PreviousDebugRoomName == currentRoomStats.DebugRoomName) return;

            // try to get info about the path
            var totalRooms = ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints.Sum(c => c.RoomCount);
            if (!TryGetInfo(currentRoomStats.DebugRoomName, out var currentCheckpoint, out var currentRoom, out var roomsBefore, out var roomsAfter)) return;

            PreviousDebugRoomName = currentRoomStats.DebugRoomName;

            // calculate expected lengths
            var overlayPosition = ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition;
            var horizontal = overlayPosition.IsHorizontal();
            var expectedTotalLength = (float) Math.Floor((horizontal ? Engine.Width : Engine.Height) * totalLengthMultiplier);
            var expectedRoomSize = expectedTotalLength / totalRooms;
            var expectedCheckpointLength = Calc.Clamp(expectedRoomSize * currentCheckpoint.RoomCount * checkpointScale, expectedTotalLength * minCheckpointLengthMultiplier, expectedTotalLength * maxCheckpointLengthMultiplier);
            var expectedCheckpointRoomLength = expectedCheckpointLength / currentCheckpoint.RoomCount;
            expectedCheckpointLength += expectedCheckpointRoomLength * currentRoomScale - expectedCheckpointRoomLength;
            var expectedRemainingLength = expectedTotalLength - expectedCheckpointLength;

            // calculate actual lengths
            var normalRoomStride = (int) (expectedRemainingLength / (roomsBefore + roomsAfter));
            var normalRoomLength = normalRoomStride - roomPaddingPixels;
            var checkpointRoomStride = (int) expectedCheckpointRoomLength;
            var checkpointRoomLength = checkpointRoomStride - roomPaddingPixels;
            var currentRoomStride = (int) (expectedCheckpointRoomLength * currentRoomScale);
            var currentRoomLength = currentRoomStride - roomPaddingPixels;

            // update rooms
            foreach (var roomRectangle in RoomRectangles) {
                if (roomRectangle.RoomInfo.DebugRoomName == currentRoomStats.DebugRoomName) {
                    roomRectangle.TweenToLength(currentRoomLength, force);
                } else if (roomRectangle.CheckpointInfo == currentCheckpoint) {
                    roomRectangle.TweenToLength(checkpointRoomLength, force);
                } else {
                    roomRectangle.TweenToLength(normalRoomLength, force);
                }
            }
        }

        private void UpdateRoomPositions() {
            if (RoomRectangles == null) return;

            int offset = 0;
            foreach (var roomRectangle in RoomRectangles) {
                roomRectangle.Offset = offset;
                offset += (int)roomRectangle.Length + roomPaddingPixels;
            }

            var half = (offset - roomPaddingPixels) / 2;
            foreach (var roomRectangle in RoomRectangles) {
                roomRectangle.Offset -= half;
            }
        }

        private class RoomRectangle : Component {
            public readonly CheckpointInfo CheckpointInfo;
            public readonly RoomInfo RoomInfo;
            public readonly RoomStats RoomStats;

            public int Offset;
            public float Length;

            public void TweenToLength(int targetLength, bool force = false) {
                if (force) {
                    Length = targetLength;
                    tweenTimeRemaining = 0;
                } else {
                    tweenFrom = Length;
                    tweenTo = targetLength;
                    tweenTimeRemaining = tweenTime;
                }
            }

            private const float tweenTime = 1f;
            private float tweenTimeRemaining;
            private float tweenFrom;
            private float tweenTo;

            private const float roomAspectRatio = 1.61803f;

            public RoomRectangle(CheckpointInfo checkpointInfo, RoomInfo roomInfo, RoomStats roomStats) : base(true, true) {
                CheckpointInfo = checkpointInfo;
                RoomInfo = roomInfo;
                RoomStats = roomStats;
            }

            public override void Update() {
                base.Update();

                if (tweenTimeRemaining > 0) {
                    tweenTimeRemaining -= Engine.RawDeltaTime;
                    Length = Calc.LerpClamp(tweenFrom, tweenTo, 1 - tweenTimeRemaining / tweenTime);
                }
            }

            public override void Render() {
                base.Render();

                var overlayPosition = ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition;
                var horizontal = overlayPosition.IsHorizontal();
                var rounded = (int) Length;
                var perp = (int) (rounded / roomAspectRatio);

                var position = Entity.Position + overlayPosition switch {
                    OverlayPosition.Top => new Vector2(Offset, 0),
                    OverlayPosition.Bottom => new Vector2(Offset, -perp),
                    OverlayPosition.Left => new Vector2(0, Offset),
                    OverlayPosition.Right => new Vector2(-perp, Offset),
                    _ => Vector2.Zero,
                };

                var lastFive = RoomStats?.LastFiveRate ?? 0;
                var consistencyColor = lastFive <= 0.33f ? Color.Red : lastFive <= 0.66f ? Color.Yellow : Color.Green;
                var color = RoomStats == null ? Color.White : consistencyColor;
                const float alpha = 0.6f;

                var size = new Vector2(horizontal ? rounded : perp, horizontal ? perp : rounded);
                Draw.Rect(position, size.X, size.Y, color * alpha);

                if (RoomStats.DebugRoomName == ConsistencyTrackerModule.Instance.CurrentChapterStats.CurrentRoom.DebugRoomName) {
                    Draw.HollowRect(position, size.X, size.Y, Color.White * alpha);
                }
            }
        }
    }
}
