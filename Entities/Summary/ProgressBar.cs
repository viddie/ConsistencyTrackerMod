using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class ProgressBar : Entity {

        public float BarWidth { get; set; }
        public float BarHeight { get; set; }

        public int Value { get; set; }
        public int MaxValue { get; set; }

        public float Progress => (float)Value / MaxValue;

        public Color Color { get; set; } = Color.Yellow;
        public Color LabelColor { get; set; } = Color.White;
        public Color BackgroundColor { get; set; } = Color.White;

        public string LeftLabel { get; set; } = "";
        public string RightLabel { get; set; } = "";
        public string ValueLabel { get; set; } = "";

        public float FontMult { get; set; } = 0.5f;

        public ProgressBar(int value, int maxValue) {
            Value = value;
            MaxValue = maxValue;
        }

        public override void Render() {
            base.Render();

            Vector2 position = Position;

            Draw.Rect(position, BarWidth, BarHeight, BackgroundColor);
            Draw.Rect(position, BarWidth * Progress, BarHeight, Color);
            
            Vector2 leftLabelPosition = position + new Vector2(-5, BarHeight / 2);
            Vector2 rightLabelPosition = position + new Vector2(BarWidth + 5, BarHeight / 2);
            Vector2 valueLabelPosition = position + new Vector2(BarWidth * Progress, -5);

            ActiveFont.Draw(LeftLabel, leftLabelPosition, new Vector2(1f, 0.5f), Vector2.One * FontMult, LabelColor);
            ActiveFont.Draw(RightLabel, rightLabelPosition, new Vector2(0f, 0.5f), Vector2.One * FontMult, LabelColor);
            ActiveFont.Draw(ValueLabel, valueLabelPosition, new Vector2(0.5f, 1f), Vector2.One * FontMult, LabelColor);
        }
    }
}
