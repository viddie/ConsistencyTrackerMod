using Celeste.Mod.ConsistencyTracker.Enums;
using Celeste.Mod.Helpers;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities {

    [Tracked]
    public class TextOverlay : Entity {

        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;

        public StatTextComponent StatText1 { get; set; }
        public StatTextComponent StatText2 { get; set; }
        public StatTextComponent StatText3 { get; set; }
        public StatTextComponent StatText4 { get; set; }

        public TextOverlay() {
            Depth = -101;
            Tag = Tags.HUD | Tags.Global | Tags.PauseUpdate | Tags.TransitionUpdate;

            StatText1 = new StatTextComponent(true, true, StatTextPosition.TopLeft);
            StatText2 = new StatTextComponent(true, true, StatTextPosition.TopRight);
            StatText3 = new StatTextComponent(true, true, StatTextPosition.BottomLeft);
            StatText4 = new StatTextComponent(true, true, StatTextPosition.BottomRight);
            InitStatTextOptions();

            ApplyModSettings();
        }

        public void ApplyModSettings() {
            ConsistencyTrackerSettings settings = Mod.ModSettings;

            Visible = settings.IngameOverlayTextEnabled && settings.Enabled;

            SetTextVisible(1, settings.IngameOverlayText1Enabled);
            SetTextHideInGolden(1, settings.IngameOverlayText1HideWithGolden);
            SetTextPosition(1, settings.IngameOverlayText1Position);
            SetTextOffsetX(1, settings.IngameOverlayText1OffsetX);
            SetTextOffsetY(1, settings.IngameOverlayText1OffsetY);
            SetTextSize(1, settings.IngameOverlayText1Size);

            SetTextVisible(2, settings.IngameOverlayText2Enabled);
            SetTextHideInGolden(2, settings.IngameOverlayText2HideWithGolden);
            SetTextPosition(2, settings.IngameOverlayText2Position);
            SetTextOffsetX(2, settings.IngameOverlayText2OffsetX);
            SetTextOffsetY(2, settings.IngameOverlayText2OffsetY);
            SetTextSize(2, settings.IngameOverlayText2Size);

            SetTextVisible(3, settings.IngameOverlayText3Enabled);
            SetTextHideInGolden(3, settings.IngameOverlayText3HideWithGolden);
            SetTextPosition(3, settings.IngameOverlayText3Position);
            SetTextOffsetX(3, settings.IngameOverlayText3OffsetX);
            SetTextOffsetY(3, settings.IngameOverlayText3OffsetY);
            SetTextSize(3, settings.IngameOverlayText3Size);

            SetTextVisible(4, settings.IngameOverlayText4Enabled);
            SetTextHideInGolden(4, settings.IngameOverlayText4HideWithGolden);
            SetTextPosition(4, settings.IngameOverlayText4Position);
            SetTextOffsetX(4, settings.IngameOverlayText4OffsetX);
            SetTextOffsetY(4, settings.IngameOverlayText4OffsetY);
            SetTextSize(4, settings.IngameOverlayText4Size);
        }
        public void ApplyTexts() {

        }
        
        public void SetVisibility(bool visible) {
            Visible = visible && Mod.ModSettings.Enabled;
            Mod.LogVerbose($"Set text overlay visibility to '{visible}'");
        }

        public override void Update() {
            base.Update();
            
            if (Mod.ModSettings.ButtonTogglePauseDeathTracking.Pressed) {
                Mod.ModSettings.PauseDeathTracking = !Mod.ModSettings.PauseDeathTracking;
                Mod.Log($"ButtonTogglePauseDeathTracking: Toggled pause death tracking to {Mod.ModSettings.PauseDeathTracking}");
            }

            if (Mod.ModSettings.ButtonToggleTextOverlayEnabled.Pressed) {
                bool currentVisible = Mod.ModSettings.IngameOverlayTextEnabled;
                Mod.ModSettings.IngameOverlayTextEnabled = !currentVisible;
                SetVisibility(!currentVisible);

                Mod.Log($"ButtonToggleTextOverlayEnabled: Toggled text overlay to {Mod.ModSettings.IngameOverlayTextEnabled}");
            }

            if (Mod.ModSettings.ButtonAddRoomSuccess.Pressed) {
                if (Mod.CurrentChapterStats != null) {
                    Mod.Log($"ButtonAddRoomSuccess: Adding room attempt success");
                    Mod.AddRoomAttempt(true);
                }
            }

            if (Mod.ModSettings.ButtonRemoveRoomLastAttempt.Pressed) {
                if (Mod.CurrentChapterStats != null) {
                    Mod.Log($"ButtonRemoveRoomLastAttempt: Removing last room attempt");
                    Mod.RemoveLastAttempt();
                }
            }

            if (Mod.ModSettings.ButtonRemoveRoomDeathStreak.Pressed) {
                if (Mod.CurrentChapterStats != null) {
                    Mod.Log($"ButtonRemoveRoomDeathStreak: Removing room death streak");
                    Mod.RemoveLastDeathStreak();
                }
            }
        }

        public void InitStatTextOptions() {
            StatText1.Font = Dialog.Languages["english"].Font;
            StatText1.FontFaceSize = Dialog.Languages["english"].FontFaceSize;

            StatText2.Font = Dialog.Languages["english"].Font;
            StatText2.FontFaceSize = Dialog.Languages["english"].FontFaceSize;

            StatText3.Font = Dialog.Languages["english"].Font;
            StatText3.FontFaceSize = Dialog.Languages["english"].FontFaceSize;

            StatText4.Font = Dialog.Languages["english"].Font;
            StatText4.FontFaceSize = Dialog.Languages["english"].FontFaceSize;
        }

        public void SetTextVisible(int textNum, bool visible) {
            StatTextComponent statText = GetStatText(textNum);
            statText.OptionVisible = visible;
            UpdateTextVisibility();
            Mod.LogVerbose($"Text '{textNum}' -> Set text visibility to '{visible}'");
        }
        public void SetTextHideInGolden(int textNum, bool visible) {
            StatTextComponent statText = GetStatText(textNum);
            statText.HideInGolden = visible;
            Mod.LogVerbose($"Text '{textNum}' -> Set text hide in golden run to '{visible}'");
        }
        public void SetText(int textNum, string text) {
            StatTextComponent statText = GetStatText(textNum);
            statText.Text = text.Replace("\\n", "\n");
            Mod.LogVerbose($"Text '{textNum}' -> Set text to '{text.Replace("\n", "\\n")}'");
        }
        public void SetTextPosition(int textNum, StatTextPosition pos) {
            StatTextComponent statText = GetStatText(textNum);
            statText.SetPosition(pos);
            Mod.LogVerbose($"Text '{textNum}' -> Set text position to '{pos}'");
        }
        public void SetTextOffsetX(int textNum, int offset) {
            StatTextComponent statText = GetStatText(textNum);
            statText.OffsetX = offset;
            statText.SetPosition();
            Mod.LogVerbose($"Text '{textNum}' -> Set text X offset to '{offset}'");
        }
        public void SetTextOffsetY(int textNum, int offset)
        {
            StatTextComponent statText = GetStatText(textNum);
            statText.OffsetY = offset;
            statText.SetPosition();
            Mod.LogVerbose($"Text '{textNum}' -> Set text Y offset to '{offset}'");
        }
        //size in percent as int
        public void SetTextSize(int textNum, int size) {
            StatTextComponent statText = GetStatText(textNum);
            statText.Scale = (float)size / 100;
            Mod.LogVerbose($"Text '{textNum}' -> Set text size to '{size}%'");
        }
        
        //size in percent as int
        public void SetTextAlpha(int textNum, int alpha) {
            StatTextComponent statText = GetStatText(textNum);
            statText.Alpha = (float)alpha / 100;
        }


        public StatTextComponent GetStatText(int textNum) {
            if (textNum == 1) {
                return StatText1;
            } else if (textNum == 2) {
                return StatText2;
            } else if (textNum == 3) {
                return StatText3;
            } else if (textNum == 4) {
                return StatText4;
            } else {
                return null;
            }
        }
        public void UpdateTextVisibility() {
            bool holdingGolden = Mod.PlayerIsHoldingGolden;
            SetGoldenState(holdingGolden);
        }

        public void SetGoldenState(bool playerHasGolden) {
            if (playerHasGolden) {
                GetStatText(1).Visible = GetStatText(1).OptionVisible && !(GetStatText(1).HideInGolden);
                GetStatText(2).Visible = GetStatText(2).OptionVisible && !(GetStatText(2).HideInGolden);
                GetStatText(3).Visible = GetStatText(3).OptionVisible && !(GetStatText(3).HideInGolden);
                GetStatText(4).Visible = GetStatText(4).OptionVisible && !(GetStatText(4).HideInGolden);
            } else {
                GetStatText(1).Visible = GetStatText(1).OptionVisible;
                GetStatText(2).Visible = GetStatText(2).OptionVisible;
                GetStatText(3).Visible = GetStatText(3).OptionVisible;
                GetStatText(4).Visible = GetStatText(4).OptionVisible;
            }
        }

        public override void Render() {
            base.Render();

            if (StatText1.Visible) {
                StatText1.Render();
            }
            if (StatText2.Visible) {
                StatText2.Render();
            }
            if (StatText3.Visible) {
                StatText3.Render();
            }
            if (StatText4.Visible) {
                StatText4.Render();
            }
        }
    }
}
