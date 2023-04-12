using Celeste.Mod.ConsistencyTracker.Enums;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    
    [Tracked]
    public class SummaryHud : Entity {
        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        
        public static SummaryHud Instance { get; set; }

        public int SelectedTab { get; set; } = 0;
        public List<SummaryHudPage> Tabs = new List<SummaryHudPage>();
        public Rectangle HudBounds { get; set; }

        public static class Settings {
            public static int Width { get; set; } = 1400;
            public static int Height { get; set; } = 800;
            public static int Margin { get; set; } = 10;
            public static int MarginLarge { get; set; } = 25;
            public static int ContentYMargin { get; set; } = 20;

            public static float FontMultLarge { get; set; } = 1;
            public static float FontMultMedium { get; set; } = 0.5f;
            public static float FontMultMediumSmall { get; set; } = 0.42f;
            public static float FontMultSmall { get; set; } = 0.35f;
            public static float FontMultVerySmall { get; set; } = 0.25f;
            public static float FontMultAnt { get; set; } = 0.18f;

            public static string TitleText { get; set; } = "Summary";
        }
        
        public SummaryHud() {
            Instance = this;
            Depth = -102;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;

            HudBounds = new Rectangle(Engine.Width / 2 - Settings.Width / 2, Engine.Height / 2 - Settings.Height / 2, Settings.Width, Settings.Height);

            Tabs.Add(new PageCurrentSession("Current Session") { });
            Tabs.Add(new PageOverall("Overall") { });

            ApplyModSettings();

            Visible = false;
        }

        private void ApplyModSettings() { 
            
        }


        public override void Update() {
            base.Update();
            
            if (Mod.ModSettings.ButtonToggleSummaryHud.Pressed) {
                if (!Mod.StatsManager.HadPass) {
                    Mod.Log("No pass yet, not showing summary hud");
                    return;
                }

                Visible = !Visible;
                Mod.Log($"Summary HUD is now '{Visible}'");
                Tabs[SelectedTab].Update();
            }
            if (Mod.ModSettings.ButtonSummaryHudNextTab.Pressed) {
                if (!Mod.StatsManager.HadPass) {
                    Mod.Log("No pass yet, not showing summary hud");
                    return;
                }

                SelectedTab = (SelectedTab + 1) % Tabs.Count;
                Tabs[SelectedTab].Update();
            }

            if (Engine.Scene is Level level && level.PauseMainMenuOpen) {
                Visible = false;
            }
        }
        
        public override void Render() {
            if (!Active) return;
            
            base.Render();

            Color backdropColor = new Color(0, 0, 0, 0.85f);
            Color activeTabColor = new Color(1, 1, 1, 1f);
            Color notActiveTabColor = new Color(0.3f, 0.3f, 0.3f, 1f);

            //Render backdrop
            Vector2 pointer = new Vector2(Engine.Width / 2 - Settings.Width / 2 - Settings.Margin, Engine.Height / 2 - Settings.Height / 2 - Settings.Margin);
            Draw.Rect(pointer, Settings.Width + Settings.Margin, Settings.Height + Settings.Margin, backdropColor);

            //Render Title
            Move(ref pointer, Settings.Margin * 2, Settings.Margin);
            ActiveFont.Draw(Settings.TitleText, pointer, Vector2.Zero, Vector2.One * Settings.FontMultLarge, Color.White);

            Vector2 titleMeasures = ActiveFont.Measure(Settings.TitleText);
            Vector2 contentPointer = MoveCopy(pointer, 0, titleMeasures.Y + Settings.ContentYMargin);

            //Render Tabs
            Move(ref pointer, titleMeasures.X + Settings.Margin * 2, titleMeasures.Y / 2);
            float lineHeight = ActiveFont.FontSize.Size * Settings.FontMultMedium;
            for (int i = 0; i < Tabs.Count; i++) {
                SummaryHudPage tab = Tabs[i];
                
                //Draw separator
                Draw.Line(pointer.X, pointer.Y - lineHeight, pointer.X, pointer.Y + lineHeight, notActiveTabColor);

                //Draw tab name
                Move(ref pointer, Settings.Margin, 0);
                Color color = SelectedTab == i ? activeTabColor : notActiveTabColor;
                ActiveFont.Draw(tab.Name, pointer, new Vector2(0, 0.5f), Vector2.One * Settings.FontMultMedium, color);
                //Draw.Circle(pointer, 10, Color.Red, 50);
                //Draw.Circle(pointer, 3, Color.Red, 3);

                Vector2 textMeasure = ActiveFont.Measure(tab.Name) * Settings.FontMultMedium;
                Move(ref pointer, textMeasure.X + Settings.Margin, 0);
            }


            //Render Tab content
            Tabs[SelectedTab].Position = contentPointer;
            Tabs[SelectedTab].Render();
        }


        public bool IsInBounds(Vector2 point, float shrinkX = 0, float shrinkY = 0) {
            //Check if point is in bounds of the hud, without using HudBounds.Contains
            return point.X > HudBounds.X + shrinkX
                && point.X < HudBounds.X + HudBounds.Width - shrinkX
                && point.Y > HudBounds.Y + shrinkY
                && point.Y < HudBounds.Y + HudBounds.Height - shrinkY;
        }



        public static void Move(ref Vector2 vec, float x, float y) {
            vec.X += x;
            vec.Y += y;
        }
        public static Vector2 MoveCopy(Vector2 vec, float x, float y) {
            return new Vector2(vec.X + x, vec.Y + y);
        }
    }
}
