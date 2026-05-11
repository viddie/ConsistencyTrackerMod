using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Utility;
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
        public bool OptionVisible { get; set; }
        public bool HideInGolden { get; set; }

        private float Scale { get; set; } = 1f;

        public bool TextOutline { get; set; } = true;

        public PixelFont Font { get; set; }
        public float FontFaceSize { get; set; }

        private Color TextColor { get; set; } = Color.White;

        public float StrokeSize { get; set; } = 2f;
        public Color StrokeColor { get; set; } = Color.Black;

        public int OffsetX { get; set; } = 5;
        public int OffsetY { get; set; } = 5;

        public Vector2 Justify { get; set; } = new Vector2();

        public float PosX { get; set; } = 0;
        public float PosY { get; set; } = 0;

        private float YOffset { get; set; } = 0;
        private float LineHeight { get; set; } = 0;

        // Tuple items:
        // 1. Line text
        // 2. Line color
        private List<Tuple<string, Color?>> TextLines { get; set; }

        public bool DebugShowPosition { get; set; }

        private static readonly int WIDTH = 1920;
        private static readonly int HEIGHT = 1080;

        public StatTextComponent(bool active, bool visible, StatTextPosition position) : base(active, visible) {
            Position = position;
        }

        public void SetText(string text) {
            text = text.Replace("\\n", "\n");
            TextLines = text.Split('\n').Select(ParseLineColor).ToList();
            UpdateText();
        }

        private void UpdateText() {
            if (TextLines == null) {
                return;
            }
            
            LineHeight = Font.Get(FontFaceSize).LineHeight * Scale;
            YOffset = TextLines.Count * Justify.Y * LineHeight;    
        }

        private Tuple<string, Color?> ParseLineColor(string line) {
            Color? color = null;

            if (!string.IsNullOrEmpty(line) && line.Length > 1 && line[0] == '[') {
                int colorLength = line[1] == '#' ? 7 : 6;
                if (line.Length >= colorLength + 2 && line[colorLength + 1] == ']' && Util.TryParseColor(line.Substring(1, colorLength), out Color parsedColor)) {
                    color = parsedColor;
                    line = line.Substring(colorLength + 2);
                } 
            }

            return new Tuple<string, Color?>(line, color);
        }

        public void SetTextColor(Color color) {
            TextColor = color;
        }

        public void SetSize(float size) {
            Scale = size;
            UpdateText();
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

            UpdateText();
        }

        public override void Render() {
            Vector2 pointer = new Vector2(PosX, PosY);
            pointer.Y -= YOffset;

            for (int i = 0; i < TextLines.Count; i++) {
                Tuple<string, Color?> line = TextLines[i];
                string text = line.Item1;
                Color color = line.Item2.HasValue ? line.Item2.Value : TextColor;

                if (TextOutline) {
                    Font.DrawOutline(
                        FontFaceSize,
                        text,
                        pointer,
                        new Vector2(Justify.X, 0),
                        Vector2.One * Scale,
                        color,
                        StrokeSize,
                        StrokeColor
                    );
                } else {
                    Font.Draw(
                        FontFaceSize,
                        text,
                        pointer,
                        new Vector2(Justify.X, 0),
                        Vector2.One * Scale,
                        color
                    );
                }

                pointer.Y += LineHeight;
            }

            if (DebugShowPosition) {
                Draw.Circle(new Vector2(PosX, PosY), 10, Color.Red, 10);
                Draw.Point(new Vector2(PosX, PosY), Color.Red);
            }
        }
    }
}
