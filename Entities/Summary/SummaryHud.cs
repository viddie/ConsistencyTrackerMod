using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.ConsistencyTracker.Utility;
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

        private static Color BackdropColor = new Color(0, 0, 0, 0.85f);
        private static Color ActiveTabColor = new Color(1, 1, 1, 1f);
        private static Color NotActiveTabColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        public static SummaryHud Instance { get; set; }

        public List<SummaryHudPage> Tabs = new List<SummaryHudPage>();
        public int SelectedTab { get; set; } = 0;
        public int SelectedStat { get; set; } = 0;
        
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

            Tabs.Add(new PageCurrentSession("Session"));
            Tabs.Add(new PageOverall("Overall"));
            Tabs.Add(new PageGoldenRunsGraph("Deaths Graphs"));
            Tabs.Add(new PageChartTest("Charts"));

            ApplyModSettings();

            Visible = false;
        }

        private void ApplyModSettings() { 
            
        }


        public override void Update() {
            base.Update();

            if (Engine.Scene is Level level && level.PauseMainMenuOpen) {
                Visible = false;
                return;
            }

            if (!Mod.StatsManager.HadPass) {
                Mod.Log("No pass yet, not showing summary hud");
                return;
            }
            
            if (Mod.ModSettings.ButtonToggleSummaryHud.Pressed) {
                Visible = !Visible;
                Mod.Log($"Summary HUD is now '{Visible}'");
                Tabs[SelectedTab].Update();
            }
            if (Mod.ModSettings.ButtonSummaryHudNextTab.Pressed) {
                SelectedTab = (SelectedTab + 1) % Tabs.Count;
                Tabs[SelectedTab].Update();
            }
            if (Mod.ModSettings.ButtonSummaryHudNextStat.Pressed) {
                Tabs[SelectedTab].ChangedSelectedStat(1);
                Tabs[SelectedTab].Update();
            }
            if (Mod.ModSettings.ButtonSummaryHudPreviousStat.Pressed) {
                Tabs[SelectedTab].ChangedSelectedStat(-1);
                Tabs[SelectedTab].Update();
            }
        }
        
        public override void Render() {
            if (!Active) return;
            
            base.Render();


            //Render backdrop
            Vector2 pointer = new Vector2(Engine.Width / 2 - Settings.Width / 2 - Settings.Margin * 2, Engine.Height / 2 - Settings.Height / 2 - Settings.Margin);
            Draw.Rect(pointer, Settings.Width + Settings.Margin * 4, Settings.Height + Settings.Margin * 2, BackdropColor);

            //Render Title
            Move(ref pointer, Settings.Margin * 2, Settings.Margin);
            Vector2 origPointer = MoveCopy(pointer, 0, 0);
            Vector2 titleMeasures = DrawHelper.DrawText(Settings.TitleText, pointer, Settings.FontMultLarge, Color.White);
            Vector2 contentPointer = MoveCopy(pointer, 0, titleMeasures.Y + Settings.ContentYMargin);

            //Render Tabs
            Move(ref pointer, titleMeasures.X + Settings.Margin * 2, titleMeasures.Y / 2);
            float lineHeight = ActiveFont.FontSize.Size * Settings.FontMultMedium;
            for (int i = 0; i < Tabs.Count; i++) {
                SummaryHudPage tab = Tabs[i];
                string name = tab.GetName();

                //Draw separator
                Draw.Line(pointer.X, pointer.Y - lineHeight, pointer.X, pointer.Y + lineHeight, NotActiveTabColor);

                //Draw tab name
                Move(ref pointer, Settings.Margin, 0);
                Color color = SelectedTab == i ? ActiveTabColor : NotActiveTabColor;
                Vector2 textMeasure = DrawHelper.DrawText(name, pointer, Settings.FontMultMedium, color, new Vector2(0, 0.5f));
                Move(ref pointer, textMeasure.X + Settings.Margin, 0);
            }

            //Render Tab content
            Tabs[SelectedTab].PageHeight = Settings.Height - (contentPointer.Y - origPointer.Y);
            Tabs[SelectedTab].PageWidth = Settings.Width;
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
