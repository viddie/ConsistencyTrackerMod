using Celeste.Mod.ConsistencyTracker.Enums;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    public class TextOverlay : Entity {

        List<StatTextComponent> StatTexts = new List<StatTextComponent>();
        public StatTextComponent StatText { get; set; }

        public TextOverlay() {
            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;

            StatText = new StatTextComponent(true, true, StatTextPosition.TopRight);
            InitStatTextOptions();

            ApplyModSettings();
        }

        public void ApplyModSettings() {
            StatText.SetPosition(ConsistencyTrackerModule.Instance.ModSettings.IngameOverlayTextPosition);
            Visible = ConsistencyTrackerModule.Instance.ModSettings.IngameOverlayTextEnabled;
            SetTextOffset(ConsistencyTrackerModule.Instance.ModSettings.IngameOverlayTextOffset);
            SetTextSize(ConsistencyTrackerModule.Instance.ModSettings.IngameOverlayTextSize);
        }

        public override void Update() {
            base.Update();

            StatText.Update();
        }

        public void InitStatTextOptions() {
            StatText.Scale = 1f;
            StatText.Alpha = 1f;
            StatText.Font = Dialog.Languages["english"].Font;
            StatText.FontFaceSize = Dialog.Languages["english"].FontFaceSize;
            StatText.TextColor = Color.White * StatText.Alpha;
            StatText.Position = StatTextPosition.TopRight;
            StatText.StrokeSize = 2f;
            StatText.StrokeColor = Color.Black * StatText.Alpha;
            StatText.Justify = new Vector2();
        }

        public void SetText(string text) {
            StatText.Text = text;
        }
        public void SetTextPosition(StatTextPosition pos) {
            StatText.SetPosition(pos);
        }
        public void SetTextOffset(int offset) {
            StatText.Offset = offset;
            StatText.SetPosition();
        }
        //size in percent as int
        public void SetTextSize(int size) {
            StatText.Scale = (float)size / 100;
        }

        public override void Render() {
            base.Render();

            //ConsistencyTrackerModule.Instance.Log("[IngameOverlay.Update] Rendering overlay...");

            StatText.Render();
        }



        public void TestStyle(int style) {
            if (style == 1) {
                InitStatTextOptions();

            } else if (style == 2) {
                StatText.Scale = 2f;

            } else if (style == 3) {
                StatText.Scale = 0.5f;

            } else if (style == 4) {
                StatText.Justify = new Vector2(0, 0);//top-left-bound text

            } else if (style == 5) {
                StatText.Justify = new Vector2(1, 0);//top-right-bound text

            } else if (style == 6) {
                StatText.Justify = new Vector2(-1, 0);//???????????

            } else if (style == 7) {
                StatText.Justify = new Vector2(0, 1);//bottom-left-bound

            } else if (style == 8) {
                StatText.Justify = new Vector2(0, -1);//???????????

            } else if (style == 9) {
                StatText.Justify = new Vector2(1, 1);//bottom-right-bound

            }
        }


    }
}
