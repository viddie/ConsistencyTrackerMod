using System.Linq;
using Celeste.Mod.ConsistencyTracker.Models;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    public class RoomOverlay : Entity {
        private Level Level;
        private PathInfo Path;

        public RoomOverlay(Level level) {
            Level = level;

            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;
        }

        public override void Update() {
            base.Update();

            Position = new Vector2(Engine.Width / 2f, Engine.Height - 10f);

            if (ConsistencyTrackerModule.Instance.CurrentChapterStats == null) return;

            if (Path == null) {
                Path = ConsistencyTrackerModule.Instance.GetPathInputInfo();
            }
        }

        public override void Render() {
            base.Render();

            if (ConsistencyTrackerModule.Instance.CurrentChapterStats == null || Path == null) return;

            const float roomPadding = 10f;
            const float checkPointPadding = 10f;
            const float checkPointWidth = 5f;
            const float checkPointHeight = 50f;

            var stats = ConsistencyTrackerModule.Instance.CurrentChapterStats;

            var smallSize = new Vector2(50, 30f);
            var largeSize = smallSize * 1.5f;

            var totalRoomWidth = Path.Checkpoints.Sum(c => c.RoomCount * smallSize.X + (c.RoomCount - 1) * roomPadding);
            var totalCheckpointWidth = (Path.Checkpoints.Count - 1) * (checkPointWidth + 2 * checkPointPadding);
            var totalWidth = totalRoomWidth + totalCheckpointWidth - smallSize.X + largeSize.X;

            var x = Position.X - totalWidth / 2f;
            var y = Position.Y - 30f;

            const float alpha = 0.6f;

            for (int i = 0; i < Path.Checkpoints.Count; i++) {
                var checkpoint = Path.Checkpoints[i];

                for (int j = 0; j < checkpoint.RoomCount; j++) {
                    var room = checkpoint.Rooms[j];
                    var roomStats = stats.GetRoom(room.DebugRoomName);
                    var current = stats.CurrentRoom.DebugRoomName == room.DebugRoomName;
                    var position = new Vector2(x, y - (current ? largeSize.Y : smallSize.Y) / 2f);
                    var size = current ? largeSize : smallSize;

                    var lastFive = roomStats.LastFiveRate;
                    var color = lastFive <= 0.33f ? Color.Red : lastFive <= 0.66f ? Color.Yellow : Color.Green;

                    Draw.Rect(position, size.X, size.Y, color * alpha);

                    x += current ? largeSize.X : smallSize.X;

                    if (j < checkpoint.RoomCount - 1) {
                        x += roomPadding;
                    }
                }

                if (i < Path.Checkpoints.Count - 1) {
                    x += checkPointPadding;
                    Draw.Rect(new Vector2(x, y - checkPointHeight / 2f), checkPointWidth, checkPointHeight, Color.White * alpha);
                    x += checkPointPadding;
                }
            }
        }
    }
}
