using Celeste.Mod.ConsistencyTracker.Enums;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    public class StatTextComponent : Component {

        public StatTextPosition Position { get; set; } = StatTextPosition.TopRight;
        public string Text { get; set; } = "";
        public bool OptionVisible { get; set; }
        public bool HideInGolden { get; set; }
        public float Scale { get; set; } = 1f;
        public float Alpha { get; set; } = 1f;
        public PixelFont Font { get; set; }
        public float FontFaceSize { get; set; }
        public Color TextColor { get; set; } = Color.White;
        public float StrokeSize { get; set; } = 2f;
        public Color StrokeColor { get; set; } = Color.Black;

        public int OffsetX { get; set; } = 5;
        public int OffsetY { get; set; } = 5;

        public Vector2 Justify { get; set; } = new Vector2();

        public float PosX { get; set; } = 0;
        public float PosY { get; set; } = 0;

        public bool DebugShowPosition { get; set; }

        private static readonly int WIDTH = 1920;
        private static readonly int HEIGHT = 1080;

        public StatTextComponent(bool active, bool visible, StatTextPosition position) : base(active, visible) {
            Position = position;
        }

        public void SetPosition() {
            SetPosition(Position);
        }
        public void SetPosition(StatTextPosition pos) {
            Position = pos;

            switch (pos) {
                case StatTextPosition.TopLeft:
                    PosX = 0 + OffsetX;
                    PosY = 0 + OffsetY;
                    Justify = new Vector2(0, 0);
                    break;

                case StatTextPosition.TopCenter:
                    PosX = (WIDTH / 2) + OffsetX;
                    PosY = 0 + OffsetY;
                    Justify = new Vector2(0.5f, 0);
                    break;

                case StatTextPosition.TopRight:
                    PosX = WIDTH - OffsetX;
                    PosY = 0 + OffsetY;
                    Justify = new Vector2(1, 0);
                    break;
                    
                    
                case StatTextPosition.MiddleLeft:
                    PosX = 0 + OffsetX;
                    PosY = (HEIGHT / 2) + OffsetY;
                    Justify = new Vector2(0, 0.5f);
                    break;

                case StatTextPosition.MiddleCenter:
                    PosX = (WIDTH / 2) + OffsetX;
                    PosY = (HEIGHT / 2) + OffsetY;
                    Justify = new Vector2(0.5f, 0.5f);
                    break;

                case StatTextPosition.MiddleRight:
                    PosX = WIDTH - OffsetX;
                    PosY = (HEIGHT / 2) + OffsetY;
                    Justify = new Vector2(1, 0.5f);
                    break;

                    
                case StatTextPosition.BottomLeft:
                    PosX = 0 + OffsetX;
                    PosY = HEIGHT - OffsetY;
                    Justify = new Vector2(0, 1);
                    break;

                case StatTextPosition.BottomCenter:
                    PosX = (WIDTH / 2) + OffsetX;
                    PosY = HEIGHT - OffsetY;
                    Justify = new Vector2(0.5f, 1f);
                    break;

                case StatTextPosition.BottomRight:
                    PosX = WIDTH - OffsetX;
                    PosY = HEIGHT - OffsetY;
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
