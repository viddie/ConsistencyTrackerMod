using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using FMOD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.ConsistencyTracker.Entities.Summary {
    public class PageGoldenRunsGraph : SummaryHudPage {

        private class Entry {
            public int GoldenDeaths { get; set; }
            public string RoomName { get; set; }
            public string CheckpointName { get; set; }
            public int CheckpointNumber { get; set; }
        }

        private List<Entry> Entries { get; set; }
        private List<Tuple<string, int, int>> CheckpointEntriesCount { get; set; }
        private int TotalGoldenDeaths { get; set; }

        private static readonly List<Color> CheckpointColors = new List<Color>() { 
            Color.Orange,
            Color.OrangeRed,
            Color.DarkRed,
            Color.Purple,
            Color.MediumPurple,
            Color.DarkBlue,
            Color.Blue,
            Color.LightBlue
        };
        private static int GoldenDeathFilterWidth = 1000;
        private static int GoldenDeathFilterHeight = 650;

        public PageGoldenRunsGraph(string name) : base(name) {
            
        }

        public override void Update() {
            base.Update();

            PathInfo path = Stats.LastPassPathInfo;
            ChapterStats stats = Stats.LastPassChapterStats;

            if (path == null) {
                MissingPath = true;
                return;
            }

            Entries = new List<Entry>();
            CheckpointEntriesCount = new List<Tuple<string, int, int>>();

            foreach (CheckpointInfo cpInfo in path.Checkpoints) {
                foreach (RoomInfo rInfo in cpInfo.Rooms) {
                    RoomStats rStats = stats.GetRoom(rInfo);
                    Entries.Add(new Entry() {
                        GoldenDeaths = rStats.GoldenBerryDeaths,
                        RoomName = rInfo.GetFormattedRoomName(StatManager.RoomNameType),
                        CheckpointName = cpInfo.Name,
                        CheckpointNumber = cpInfo.CPNumberInChapter
                    });
                }
                CheckpointEntriesCount.Add(Tuple.Create(cpInfo.Name, cpInfo.RoomCount, cpInfo.Stats.GoldenBerryDeaths));
            }

            TotalGoldenDeaths = path.Stats.GoldenBerryDeaths;
        }

        public override void Render() {
            base.Render();

            if (MissingPath) return;
            
            float sliceHeight = (float)GoldenDeathFilterHeight / Entries.Count;

            Vector2 pointer = Position;
            Vector2 labelPointer = MoveCopy(pointer, GoldenDeathFilterWidth + BasicMargin, sliceHeight / 2);
            int runsRemaining = TotalGoldenDeaths;

            //Backdrop
            Draw.Rect(pointer, GoldenDeathFilterWidth, GoldenDeathFilterHeight, new Color(0.7f, 0.7f, 0.7f, 0.7f));

            float maxWidth = 0;
            string lastCheckpointName = "";
            foreach (Entry entry in Entries) {
                Color color = CheckpointColors[(entry.CheckpointNumber-1) % CheckpointColors.Count];
                
                float widthTop = (float)runsRemaining / TotalGoldenDeaths * GoldenDeathFilterWidth;
                if (widthTop != 0) widthTop = Math.Max(1f, widthTop);
                
                runsRemaining -= entry.GoldenDeaths;
                
                float widthBottom = (float)runsRemaining / TotalGoldenDeaths * GoldenDeathFilterWidth;
                if (widthBottom != 0) widthBottom = Math.Max(1f, widthBottom);

                Vector2 measure = DrawText(entry.RoomName, labelPointer, FontMultAnt, Color.White, new Vector2(0f, 0.5f));
                if (measure.X > maxWidth) maxWidth = measure.X;

                
                DrawHelper.DrawTrapezoid(pointer, widthTop, widthBottom, sliceHeight, color);

                if ((lastCheckpointName != entry.CheckpointName || Entries.Count < 40) && runsRemaining + entry.GoldenDeaths > 0) {
                    Vector2 labelCenterPointer = MoveCopy(labelPointer, -(GoldenDeathFilterWidth / 2 + BasicMargin), 0);
                    DrawText($"{runsRemaining + entry.GoldenDeaths}", labelCenterPointer, FontMultVerySmall, Color.White, new Vector2(0.5f, 0.5f));
                }
                lastCheckpointName = entry.CheckpointName;

                Move(ref pointer, widthTop / 2 - widthBottom / 2, sliceHeight);
                Draw.Line(Position.X, pointer.Y, Position.X + GoldenDeathFilterWidth, pointer.Y, Color.LightGray);
                Move(ref labelPointer, 0, sliceHeight);
            }

            Vector2 cpLabelPointer = MoveCopy(Position, GoldenDeathFilterWidth + BasicMargin * 2 + maxWidth, 0);
            foreach (Tuple<string, int, int> cpEntries in CheckpointEntriesCount) {
                string cpName = cpEntries.Item1;
                int cpEntriesCount = cpEntries.Item2;
                int cpGoldenDeaths = cpEntries.Item3;

                float totalHeight = cpEntriesCount * sliceHeight;
                Move(ref cpLabelPointer, 0, totalHeight / 2);
                DrawText($"{cpName} ({cpGoldenDeaths} deaths)", cpLabelPointer, FontMultSmall, Color.White, new Vector2(0f, 0.5f));
                Move(ref cpLabelPointer, 0, totalHeight / 2);
            }
        }
    }
}
