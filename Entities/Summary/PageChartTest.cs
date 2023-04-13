using Celeste.Mod.ConsistencyTracker.Entities.Summary.Chart;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageChartTest : SummaryHudPage {

        private LineChart TestChart { get; set; }
        private LineChart TestChartSmall { get; set; }
        
        public PageChartTest(string name) : base(name) {
            TestChart = new LineChart(new ChartSettings() {
                ChartWidth = 1000,
                ChartHeight = 650,
            });

            TestChartSmall = new LineChart(new ChartSettings() {
                ChartWidth = 200,
                ChartHeight = 150,
                Scale = 0.5f,
                BackgroundColor = new Color(0f, 0f, 0f, 0f),
            });
        }

        public override void Update() {
            base.Update();

        }

        public override void Render() {
            base.Render();

            TestChart.Position = MoveCopy(Position, 50, 0);
            TestChart.Render();

            TestChartSmall.Position = MoveCopy(Position, 50 + TestChart.Settings.ChartWidth + BasicMargin * 5, 0);
            TestChartSmall.Render();
        }
    }
}
