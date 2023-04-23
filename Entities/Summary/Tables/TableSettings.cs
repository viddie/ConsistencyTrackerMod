using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables {
    public class TableSettings {
        public float CellPadding { get; set; } = 5;
        public float FontMultAll { get; set; } = 0.5f;
        public float FontMultHeader { get; set; } = 1f;
        public bool HeaderBold { get; set; } = true;
        public float SeparatorWidth { get; set; } = 2;

        public Color SeparatorColor { get; set; } = Color.Gray;
        public Color TextColor { get; set; } = Color.White;
        public Color BackgroundColorEven { get; set; } = Color.Transparent;
        public Color BackgroundColorOdd { get; set; } = Color.Transparent;
    }
}
