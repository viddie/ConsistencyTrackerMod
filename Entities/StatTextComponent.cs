using Celeste.Mod.ConsistencyTracker.Enums;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    public class StatTextComponent : Component {

        public StatTextPosition Position { get; set; }
        public string Text { get; set; } = "Text Stats";
        public float Scale { get; set; }
        public float Alpha { get; set; }
        public PixelFont Font { get; set; }
        public float FontFaceSize { get; set; }
        public Color TextColor { get; set; }
        public float StrokeSize { get; set; }
        public Color StrokeColor { get; set; }

        public int Offset { get; set; }

        public Vector2 Justify { get; set; }

        public float PosX { get; set; } = 0;
        public float PosY { get; set; } = 0;

        public bool DebugShowPosition { get; set; }

        public StatTextComponent(bool active, bool visible, StatTextPosition position) : base(active, visible) {
            Position = position;
        }

        public void SetPosition() {
            SetPosition(Position);
        }
        public void SetPosition(StatTextPosition pos) {
            switch (pos) {
                case StatTextPosition.TopLeft:
                    PosX = 0 + Offset;
                    PosY = 0 + Offset;
                    Justify = new Vector2(0, 0);
                    break;

                case StatTextPosition.TopRight:
                    PosX = 1920 - Offset;
                    PosY = 0 + Offset;
                    Justify = new Vector2(1, 0);
                    break;

                case StatTextPosition.BottomLeft:
                    PosX = 0 + Offset;
                    PosY = 1080 - Offset;
                    Justify = new Vector2(0, 1);
                    break;

                case StatTextPosition.BottomRight:
                    PosX = 1920 - Offset;
                    PosY = 1080 - Offset;
                    Justify = new Vector2(1, 1);
                    break;
            }
        }

        public override void Render() {
            base.Render();

            Font.DrawOutline(
                FontFaceSize,
                Text,
                new Vector2(PosX, PosY),
                Justify,
                Vector2.One * Scale,
                TextColor,
                StrokeSize,
                StrokeColor
            );

            if (DebugShowPosition) {
                Draw.Circle(new Vector2(PosX, PosY), 10, Color.Red, 10);
                Draw.Point(new Vector2(PosX, PosY), Color.Red);
            }
        }
    }
}
