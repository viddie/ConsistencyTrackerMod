using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    public class RoomOverlay : Entity {
        private const float roomAspectRatio = 1.61803f; // golden ratio
        private const float offsetPixels = 10f; // this should be configurable
        private const float totalLengthMultiplier = 0.9f;
        private const float minCheckpointLengthMultiplier = 0.25f;
        private const float maxCheckpointLengthMultiplier = 0.4f;
        private const float checkpointScale = 1.5f;
        private const float currentRoomScale = 1.3f;
        private const int roomPaddingPixels = 4;
        private const int checkpointSeparatorShortPixels = 4;

        private string previousDebugRoomName;
        private OverlayPosition previousOverlayPosition;
        private List<RoomRectangle> roomRectangles;
        private int[] checkpointMarkers;
        private int checkpointSeparatorLongPixels;

        public RoomOverlay() {
            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;

            Visible = false;
        }

        private bool TryGetInfo(string debugRoomName, out CheckpointInfo currentCheckpoint, out int otherRoomCount) {
            currentCheckpoint = null;
            otherRoomCount = 0;

            foreach (var checkpoint in ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints) {
                var roomIndex = checkpoint.Rooms.FindIndex(r => r.DebugRoomName == debugRoomName);
                if (roomIndex >= 0) {
                    currentCheckpoint = checkpoint;
                } else {
                    otherRoomCount += checkpoint.RoomCount;
                }
            }

            return currentCheckpoint != null;
        }

        public override void Update() {
            // build rooms if we must
            var createRooms = roomRectangles == null;
            if (ConsistencyTrackerModule.Instance.CurrentChapterStats != null &&
                ConsistencyTrackerModule.Instance.CurrentChapterPath != null &&
                createRooms) {
                roomRectangles = new List<RoomRectangle>();
                foreach (var checkpoint in ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints) {
                    foreach (var room in checkpoint.Rooms) {
                        var stats = ConsistencyTrackerModule.Instance.CurrentChapterStats.GetRoom(room.DebugRoomName);
                        var rect = new RoomRectangle(checkpoint, room, stats);
                        roomRectangles.Add(rect);
                        Add(rect);
                    }
                }

                checkpointMarkers = Enumerable.Range(0, ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints.Count).ToArray();
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

        public override void Render() {
            base.Render();

            var overlayAlpha = ConsistencyTrackerModule.Instance.ModSettings.OverlayOpacity * 0.1f;
            var overlayPosition = ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition;
            var horizontal = overlayPosition.IsHorizontal();
            var size = horizontal ? new Vector2(checkpointSeparatorShortPixels, checkpointSeparatorLongPixels) : new Vector2(checkpointSeparatorLongPixels, checkpointSeparatorShortPixels);

            foreach (var offset in checkpointMarkers) {
                var position = Position + overlayPosition switch {
                    OverlayPosition.Top => new Vector2(offset, 0),
                    OverlayPosition.Bottom => new Vector2(offset, -size.Y),
                    OverlayPosition.Left => new Vector2(0, offset),
                    OverlayPosition.Right => new Vector2(-size.X, offset),
                    _ => Vector2.Zero,
                };

                Draw.Rect(position.X, position.Y, size.X, size.Y, Color.White * overlayAlpha);
            }
        }

        private void UpdateRoomLengths(bool force = false) {
            if (roomRectangles == null) return;

            var currentRoomStats = ConsistencyTrackerModule.Instance.CurrentChapterStats.CurrentRoom;
            if (currentRoomStats == null) return;

            // if the overlay position has changed, force
            var overlayPosition = ConsistencyTrackerModule.Instance.ModSettings.OverlayPosition;
            if (previousOverlayPosition != overlayPosition) force = true;

            // break if the room hasn't changed and we're not forcing
            if (!force && previousDebugRoomName == currentRoomStats.DebugRoomName) return;

            // try to get info about the path
            var totalRooms = ConsistencyTrackerModule.Instance.CurrentChapterPath.Checkpoints.Sum(c => c.RoomCount);
            if (!TryGetInfo(currentRoomStats.DebugRoomName, out var currentCheckpoint, out var otherRoomCount)) return;

            previousDebugRoomName = currentRoomStats.DebugRoomName;
            previousOverlayPosition = overlayPosition;

            // calculate expected lengths
            var horizontal = overlayPosition.IsHorizontal();
            var expectedTotalLength = (float) Math.Floor((horizontal ? Engine.Width : Engine.Height) * totalLengthMultiplier);
            var expectedRoomSize = expectedTotalLength / totalRooms;
            var expectedCheckpointLength = Calc.Clamp(expectedRoomSize * currentCheckpoint.RoomCount * checkpointScale, expectedTotalLength * minCheckpointLengthMultiplier, expectedTotalLength * maxCheckpointLengthMultiplier);
            var expectedCheckpointRoomLength = expectedCheckpointLength / currentCheckpoint.RoomCount;
            expectedCheckpointLength += expectedCheckpointRoomLength * currentRoomScale - expectedCheckpointRoomLength;
            var expectedRemainingLength = expectedTotalLength - expectedCheckpointLength;

            // calculate actual lengths
            var normalRoomStride = (int) (expectedRemainingLength / otherRoomCount);
            var normalRoomLength = normalRoomStride - roomPaddingPixels;
            var checkpointRoomStride = (int) expectedCheckpointRoomLength;
            var checkpointRoomLength = checkpointRoomStride - roomPaddingPixels;
            var currentRoomStride = (int) (expectedCheckpointRoomLength * currentRoomScale);
            var currentRoomLength = currentRoomStride - roomPaddingPixels;

            checkpointSeparatorLongPixels = (int) (checkpointRoomLength / roomAspectRatio);

            // update rooms
            foreach (var roomRectangle in roomRectangles) {
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
            if (roomRectangles == null) return;

            int offset = 0;
            int checkpointIndex = 0;
            CheckpointInfo checkpoint = null;

            foreach (var roomRectangle in roomRectangles) {
                if (checkpoint != null && checkpoint != roomRectangle.CheckpointInfo) {
                    checkpointMarkers[checkpointIndex++] = offset + roomPaddingPixels;
                    offset += checkpointSeparatorShortPixels + roomPaddingPixels * 3;
                }

                roomRectangle.Offset = offset;
                offset += (int)roomRectangle.Length + roomPaddingPixels;

                checkpoint = roomRectangle.CheckpointInfo;
            }

            var half = (offset - roomPaddingPixels) / 2;
            foreach (var roomRectangle in roomRectangles) {
                roomRectangle.Offset -= half;
            }

            for (int i = 0; i < checkpointMarkers.Length; i++) {
                checkpointMarkers[i] -= half;
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

            private const float tweenTime = 0.2f;
            private float tweenTimeRemaining;
            private float tweenFrom;
            private float tweenTo;

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

                var color = Color.White;
                if (RoomStats?.PreviousAttempts.Any() == true) {
                    var lastFive = RoomStats.LastFiveRate;
                    color = lastFive <= 0.33f ? Color.Red : lastFive <= 0.66f ? Color.Yellow : Color.Green;
                }

                var overlayAlpha = ConsistencyTrackerModule.Instance.ModSettings.OverlayOpacity * 0.1f;

                var size = new Vector2(horizontal ? rounded : perp, horizontal ? perp : rounded);
                Draw.Rect(position, size.X, size.Y, color * overlayAlpha);

                if (RoomStats.DebugRoomName == ConsistencyTrackerModule.Instance.CurrentChapterStats.CurrentRoom.DebugRoomName) {
                    Draw.HollowRect(position, size.X, size.Y, Color.White * overlayAlpha);
                }
            }
        }
    }
}
