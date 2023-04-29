using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables {
    public class TableSettings {

        public string Title { get; set; } = "";
        public float FontMultTitle { get; set; } = 2f;

        public float CellPadding { get; set; } = 5;
        public float FontMultAll { get; set; } = 1f;
        
        public bool ShowHeader { get; set; } = true;
        public float FontMultHeader { get; set; } = 1f;

        public float SeparatorWidth { get; set; } = 2;
        public Color SeparatorColor { get; set; } = Color.Gray;
        
        public Color TextColor { get; set; } = Color.White;
        public Color BackgroundColorEven { get; set; } = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        public Color BackgroundColorOdd { get; set; } = new Color(0.2f, 0.2f, 0.2f, 0.25f);
    }
}
