using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageCurrentSession : SummaryHudPage {
        
        private ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        private StatManager Stats => Mod.StatsManager;

        public string TextLastRuns { get; set; }
        public string TextBestRuns { get; set; }
        public string TextAttemptCount { get; set; }
        
        public List<string> GoldenDeathsTree { get; set; }

        public PageCurrentSession(string name) : base(name) {
            
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (path == null) {
                MissingPath = true;
                return;
            }

            string format = "Golden Deaths This Session: {chapter:goldenDeathsSession}";
            TextAttemptCount = Stats.FormatVariableFormat(format);

            format = "Best Runs" +
                "\n#1 {pb:bestSession} ({pb:bestRoomNumberSession}/{chapter:roomCount})" +
                "\n#2 {pb:bestSession#2} ({pb:bestRoomNumberSession#2}/{chapter:roomCount})" +
                "\n#3 {pb:bestSession#3} ({pb:bestRoomNumberSession#3}/{chapter:roomCount})" +
                "\n#4 {pb:bestSession#4} ({pb:bestRoomNumberSession#4}/{chapter:roomCount})" +
                "\n#5 {pb:bestSession#5} ({pb:bestRoomNumberSession#5}/{chapter:roomCount})";
            TextBestRuns = Stats.FormatVariableFormat(format);

            format = "Last runs\n1 -> ({chapter:lastRunDistance#1}/{chapter:roomCount})" +
                "\n2 -> ({chapter:lastRunDistance#2}/{chapter:roomCount})" +
                "\n3 -> ({chapter:lastRunDistance#3}/{chapter:roomCount})" +
                "\n4 -> ({chapter:lastRunDistance#4}/{chapter:roomCount})" +
                "\n5 -> ({chapter:lastRunDistance#5}/{chapter:roomCount})";
            TextLastRuns = Stats.FormatVariableFormat(format);

            
            GoldenDeathsTree = new List<string>() { };
            Dictionary<RoomInfo, Tuple<int, float, int, float>> roomGoldenSuccessRateData = ChokeRateStat.GetRoomData(path, stats);
            //Walk path
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                GoldenDeathsTree.Add($"{cpInfo.Name} (Deaths: {cpInfo.Stats.GoldenBerryDeathsSession})");
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = stats.GetRoom(rInfo);
                    Tuple<int, float, int, float> data = roomGoldenSuccessRateData[rInfo];
                    string successRate = float.IsNaN(data.Item4) ? "-%" : $"{StatManager.FormatPercentage(data.Item4)}";
                    GoldenDeathsTree.Add($"    {rInfo.GetFormattedRoomName(StatManager.RoomNameType)}: {successRate} ({data.Item3 - rStats.GoldenBerryDeathsSession}/{data.Item3})");
                }
            }
        }

        public override void Render() {
            base.Render();

            if (MissingPath) return;

            Vector2 pointer = Position + Vector2.Zero;
            Vector2 pointerCol2 = MoveCopy(pointer, SummaryHud.Settings.Width / 2 - 150, 0);

            //Left Column
            Vector2 measure = DrawText(TextAttemptCount, pointer, SummaryHud.Settings.FontMultMediumSmall, Color.White);

            Move(ref pointer, 0, measure.Y + SummaryHud.Settings.Margin * 2);
            measure = DrawText(TextBestRuns, pointer, SummaryHud.Settings.FontMultSmall, Color.White);

            Move(ref pointer, 0, measure.Y + SummaryHud.Settings.Margin * 2);
            measure = DrawText(TextLastRuns, pointer, SummaryHud.Settings.FontMultSmall, Color.White);

            Move(ref pointer, 0, measure.Y + SummaryHud.Settings.Margin);

            //Right Column: Draw deaths tree
            //We start drawing GoldenDeathTree lines at pointerCol2, moving the pointer downwards until we hit the limit
            //we mark down the longest line by width, and when we hit the limit, move the pointer back to the start, shifted by the longest line width
            //then continue
            measure = DrawText("Session Golden Deaths:", pointerCol2, SummaryHud.Settings.FontMultMediumSmall, Color.White);
            Move(ref pointerCol2, 0, measure.Y);

            Vector2 startPointer = MoveCopy(pointerCol2, 0, 0);
            float maxWidth = 0;
            foreach (string line in GoldenDeathsTree) {
                float fontMult = 0;

                if (GoldenDeathsTree.Count > 100) {
                    fontMult = line.StartsWith("    ") ? SummaryHud.Settings.FontMultAnt : SummaryHud.Settings.FontMultSmall;
                } else if (GoldenDeathsTree.Count > 80) {
                    fontMult = line.StartsWith("    ") ? SummaryHud.Settings.FontMultVerySmall : SummaryHud.Settings.FontMultSmall;
                } else {
                    fontMult = line.StartsWith("    ") ? SummaryHud.Settings.FontMultSmall : SummaryHud.Settings.FontMultMediumSmall;
                }

                measure = DrawText(line, pointerCol2, fontMult, Color.White);
                if (measure.X > maxWidth) maxWidth = measure.X;

                Move(ref pointerCol2, 0, measure.Y);

                if (!SummaryHud.Instance.IsInBounds(pointerCol2, 0, measure.Y * 2)) {
                    pointerCol2 = MoveCopy(startPointer, maxWidth + 50, 0);
                    Move(ref startPointer, maxWidth + 50, 0);
                    
                    if (!SummaryHud.Instance.IsInBounds(pointerCol2)) break;
                }
            }
        }
    }
}
