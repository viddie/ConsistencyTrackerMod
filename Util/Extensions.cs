using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.ConsistencyTracker.Util {
    public static class Extensions {

        public static void AddDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description) {
            TextMenuExt.EaseInSubHeaderExt descriptionText = new TextMenuExt.EaseInSubHeaderExt(description, false, containingMenu) {
                TextColor = Color.Gray,
                HeightExtra = 0f
            };

            subMenu.Add(descriptionText);

            subMenuItem.OnEnter += () => descriptionText.FadeVisible = true;
            subMenuItem.OnLeave += () => descriptionText.FadeVisible = false;
        }

        public static string ToReadableString(this AreaMode mode) {
            switch (mode) {
                case AreaMode.Normal:
                    return "A-Side";
                case AreaMode.BSide:
                    return "B-Side";
                case AreaMode.CSide:
                    return "C-Side";
                default:
                    return "wha-Side";
            }
        }

        public static JsonRectangle ToJsonRectangle(this Rectangle rect) {
            return new JsonRectangle {
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height
            };
        }

        public static JsonCircle ToJsonCircle(this Circle circle) {
            return new JsonCircle {
                X = circle.Position.X,
                Y = circle.Position.Y,
                Radius = circle.Radius
            };
        }

        public static JsonVector2 ToJsonVector2(this Vector2 vec) {
            return new JsonVector2 {
                X = vec.X,
                Y = vec.Y
            };
        }
    }
}
