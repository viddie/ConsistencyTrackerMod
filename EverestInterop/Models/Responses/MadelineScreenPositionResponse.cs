using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.ConsistencyTracker.EverestInterop.Models.Responses {
    public class MadelineScreenPositionResponse : Response {
        public Vector2 madelineScreenPosition { get; set; }
        
        public int x { get; set; }
        public int y { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        
        public bool isInBounds { get; set; }
    }
}
