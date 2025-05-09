using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.ConsistencyTracker.Models;
using Celeste.Mod.ConsistencyTracker.Stats;
using Celeste.Mod.ConsistencyTracker.Utility;
using static Celeste.TextMenu;

namespace Celeste.Mod.ConsistencyTracker.Entities.Menu {
    public class EnduranceGraph : Item {
        private static ConsistencyTrackerModule Mod => ConsistencyTrackerModule.Instance;
        
        public string ConfirmSfx = "event:/ui/main/button_select";

        private double Slope = 1.3f;
        private double Power = 1.5f;
        public double TargetSlope = 1.3f;
        public double TargetPower = 1.5f;

        public double CalculatedForHash = 0;
        public Dictionary<int, double> Values = null;
        
        public Color GraphColor = Color.White;
        private float GraphAlpha = 0;
        
        private float GraphWidth = 400;
        private float GraphHeight = 200;

        public EnduranceGraph(double slope, double power) {
            TargetSlope = slope;
            Slope = slope;
            TargetPower = power;
            Power = power;
        }

        public override float LeftWidth() {
            return GraphWidth;
        }

        public override float Height() {
            return GraphHeight;
        }
        
        public override void ConfirmPressed() {
            Audio.Play(ConfirmSfx);
        }

        public override void Update() {
            // Approach the target slope and power
            if (Slope != TargetSlope) {
                Slope = Calc.Approach((float)Slope, (float)TargetSlope, Engine.RawDeltaTime * 0.5f);
                if(Math.Abs(Slope - TargetSlope) < 0.001f) {
                    Slope = TargetSlope;
                }
            }
            if (Power != TargetPower) {
                Power = Calc.Approach((float)Power, (float)TargetPower, Engine.RawDeltaTime * 0.5f);
                if(Math.Abs(Power - TargetPower) < 0.001f) {
                    Power = TargetPower;
                }
            }
            
            double hash = Slope * 7 + Power * 11;
            if (CalculatedForHash != hash && hash != 0) {
                CalculatedForHash = hash;
                Values = new Dictionary<int, double>();
                for (int i = 0; i <= 100; i++) {
                    double t = i / 100f;
                    try {
                        Values[i] = DifficultyWeightStats.EnduranceFactor(t, Slope, Power);
                    } catch (Exception) {
                        // ignored
                    }
                }
            }
        }

        public override void Render(Vector2 position, bool highlighted) {
            float alpha = Container.Alpha;
            Vector2 pointer = new Vector2(position.X, position.Y - Height() * 0.5f);
            Draw.HollowRect(pointer + new Vector2(-1, -1), GraphWidth+2, GraphHeight+2, Color.White * alpha);
            //Draw.Line(pointer + new Vector2(0, GraphHeight), pointer + new Vector2(GraphWidth, 0), Color.White * alpha);
            
            if (Values == null || Values.Count == 0) return;
            
            //Draw grid lines at 10% intervals each axis
            Color gridlineColor = Color.Gray * alpha * 0.1f;
            for (int i = 0; i <= 10; i++) {
                float xPos = i / 10f * GraphWidth;
                Draw.Line(pointer + new Vector2(xPos, 0), pointer + new Vector2(xPos, GraphHeight), gridlineColor);
                float yPos = i / 10f * GraphHeight;
                Draw.Line(pointer + new Vector2(0, yPos), pointer + new Vector2(GraphWidth, yPos), gridlineColor);
            }
            
            //The values are in the range of 0 and 1, in both x and y
            foreach (var kvp in Values) {
                int x = kvp.Key;
                if (x == 0) continue;
                
                double y = kvp.Value;
                float xPos = x / 100f * GraphWidth;
                float yPos = (float)y * GraphHeight;
                float yOffset = GraphHeight - yPos;

                //Draw the line
                float prevXPos = (x - 1) / 100f * GraphWidth;
                float prevYPos = (float)Values[x - 1] * GraphHeight;
                float prevYOffset = GraphHeight - prevYPos;
                Draw.Line(pointer + new Vector2(prevXPos, prevYOffset), pointer + new Vector2(xPos, yOffset), GraphColor * alpha);
            }
            
            //Highlight the current point
            string currentPointValue = "";
            if (Mod.CurrentChapterPath != null && Mod.CurrentChapterStats != null && Mod.CurrentChapterPath.CurrentRoom != null) {
                var chokeRateData = ChokeRateStat.GetRoomData(Mod.CurrentChapterPath, Mod.CurrentChapterStats);
                int pastDifficulty = 0;
                foreach (RoomInfo tempRoom in Mod.CurrentChapterPath.WalkPath()) {
                    if (tempRoom.RoomNumberInChapter == Mod.CurrentChapterPath.CurrentRoom.RoomNumberInChapter) break; //Current room is uncleared, so stop before this is counted
                    int roomDiff = tempRoom.DifficultyWeight;
                    if (roomDiff == -1) {
                        roomDiff = ConsoleCommands.GetRoomDifficultyBasedOnStats(chokeRateData, tempRoom);
                    }
                    pastDifficulty += roomDiff;
                }
                double pastDiffRatio = (double)pastDifficulty / Mod.CurrentChapterPath.Stats.DifficultyWeight;
                
                //pastDiffRatio is now the x value that we want to highlight in red
                float xPos = (float)pastDiffRatio * GraphWidth;
                float yPos = (float)Values[(int)(pastDiffRatio * 100)] * GraphHeight;
                float yOffset = GraphHeight - yPos;
                Draw.Circle(pointer + new Vector2(xPos, yOffset), 1f, Color.Red, 10);
                Draw.Circle(pointer + new Vector2(xPos, yOffset), 2f, Color.Red, 10);
                Draw.Circle(pointer + new Vector2(xPos, yOffset), 3f, Color.Red, 10);

                double exactValue = DifficultyWeightStats.EnduranceFactor(pastDiffRatio, Slope, Power);
                currentPointValue = $"\n\nGetting to the current room is {Math.Round(pastDiffRatio*100, 2)}% of the map's difficulty and worth {Math.Round(exactValue*100, 2)}% of the maximum points.";
            }

            string hintText = "x-Axis: How far in the map you are difficulty-wise\n" +
                              "y-Axis: % of the maximum points you will earn at that point\n";
            
            //Show examples of 10%, 25%, 50%, 75% and 90% in the hintText
            int[] exampleValues = { 10, 25, 50, 75, 90 };
            foreach (int i in exampleValues) {
                if (Values.ContainsKey(i)) {
                    double y = Values[i];
                    hintText += $"\n{i}%: {Math.Round(y * 100, 2)}%";
                }
            }

            hintText += currentPointValue;
            
            
            ActiveFont.DrawOutline(hintText, 
                                   pointer + new Vector2(GraphWidth + 10f, 0f), 
                                   Vector2.Zero, 
                                   Vector2.One * 0.3f, 
                                   Color.Gray * alpha, 
                                   2f, 
                                   Color.Black * alpha);
            
            //Draw.Line(pointer, pointer + new Vector2(GraphWidth, GraphHeight), Color.White * alpha);
        }
    }
}
