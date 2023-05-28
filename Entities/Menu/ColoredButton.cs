using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Menu {
    public class ColoredButton : TextMenu.Button {

        public Color? HighlightColor;
        
        public ColoredButton(string label) : base(label) {
            
        }

        public override void Render(Vector2 position, bool highlighted) {
            Color prevHighlightColor = Container.HighlightColor;
            if (HighlightColor != null) {
                Container.HighlightColor = HighlightColor.Value;
            }
            
            base.Render(position, highlighted);
            
            if (HighlightColor != null) {
                Container.HighlightColor = prevHighlightColor;
            }
        }
    }
}
