using Celeste.Mod.ConsistencyTracker.Entities.Summary.Tables;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageOverall : SummaryHudPage {

        public string TextAttemptCount { get; set; }
        public string TextAvgSuccessRate { get; set; }
        public string TextAvgSuccessRateCheckpoints { get; set; }
        public string TextRedRooms { get; set; }
        
        public List<string> GoldenDeathsTree { get; set; }


        private readonly int countBestRuns = 5;
        private List<ProgressBar> BestRunsProgressBars { get; set; }
        private List<Tuple<string, int>> BestRunsData { get; set; } = new List<Tuple<string, int>>();
        private int ChapterRoomCount { get; set; }
        
        public Table TestTable { get; set; }

        public PageOverall(string name) : base(name) {
            int barWidth = 400;
            int barHeight = 13;
            BestRunsProgressBars = new List<ProgressBar>();
            for (int i = 0; i < countBestRuns; i++) {
                BestRunsProgressBars.Add(new ProgressBar(0, 100) { BarWidth = barWidth, BarHeight = barHeight });
            }

            TestTable = new Table() {
                Settings = new TableSettings() {
                    FontMultAll = 0.3f,
                    BackgroundColorEven = new Color(0.1f, 0.1f, 0.1f, 0.1f),
                    BackgroundColorOdd = new Color(0.2f, 0.2f, 0.2f, 0.25f),
                },
            };
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (path == null) {
                MissingPath = true;
                return;
            }

            string format = "Golden Deaths Total: {chapter:goldenDeaths}";
            TextAttemptCount = Stats.FormatVariableFormat(format);

            format = "Average Success Rate: {chapter:successRate}" +
                "\nChapter Golden Chance: {chapter:goldenChance}";
            TextAvgSuccessRate = Stats.FormatVariableFormat(format);

            TextAvgSuccessRateCheckpoints = "Checkpoints:";
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                TextAvgSuccessRateCheckpoints += $"\n  {cpInfo.Name} -> SR {StatManager.FormatPercentage(cpInfo.Stats.SuccessRate)}, Golden Chance {StatManager.FormatPercentage(cpInfo.Stats.GoldenChance)}";
            }

            //format = "Best Runs" +
            //    "\n#1 {pb:best} ({pb:bestRoomNumber}/{chapter:roomCount})" +
            //    "\n#2 {pb:best#2} ({pb:bestRoomNumber#2}/{chapter:roomCount})" +
            //    "\n#3 {pb:best#3} ({pb:bestRoomNumber#3}/{chapter:roomCount})" +
            //    "\n#4 {pb:best#4} ({pb:bestRoomNumber#4}/{chapter:roomCount})" +
            //    "\n#5 {pb:best#5} ({pb:bestRoomNumber#5}/{chapter:roomCount})";
            //TextBestRuns = Stats.FormatVariableFormat(format);
            ChapterRoomCount = path.RoomCount;
            for (int i = 0; i < countBestRuns; i++) {
                string[] split = Stats.FormatVariableFormat($"{{pb:best#{i + 1}}};{{pb:bestRoomNumber#{i + 1}}}").Split(';');
                string bestRoom = split[0];
                string bestRoomNumberStr = split[1];

                if (!int.TryParse(bestRoomNumberStr, out int bestRoomNumber)) {
                    bestRoomNumber = 0;
                }

                BestRunsData.Add(Tuple.Create(bestRoom, bestRoomNumber));
            }


            format = "Red Rooms\n{chapter:listColor-red}";
            TextRedRooms = Stats.FormatVariableFormat(format);


            GoldenDeathsTree = new List<string>() { };
            Dictionary<RoomInfo, Tuple<int, float, int, float>> roomGoldenSuccessRateData = ChokeRateStat.GetRoomData(path, stats);
            //Walk path
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                GoldenDeathsTree.Add($"{cpInfo.Name} (Deaths: {cpInfo.Stats.GoldenBerryDeaths})");
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = stats.GetRoom(rInfo);
                    Tuple<int, float, int, float> data = roomGoldenSuccessRateData[rInfo];
                    string successRate = float.IsNaN(data.Item2) ? "-%" : $"{StatManager.FormatPercentage(data.Item2)}";
                    GoldenDeathsTree.Add($"    {rInfo.GetFormattedRoomName(StatManager.RoomNameType)}: {successRate} ({data.Item1 - rStats.GoldenBerryDeaths}/{data.Item1})");
                }
            }

            DataColumn roomColumn = new DataColumn("Room", typeof(string));
            DataColumn successRateColumn = new DataColumn("Choke Rate", typeof(float));
            DataColumn deathsColumn = new DataColumn("Successes/Entries", typeof(string));

            DataTable testData = new DataTable() {
                Columns = { roomColumn, successRateColumn, deathsColumn }
            };
            //Walk path
            StatCount = (int) Math.Ceiling(((float)path.RoomCount) / 20);
            int startIndex = SelectedStat * 20;
            int index = 0;
            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                //if (index >= startIndex && index < startIndex + 20) {
                //    testData.Rows.Add($"{cpInfo.Name} (Deaths: {cpInfo.Stats.GoldenBerryDeaths})", float.NaN, "");
                //}
                //index++;

                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = stats.GetRoom(rInfo);
                    Tuple<int, float, int, float> data = roomGoldenSuccessRateData[rInfo];

                    if (index >= startIndex && index < startIndex + 20) {
                        testData.Rows.Add(rInfo.GetFormattedRoomName(StatManager.RoomNameType), float.IsNaN(data.Item2) ? float.NaN : 1 - data.Item2, $"{data.Item1 - rStats.GoldenBerryDeaths}/{data.Item1}");
                    }
                    index++;
                }
            }
            TestTable.ColSettings = new Dictionary<DataColumn, ColumnSettings>() {
                [roomColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center, MinWidth = 120 },
                [successRateColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Right, ValueFormatter = (obj) => {
                    float val = (float)obj;
                    return float.IsNaN(val) ? "-%" : $"{StatManager.FormatPercentage(val)}";
                } },
                [deathsColumn] = new ColumnSettings() { Alignment = ColumnSettings.TextAlign.Center },
            };
            TestTable.Data = testData;
            TestTable.Settings.Title = $"Table Page ({SelectedStat + 1}/{StatCount})";
            TestTable.Update();
        }

        public override void Render() {
            base.Render();

            if (MissingPath) return;

            Vector2 pointer = Position + Vector2.Zero;
            Vector2 pointerCol2 = MoveCopy(pointer, SummaryHud.Settings.Width / 2 - 0, 0);
            
            //Left Column
            Vector2 measure = DrawText(TextAttemptCount, pointer, FontMultMediumSmall, Color.White);

            Move(ref pointer, 0, measure.Y + BasicMargin);
            measure = DrawText(TextAvgSuccessRate, pointer, FontMultSmall, Color.White);

            Move(ref pointer, 0, measure.Y + BasicMargin);
            measure = DrawText(TextAvgSuccessRateCheckpoints, pointer, FontMultSmall, Color.White);


            Move(ref pointer, 0, measure.Y + BasicMargin * 2);
            measure = DrawText("Best Runs", pointer, FontMultSmall, Color.White);

            Move(ref pointer, 0, measure.Y + BasicMargin * 3);
            float maxLabelHeight = 0;
            float maxWidth = 0;
            //Determine highest label width
            for (int i = 0; i < countBestRuns; i++) {
                string bestRoom = BestRunsData[i].Item1;
                int bestRoomNumber = BestRunsData[i].Item2;

                measure = ActiveFont.Measure($"{bestRoom} ({bestRoomNumber}/{ChapterRoomCount})") * FontMultSmall;
                if (measure.Y > maxLabelHeight) maxLabelHeight = measure.Y;

                measure = ActiveFont.Measure($"#{i + 1}") * FontMultSmall;
                if (measure.X > maxWidth) maxWidth = measure.X;
            }
            for (int i = 0; i < countBestRuns; i++) {
                string bestRoom = BestRunsData[i].Item1;
                int bestRoomNumber = BestRunsData[i].Item2;

                measure = DrawText($"#{i + 1}", pointer, FontMultSmall, Color.White);

                ProgressBar bar = BestRunsProgressBars[i];
                bar.FontMult = FontMultSmall;
                bar.Value = bestRoomNumber;
                bar.MaxValue = ChapterRoomCount;
                bar.RightLabel = $"{ChapterRoomCount}";
                bar.ValueLabel = bestRoomNumber == 0 ? "" : $"{bestRoom} ({bestRoomNumber}/{ChapterRoomCount})";
                bar.Color = new Color(242, 182, 0);
                bar.Position = MoveCopy(pointer, maxWidth + 10, measure.Y / 2 - bar.BarHeight / 2);
                bar.Render();

                Move(ref pointer, 0, Math.Max(bar.BarHeight, measure.Y) + maxLabelHeight + BasicMargin);
            }
            

            Move(ref pointer, 0, measure.Y - BasicMargin);
            measure = DrawText(TextRedRooms, pointer, FontMultSmall, Color.White);

            Move(ref pointer, 0, measure.Y + BasicMargin);

            //Right Column: Draw deaths tree
            //We start drawing GoldenDeathTree lines at pointerCol2, moving the pointer downwards until we hit the limit
            //we mark down the longest line by width, and when we hit the limit, move the pointer back to the start, shifted by the longest line width
            //then continue
            //measure = DrawText("All Golden Deaths:", pointerCol2, FontMultMediumSmall, Color.White);
            //Move(ref pointerCol2, 0, measure.Y);

            //Vector2 startPointer = MoveCopy(pointerCol2, 0, 0);
            //maxWidth = 0;
            //foreach (string line in GoldenDeathsTree) {
            //    float fontMult = 0;

            //    if (GoldenDeathsTree.Count > 100) {
            //        fontMult = line.StartsWith("    ") ? FontMultAnt : FontMultSmall;
            //    } else if (GoldenDeathsTree.Count > 80) {
            //        fontMult = line.StartsWith("    ") ? FontMultVerySmall : FontMultSmall;
            //    } else {
            //        fontMult = line.StartsWith("    ") ? FontMultSmall : FontMultMediumSmall;
            //    }

            //    measure = DrawText(line, pointerCol2, fontMult, Color.White);
            //    if (measure.X > maxWidth) maxWidth = measure.X;

            //    Move(ref pointerCol2, 0, measure.Y);

            //    if (!SummaryHud.Instance.IsInBounds(pointerCol2, 0, measure.Y)) {
            //        pointerCol2 = MoveCopy(startPointer, maxWidth + 50, 0);
            //        Move(ref startPointer, maxWidth + 50, 0);

            //        if (!SummaryHud.Instance.IsInBounds(pointerCol2)) break;
            //    }
            //}

            Move(ref pointerCol2, 0, 0);
            TestTable.Position = pointerCol2;
            TestTable.Render();

            //Move(ref pointerCol2, TestTable.TotalWidth / 2, -5);
            //DrawText($"Table Page ({SelectedStat+1}/{StatCount})", pointerCol2, FontMultMedium, Color.White, new Vector2(0.5f, 1f));
        }
    }
}
