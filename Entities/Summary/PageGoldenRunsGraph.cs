using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageGoldenRunsGraph : SummaryHudPage {


        
        public PageGoldenRunsGraph(string name) : base(name) {
            
        }

        public override void Render() {
            base.Render();

            if (MissingPath) return;

            Vector2 pointer = Position;

            Vector2 measure = DrawText("Graphs???", pointer, FontMultLarge, Color.White);
            Move(ref pointer, 0, measure.Y + BasicMargin);
        }
    }
}
