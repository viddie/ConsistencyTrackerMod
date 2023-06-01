using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Celeste.TextMenu;

namespace Celeste.Mod.ConsistencyTracker.Entities.Menu {
    public class FAQEntry : TextMenu.Item {

        public class FAQSectionModel {
            public string Title;
            public List<FAQEntryModel> Entries;
        }
        public class FAQEntryModel {
            public string Question;
            public string Answer;
        }


        public string ConfirmSfx = "event:/ui/main/button_select";
        
        public float QuestionScale = 0.8f;
        public float AnswerScale = 0.5f;

        public Color QuestionColor = Color.White;
        public Color QuestionColorHighlighted = new Color(158, 255, 158);
        public Color AnswerColor = Color.Gray;

        public float MaxLineWidth = 1400f;

        public string Question {
            get => _Question;
            set {
                _Question = $"> {value}";
                _QuestionLineBreak = LineBreakText(_Question, QuestionScale, MaxLineWidth);
            }
        }
        private string _Question;
        private string _QuestionLineBreak;
        
        public string Answer {
            get => _Answer;
            set {
                _Answer = value;
                _AnswerLineBreak = LineBreakText(_Answer, AnswerScale, MaxLineWidth);
            }
        }
        private string _Answer;
        private string _AnswerLineBreak;

        public bool DoExpand = false;
        private float _AnswerAlpha = 0;
        private float _UneasedAlpha = 0;

        public FAQEntry() {  }
        public FAQEntry(string question, string answer) {
            Question = question;
            Selectable = true;
            Answer = answer;

            OnLeave = () => {
                DoExpand = false;
            };
        }

        public string LineBreakText(string text, float scale, float maxWidth) {
            List<string> lines = new List<string>();

            string[] words = text.Split(' ');

            string line = "";
            foreach (string word in words) {
                if (ActiveFont.Measure(word).X * scale > maxWidth) {
                    // word is too long, split it
                    if (line.Length > 0) {
                        lines.Add(line);
                        line = "";
                    }
                    lines.Add(word);
                } else if (ActiveFont.Measure(line + " " + word).X * scale > maxWidth) {
                    // word would make the line too long, start a new line
                    lines.Add(line);
                    line = word;
                } else {
                    // word fits, add it to the line
                    if (line.Length > 0) {
                        line += " ";
                    }
                    line += word;
                }
            }

            if (line.Length > 0) {
                lines.Add(line);
            }
            
            return string.Join("\n", lines);
        }

        public override float LeftWidth() {
            float questionW = string.IsNullOrEmpty(_QuestionLineBreak) ? 0f : ActiveFont.Measure(_QuestionLineBreak).X * QuestionScale;
            float answerW = string.IsNullOrEmpty(_AnswerLineBreak) ? 0f : ActiveFont.Measure(_AnswerLineBreak).X * AnswerScale;
            return Math.Max(questionW, answerW);
        }

        public override float Height() {
            return QuestionHeight() + (AnswerHeight() * _AnswerAlpha);
        }
        public float QuestionHeight() {
            return string.IsNullOrEmpty(_QuestionLineBreak) ? 0f : ActiveFont.HeightOf(_QuestionLineBreak) * QuestionScale;
        }
        public float AnswerHeight() {
            return string.IsNullOrEmpty(_AnswerLineBreak) ? 0f : ActiveFont.HeightOf(_AnswerLineBreak) * AnswerScale * _AnswerAlpha;
        }

        public override void ConfirmPressed() {
            Audio.Play(ConfirmSfx);
            DoExpand = !DoExpand;
        }

        public override void Update() {
            float num = (DoExpand ? 1 : 0);
            if (_AnswerAlpha != num) {
                _UneasedAlpha = Calc.Approach(_UneasedAlpha, num, Engine.RawDeltaTime * 3f);
                if (DoExpand) {
                    _AnswerAlpha = Ease.SineOut(_UneasedAlpha);
                } else {
                    _AnswerAlpha = Ease.SineIn(_UneasedAlpha);
                }
            }
        }

        public override void Render(Vector2 position, bool highlighted) {
            float alpha = Container.Alpha;
            Color strokeColor = Color.Black * (alpha * alpha * alpha);
            Vector2 pointer = new Vector2(position.X, position.Y - Height() * 0.5f);

            if (!string.IsNullOrEmpty(_QuestionLineBreak)) {
                Color color = (highlighted ? Container.HighlightColor : QuestionColor) * alpha;
                ActiveFont.DrawOutline(_QuestionLineBreak, pointer, new Vector2(0, 0), Vector2.One * QuestionScale, color, 2f, strokeColor * alpha);
                pointer.Y += QuestionHeight();
            }
            if (!string.IsNullOrEmpty(_AnswerLineBreak)) {
                ActiveFont.DrawOutline(_AnswerLineBreak, pointer, new Vector2(0, 0f), Vector2.One * AnswerScale, AnswerColor * alpha * _AnswerAlpha, 2f, strokeColor * alpha * _AnswerAlpha);
            }
        }

    }
}
