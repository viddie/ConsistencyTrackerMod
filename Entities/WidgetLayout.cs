using Celeste.Mod.ConsistencyTracker.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities {
    [Serializable]
    public class WidgetLayout {
        public enum LayoutAnchor { 
            TopLeft, TopCenter, TopRight,
            MiddleLeft, MiddleCenter, MiddleRight,
            BottomLeft, BottomCenter, BottomRight
        }

        public bool Enabled { get; set; } = false;

        public LayoutAnchor Anchor { get; set; } = LayoutAnchor.TopLeft;

        public bool HideWithGolden { get; set; } = false;

        public int Size { get; set; } = 100;
        
        public int OffsetX { get; set; } = 5;
        public int OffsetY { get; set; } = 0;
    }
}
