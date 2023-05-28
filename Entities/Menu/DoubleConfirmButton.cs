using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Menu {
    public class DoubleConfirmButton : ColoredButton {

        public string ConfirmationString = " - Confirm";
        private bool HasClickedOnce = false;

        public new Action OnLeave;
        public Action OnDoubleConfirmation;

        public DoubleConfirmButton(string label) : base(label) {
            base.OnLeave = () => {
                OnLeave?.Invoke();
                if (HasClickedOnce) {
                    SetClickState(false);
                }
            };
        }

        private void SetClickState(bool clickedOnce) {
            if (string.IsNullOrEmpty(ConfirmationString)) return;
            
            if (clickedOnce) {
                HasClickedOnce = true;
                Label += ConfirmationString;
            } else {
                HasClickedOnce = false;
                Label = Label.Replace(ConfirmationString, "");
            }
        }

        public override void ConfirmPressed() {
            if (!string.IsNullOrEmpty(ConfirmSfx)) {
                Audio.Play(ConfirmSfx);
            }

            if (!HasClickedOnce && !string.IsNullOrEmpty(ConfirmationString)) {
                SetClickState(true);
                return;
            } else {
                SetClickState(false);
            }

            OnDoubleConfirmation?.Invoke();
        }

        //public override void Render(Vector2 position, bool highlighted) {
        //    base.Render(position, highlighted);
        //}
    }
}
