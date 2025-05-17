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

        private double _Slope;
        private double _Power;
        public double TargetSlope;
        public double TargetPower;
        public double MaxGP;

        public double CalculatedForHash = 0;
        public Dictionary<int, double> Values = null;
        
        public Color GraphColor = Color.White;
        
        private float _GraphWidth = 400;
        private float _GraphHeight = 200;

        public EnduranceGraph(double slope, double power, double maxGp) {
            TargetSlope = slope;
            _Slope = slope;
            TargetPower = power;
            _Power = power;
            MaxGP = maxGp;
        }

        public override float LeftWidth() {
            return _GraphWidth;
        }

        public override float Height() {
            return _GraphHeight;
        }
        
        public override void ConfirmPressed() {
            Audio.Play(ConfirmSfx);
        }

        public override void Update() {
            // Approach the target slope and power
            if (_Slope != TargetSlope) {
                _Slope = Calc.Approach((float)_Slope, (float)TargetSlope, Engine.RawDeltaTime * 0.6f);
                if(Math.Abs(_Slope - TargetSlope) < 0.001f) {
                    _Slope = TargetSlope;
                }
            }
            if (_Power != TargetPower) {
                _Power = Calc.Approach((float)_Power, (float)TargetPower, Engine.RawDeltaTime * 0.6f);
                if(Math.Abs(_Power - TargetPower) < 0.001f) {
                    _Power = TargetPower;
                }
            }
            
            double hash = _Slope * 7 + _Power * 11;
            if (CalculatedForHash != hash && hash != 0) {
                CalculatedForHash = hash;
                Values = new Dictionary<int, double>();
                for (int i = 0; i <= 100; i++) {
                    double t = i / 100f;
                    try {
                        Values[i] = DifficultyWeightStats.EnduranceFactor(t, _Slope, _Power);
                    } catch (Exception) {
                        // ignored
                    }
                }
            }
        }

        public override void Render(Vector2 position, bool highlighted) {
            float alpha = Container.Alpha;
            Vector2 pointer = new Vector2(position.X, position.Y - Height() * 0.5f);
            Draw.HollowRect(pointer + new Vector2(-1, -1), _GraphWidth+2, _GraphHeight+2, Color.White * alpha);
            //Draw.Line(pointer + new Vector2(0, GraphHeight), pointer + new Vector2(GraphWidth, 0), Color.White * alpha);
            
            if (Values == null || Values.Count == 0) return;
            
            //Draw grid lines at 10% intervals each axis
            Color gridlineColor = Color.Gray * alpha * 0.1f;
            for (int i = 0; i <= 10; i++) {
                float xPos = i / 10f * _GraphWidth;
                Draw.Line(pointer + new Vector2(xPos, 0), pointer + new Vector2(xPos, _GraphHeight), gridlineColor);
                float yPos = i / 10f * _GraphHeight;
                Draw.Line(pointer + new Vector2(0, yPos), pointer + new Vector2(_GraphWidth, yPos), gridlineColor);
            }
            
            //The values are in the range of 0 and 1, in both x and y
            foreach (var kvp in Values) {
                int x = kvp.Key;
                if (x == 0) continue;
                
                double y = kvp.Value;
                float xPos = x / 100f * _GraphWidth;
                float yPos = (float)y * _GraphHeight;
                float yOffset = _GraphHeight - yPos;

                //Draw the line
                float prevXPos = (x - 1) / 100f * _GraphWidth;
                float prevYPos = (float)Values[x - 1] * _GraphHeight;
                float prevYOffset = _GraphHeight - prevYPos;
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
                float xPos = (float)pastDiffRatio * _GraphWidth;
                float yPos = (float)Values[(int)(pastDiffRatio * 100)] * _GraphHeight;
                float yOffset = _GraphHeight - yPos;
                Draw.Circle(pointer + new Vector2(xPos, yOffset), 1f, Color.Red, 10);
                Draw.Circle(pointer + new Vector2(xPos, yOffset), 2f, Color.Red, 10);
                Draw.Circle(pointer + new Vector2(xPos, yOffset), 3f, Color.Red, 10);

                double exactValue = DifficultyWeightStats.EnduranceFactor(pastDiffRatio, _Slope, _Power);
                double gpValue = exactValue * MaxGP;
                currentPointValue = $"\n\nGetting to the current room is {Math.Round(pastDiffRatio*100, 2)}% of the map's difficulty and worth {Math.Round(gpValue, 2)} gp ({Math.Round(exactValue*100, 2)}%) of the maximum {Math.Round(MaxGP, 2)} gp.";
            }

            string hintText = "x-Axis: How far in the map you are difficulty-wise\n" +
                              "y-Axis: % of the maximum points you will earn at that point\n";
            
            //Show examples of 10%, 25%, 50%, 75% and 90% in the hintText
            int[] exampleValues = { 10, 25, 50, 75, 90 };
            foreach (int i in exampleValues) {
                if (Values.ContainsKey(i)) {
                    double y = Values[i];
                    hintText += $"\n{i}%: {Math.Round(y * 100, 2)}% = {Math.Round(y * MaxGP, 2)} gp";
                }
            }

            hintText += currentPointValue;
            
            
            ActiveFont.DrawOutline(hintText, 
                                   pointer + new Vector2(_GraphWidth + 10f, 0f), 
                                   Vector2.Zero, 
                                   Vector2.One * 0.3f, 
                                   Color.Gray * alpha, 
                                   2f, 
                                   Color.Black * alpha);
            
            //Draw.Line(pointer, pointer + new Vector2(GraphWidth, GraphHeight), Color.White * alpha);
        }
    }
}
