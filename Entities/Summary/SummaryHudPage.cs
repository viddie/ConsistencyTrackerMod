using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public abstract class SummaryHudPage : Entity {
        
        protected static float FontMultLarge => SummaryHud.Settings.FontMultLarge;
        protected static float FontMultMedium => SummaryHud.Settings.FontMultMedium;
        protected static float FontMultMediumSmall => SummaryHud.Settings.FontMultMediumSmall;
        protected static float FontMultSmall => SummaryHud.Settings.FontMultSmall;
        protected static float FontMultVerySmall => SummaryHud.Settings.FontMultVerySmall;
        protected static float FontMultAnt => SummaryHud.Settings.FontMultAnt;

        protected static float BasicMargin => 10;


        public readonly string Name;
        protected bool MissingPath { get; set; }

        public SummaryHudPage(string name) {
            Name = name;

            Position = new Vector2(0, 0);
        }



        protected static Vector2 DrawText(string text, Vector2 pointer, float fontSize, Color color, Vector2 justify = default) {
            Vector2 measure = ActiveFont.Measure(text) * fontSize;
            ActiveFont.Draw(text, pointer, justify, Vector2.One * fontSize, color);
            return measure;
        }
        protected static void Move(ref Vector2 vec, float x, float y) {
            SummaryHud.Move(ref vec, x, y);
        }
        protected static Vector2 MoveCopy(Vector2 vec, float x, float y) {
            return SummaryHud.MoveCopy(vec, x, y);
        }

        public override void Render() {
            base.Render();

            if (MissingPath) {
                Vector2 middle = Position + new Vector2(SummaryHud.Settings.Width / 2, SummaryHud.Settings.Height / 2 - 100);
                DrawText("No Path", middle, 4, new Color(0.4f, 0.4f, 0.4f), new Vector2(0.5f, 0.5f));
            }
        }
    }
}
